#pragma once
#ifndef DNS_H
#define DNS_H
#include <Windows.h>

#include <nfapi.h>

void dns_init();
void dns_free();
void dnsWorker();
void dnsCreate(ENDPOINT_ID id, PBYTE target, ULONG targetLength, PCHAR buffer, ULONG bufferLength, PNF_UDP_OPTIONS options);
void dnsDelete();

#endif
