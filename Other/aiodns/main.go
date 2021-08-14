package main

import (
	"bufio"
	"fmt"
	"net"
	"net/url"
	"os"
	"strings"

	"github.com/miekg/dns"
)

import "C"

const (
	TYPE_REST = iota
	TYPE_LIST
	TYPE_LISN
	TYPE_CDNS
	TYPE_ODNS
)

var (
	Path     string
	Listen   string
	ChinaDNS string
	OtherDNS string

	CDNS = dns.Client{}
	ODNS = dns.Client{}

	mux       *dns.ServeMux  = nil
	tcpSocket net.Listener   = nil
	udpSocket net.PacketConn = nil
)

//export aiodns_dial
func aiodns_dial(name int, value *C.char) bool {
	switch name {
	case TYPE_REST:
		Path = ""
		Listen = ""
		ChinaDNS = ""
		OtherDNS = ""
	case TYPE_LIST:
		Path = C.GoString(value)
	case TYPE_LISN:
		Listen = C.GoString(value)
	case TYPE_CDNS:
		{
			info, err := url.Parse(C.GoString(value))
			if err != nil {
				fmt.Printf("[aiodns][dial] url.Parse: %v\n", err)
				return false
			}

			switch info.Scheme {
			case "tls":
				CDNS.Net = "tcp-tls"
			default:
				CDNS.Net = "tcp"
			}

			ChinaDNS = info.Host
		}
	case TYPE_ODNS:
		{
			info, err := url.Parse(C.GoString(value))
			if err != nil {
				fmt.Printf("[aiodns][dial] url.Parse: %v\n", err)
				return false
			}

			switch info.Scheme {
			case "tls":
				ODNS.Net = "tcp-tls"
			default:
				ODNS.Net = "tcp"
			}

			OtherDNS = info.Host
		}
	default:
		return false
	}

	return true
}

//export aiodns_init
func aiodns_init() bool {
	mux = dns.NewServeMux()
	if Path != "" {
		file, err := os.Open(Path)
		if err != nil {
			fmt.Printf("[aiodns][init] os.Open: %v\n", err)
			return false
		}
		defer file.Close()

		scan := bufio.NewScanner(file)
		for scan.Scan() {
			mux.HandleFunc(dns.Fqdn(strings.TrimSpace(scan.Text())), handleChinaDNS)
		}
	}
	mux.HandleFunc("in-addr.arpa.", handleServerName)
	mux.HandleFunc(".", handleOtherDNS)

	var err error

	tcpSocket, err = net.Listen("tcp", Listen)
	if err != nil {
		fmt.Printf("[aiodns][init] net.Listen: %v\n", err)
		return false
	}

	udpSocket, err = net.ListenPacket("udp", Listen)
	if err != nil {
		fmt.Printf("[aiodns][init] net.ListenPacket: %v\n", err)
		return false
	}

	go dns.ActivateAndServe(tcpSocket, nil, mux)
	go dns.ActivateAndServe(nil, udpSocket, mux)

	fmt.Println("[aiodns] Started")
	return true
}

//export aiodns_free
func aiodns_free() {
	if tcpSocket != nil {
		tcpSocket.Close()
		tcpSocket = nil
	}

	if udpSocket != nil {
		udpSocket.Close()
		udpSocket = nil
	}

	mux = nil
}

func handleServerName(w dns.ResponseWriter, m *dns.Msg) {
	r := new(dns.Msg)
	r.SetReply(m)

	for i := 0; i < len(m.Question); i++ {
		rr, err := dns.NewRR(fmt.Sprintf("%s PTR Netch", m.Question[i].Name))
		if err != nil {
			fmt.Printf("[aiodns][dns.NewRR] %v\n", err)
			return
		}

		r.Answer = append(m.Answer, rr)
	}

	_ = w.WriteMsg(r)
}

func handleChinaDNS(w dns.ResponseWriter, m *dns.Msg) {
	r, _, err := CDNS.Exchange(m, ChinaDNS)
	if err != nil {
		fmt.Printf("[aiodns] handleChinaDNS: %v\n", err)
	}

	_ = w.WriteMsg(r)
}

func handleOtherDNS(w dns.ResponseWriter, m *dns.Msg) {
	r, _, err := ODNS.Exchange(m, OtherDNS)
	if err != nil {
		fmt.Printf("[aiodns] handleOtherDNS: %v\n", err)
	}

	_ = w.WriteMsg(r)
}

func main() {

}
