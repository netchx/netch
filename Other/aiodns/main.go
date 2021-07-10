package main

import (
	"bufio"
	"flag"
	"log"
	"net"
	"net/url"
	"os"
	"strings"

	"github.com/miekg/dns"
)

var (
	flags struct {
		Path     string
		Listen   string
		ChinaDNS string
		OtherDNS string
	}
)

func main() {
	flag.StringVar(&flags.Path, "c", "", "")
	flag.StringVar(&flags.Listen, "l", ":53", "Listen")
	flag.StringVar(&flags.ChinaDNS, "cdns", "tls://223.5.5.5:853", "China DNS")
	flag.StringVar(&flags.OtherDNS, "odns", "tls://1.1.1.1:853", "Other DNS")
	flag.Parse()

	{
		{
			info, err := url.Parse(flags.ChinaDNS)
			if err != nil {
				log.Fatalf("[aiodns][main][url.Parse] %v", err)
			}

			switch info.Scheme {
			case "tls":
				ChinaDNS.Net = "tcp-tls"
			default:
				ChinaDNS.Net = "tcp"
			}

			flags.ChinaDNS = info.Host
		}

		{
			info, err := url.Parse(flags.OtherDNS)
			if err != nil {
				log.Fatalf("[aiodns][main][url.Parse] %v", err)
			}

			switch info.Scheme {
			case "tls":
				OtherDNS.Net = "tcp-tls"
			default:
				OtherDNS.Net = "tcp"
			}

			flags.OtherDNS = info.Host
		}
	}

	mux := dns.NewServeMux()
	if flags.Path != "" {
		file, err := os.Open(flags.Path)
		if err != nil {
			log.Fatalf("[aiodns][main][os.Open] %v", err)
		}

		scan := bufio.NewScanner(file)
		for scan.Scan() {
			mux.HandleFunc(dns.Fqdn(strings.TrimSpace(scan.Text())), handleChinaDNS)
		}
	}
	mux.HandleFunc("in-addr.arpa.", handleServerName)
	mux.HandleFunc(".", handleOtherDNS)

	tcpSocket, err := net.Listen("tcp", flags.Listen)
	if err != nil {
		log.Fatalf("[aiodns][main][net.Listen] %v", err)
	}

	udpSocket, err := net.ListenPacket("udp", flags.Listen)
	if err != nil {
		log.Fatalf("[aiodns][main][net.ListenPacket] %v", err)
	}

	log.Printf("[aiodns] Started")

	go dns.ActivateAndServe(tcpSocket, nil, mux)
	dns.ActivateAndServe(nil, udpSocket, mux)
}
