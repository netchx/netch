package main

import (
	"bufio"
	"fmt"
	"net"
	"net/url"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"github.com/miekg/dns"
	"log"
)

import "C"

const (
	TYPE_REST = iota
	TYPE_LIST
	TYPE_LISN
	TYPE_CDNS
	TYPE_ODNS
	TYPE_LOG
)

var (
	Path     string
	Listen   string

	CDNS = dnsResolver{}
	ODNS = dnsResolver{}

	mux       *dns.ServeMux  = nil
	tcpSocket net.Listener   = nil
	udpSocket net.PacketConn = nil
	logFile   *os.File       = nil
	logWriter *log.Logger    = nil
	logMu     sync.Mutex
)

type dnsEndpoint struct {
	Address string
	Network string
}

type dnsResolver struct {
	Endpoint dnsEndpoint
	Client   dns.Client
}

//export aiodns_dial
func aiodns_dial(name int, value *C.char) bool {
	switch name {
	case TYPE_REST:
		closeLog()
		Path = ""
		Listen = ""
		CDNS = dnsResolver{}
		ODNS = dnsResolver{}
	case TYPE_LIST:
		Path = C.GoString(value)
	case TYPE_LISN:
		Listen = C.GoString(value)
	case TYPE_CDNS:
		if !configureResolver(&CDNS, C.GoString(value)) {
			return false
		}
	case TYPE_ODNS:
		if !configureResolver(&ODNS, C.GoString(value)) {
			return false
		}
	case TYPE_LOG:
		return configureLog(C.GoString(value))
	default:
		return false
	}

	return true
}

func configureLog(raw string) bool {
	raw = strings.TrimSpace(raw)
	if raw == "" {
		fmt.Println("[aiodns][dial] log path is empty")
		return false
	}

	if err := os.MkdirAll(filepath.Dir(raw), 0755); err != nil {
		fmt.Printf("[aiodns][dial] create log directory: %v\n", err)
		return false
	}

	file, err := os.OpenFile(raw, os.O_CREATE|os.O_WRONLY|os.O_TRUNC|os.O_APPEND, 0644)
	if err != nil {
		fmt.Printf("[aiodns][dial] open log file: %v\n", err)
		return false
	}

	logMu.Lock()
	logFile = file
	logWriter = log.New(file, "", log.LstdFlags|log.Lmicroseconds)
	logMu.Unlock()
	writeLog("log opened: %s", raw)
	return true
}

func closeLog() {
	logMu.Lock()
	defer logMu.Unlock()

	if logWriter != nil {
		logWriter.Print("log closed")
	}
	if logFile != nil {
		_ = logFile.Close()
	}
	logWriter = nil
	logFile = nil
}

func writeLog(format string, args ...interface{}) {
	line := fmt.Sprintf(format, args...)
	fmt.Printf("[aiodns] %s\n", line)

	logMu.Lock()
	defer logMu.Unlock()
	if logWriter != nil {
		logWriter.Print(line)
	}
}

func configureResolver(resolver *dnsResolver, raw string) bool {
	raw = strings.TrimSpace(raw)
	if raw == "" {
		fmt.Println("[aiodns][dial] DNS endpoint is empty")
		return false
	}

	// A bare host:port remains supported and uses the normal DNS transport.
	network := "udp"
	address := raw
	if strings.Contains(raw, "://") {
		info, err := url.Parse(raw)
		if err != nil || info.Host == "" {
			fmt.Printf("[aiodns][dial] url.Parse: %v\n", err)
			return false
		}

		address = info.Host
		switch strings.ToLower(info.Scheme) {
		case "udp":
			network = "udp"
		case "tcp":
			network = "tcp"
		case "tls":
			network = "tcp-tls"
		default:
			fmt.Printf("[aiodns][dial] unsupported DNS scheme: %s\n", info.Scheme)
			return false
		}
	}

	resolver.Endpoint = dnsEndpoint{Address: address, Network: network}
	resolver.Client.Net = network
	return true
}

func (resolver *dnsResolver) exchange(name string, message *dns.Msg) (*dns.Msg, time.Duration, error) {
	started := time.Now()
	response, roundTrip, err := resolver.Client.Exchange(message, resolver.Endpoint.Address)
	fallback := false
	if err == nil && response != nil && response.Truncated && resolver.Endpoint.Network == "udp" {
		// Large DNS answers may be truncated over UDP. Retry them over TCP while
		// keeping the fast path connectionless for ordinary lookups.
		tcpClient := resolver.Client
		tcpClient.Net = "tcp"
		fallback = true
		response, roundTrip, err = tcpClient.Exchange(message, resolver.Endpoint.Address)
	}

	query := "<empty>"
	if len(message.Question) > 0 {
		question := message.Question[0]
		query = fmt.Sprintf("%s/%s", question.Name, dns.TypeToString[question.Qtype])
	}
	answers := 0
	truncated := false
	if response != nil {
		answers = len(response.Answer)
		truncated = response.Truncated
	}
	writeLog("query=%s resolver=%s upstream=%s transport=%s rtt=%s elapsed=%s answers=%d truncated=%t fallback_tcp=%t error=%v",
		query, name, resolver.Endpoint.Address, resolver.Endpoint.Network, roundTrip, time.Since(started), answers, truncated, fallback, err)

	return response, roundTrip, err
}

//export aiodns_init
func aiodns_init() bool {
	mux = dns.NewServeMux()
	if Path != "" {
		file, err := os.Open(Path)
		if err != nil {
			writeLog("[init] os.Open: %v", err)
			return false
		}
		defer file.Close()

		scan := bufio.NewScanner(file)
		for scan.Scan() {
			mux.HandleFunc(dns.Fqdn(strings.TrimSpace(scan.Text())), handleChinaDNS)
		}
		if err := scan.Err(); err != nil {
			writeLog("[init] read rule file: %v", err)
			return false
		}
	}
	mux.HandleFunc("in-addr.arpa.", handleServerName)
	mux.HandleFunc(".", handleOtherDNS)

	var err error

	tcpSocket, err = net.Listen("tcp", Listen)
	if err != nil {
		writeLog("[init] net.Listen: %v", err)
		return false
	}

	udpSocket, err = net.ListenPacket("udp", Listen)
	if err != nil {
		writeLog("[init] net.ListenPacket: %v", err)
		return false
	}

	go dns.ActivateAndServe(tcpSocket, nil, mux)
	go dns.ActivateAndServe(nil, udpSocket, mux)

	writeLog("started: listen=%s rules=%s ChinaDNS=%s/%s OtherDNS=%s/%s", Listen, Path,
		CDNS.Endpoint.Address, CDNS.Endpoint.Network, ODNS.Endpoint.Address, ODNS.Endpoint.Network)
	return true
}

//export aiodns_free
func aiodns_free() {
	writeLog("stopping")
	if tcpSocket != nil {
		tcpSocket.Close()
		tcpSocket = nil
	}

	if udpSocket != nil {
		udpSocket.Close()
		udpSocket = nil
	}

	mux = nil
	closeLog()
}

func handleServerName(w dns.ResponseWriter, m *dns.Msg) {
	r := new(dns.Msg)
	r.SetReply(m)

	for i := 0; i < len(m.Question); i++ {
		rr, err := dns.NewRR(fmt.Sprintf("%s PTR Netch", m.Question[i].Name))
		if err != nil {
			writeLog("[dns.NewRR] %v", err)
			return
		}

		r.Answer = append(r.Answer, rr)
	}

	_ = w.WriteMsg(r)
}

func handleChinaDNS(w dns.ResponseWriter, m *dns.Msg) {
	r, _, err := CDNS.exchange("ChinaDNS", m)
	if err != nil || r == nil {
		writeLog("ChinaDNS response failed: %v", err)
		failure := new(dns.Msg)
		failure.SetRcode(m, dns.RcodeServerFailure)
		_ = w.WriteMsg(failure)
		return
	}

	_ = w.WriteMsg(r)
}

func handleOtherDNS(w dns.ResponseWriter, m *dns.Msg) {
	r, _, err := ODNS.exchange("OtherDNS", m)
	if err != nil || r == nil {
		writeLog("OtherDNS response failed: %v", err)
		failure := new(dns.Msg)
		failure.SetRcode(m, dns.RcodeServerFailure)
		_ = w.WriteMsg(failure)
		return
	}

	_ = w.WriteMsg(r)
}

func main() {

}
