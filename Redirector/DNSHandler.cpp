#include "DNSHandler.h"

#include "Data.h"
#include "Utils.h"

#include <stdio.h>

#include <thread>

DNSHandler::DNSHandler(string dnsHost, USHORT dnsPort)
{
	lock_guard<mutex> lg(this->DNSLock);

	this->Started = TRUE;
	this->DNSHost = dnsHost;
	this->DNSPort = dnsPort;

	for (int i = 0; i < 4; i++)
	{
		thread(&DNSHandler::Worker, this).detach();
	}
}

DNSHandler::~DNSHandler()
{
	lock_guard<mutex> lg(this->DNSLock);

	this->Started = FALSE;
	for (PDNSPKT i : this->DNSList)
	{
		this->Delete(i);
	}
	this->DNSList.clear();
}

void DNSHandler::Create(ENDPOINT_ID id, PBYTE target, ULONG targetLength, PCHAR buffer, ULONG bufferLength, PNF_UDP_OPTIONS option)
{
	if (!this->Started)
	{
		return;
	}

	auto data = new DNSPKT();
	data->ID = id;
	data->Target = new BYTE[targetLength]();
	data->TargetLength = targetLength;
	data->Buffer = new CHAR[bufferLength]();
	data->BufferLength = bufferLength;
	data->Option = new NF_UDP_OPTIONS();

	memcpy(data->Target, target, targetLength);
	memcpy(data->Buffer, buffer, bufferLength);
	memcpy(data->Option, option, sizeof(NF_UDP_OPTIONS));

	lock_guard<mutex> lg(this->DNSLock);
	this->DNSList.emplace_back(data);
}

void DNSHandler::Delete(PDNSPKT i)
{
	if (i)
	{
		if (i->Target)
		{
			delete[] i->Target;
		}

		if (i->Buffer)
		{
			delete[] i->Buffer;
		}

		if (i->Option)
		{
			delete i->Option;
		}

		delete i;
	}

	i = NULL;
}

void DNSHandler::Worker()
{
	while (this->Started)
	{
		
	}
}
