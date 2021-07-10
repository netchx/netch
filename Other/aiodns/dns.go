package main

import (
	"fmt"
	"log"

	"github.com/miekg/dns"
)

var (
	ChinaDNS = dns.Client{}
	OtherDNS = dns.Client{}
)

func handleServerName(w dns.ResponseWriter, m *dns.Msg) {
	r := new(dns.Msg)
	r.SetReply(m)

	for i := 0; i < len(m.Question); i++ {
		rr, err := dns.NewRR(fmt.Sprintf("%s PTR Netch", m.Question[i].Name))
		if err != nil {
			log.Printf("[aiodns][dns.NewRR] %v", err)
			return
		}

		r.Answer = append(m.Answer, rr)
	}

	_ = w.WriteMsg(r)
}

func handleChinaDNS(w dns.ResponseWriter, m *dns.Msg) {
	r, _, err := ChinaDNS.Exchange(m, flags.ChinaDNS)
	if err != nil {
		log.Printf("[aiodns][handleChinaDNS] %v", err)
	}

	_ = w.WriteMsg(r)
}

func handleOtherDNS(w dns.ResponseWriter, m *dns.Msg) {
	r, _, err := OtherDNS.Exchange(m, flags.OtherDNS)
	if err != nil {
		log.Printf("[aiodns][handleOtherDNS] %v", err)
	}

	_ = w.WriteMsg(r)
}
