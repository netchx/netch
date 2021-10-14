#pragma once
#ifndef TCPHANDLER_H
#define TCPHANDLER_H
#include "Based.h"
#include "SocksHelper.h"

namespace TCPHandler
{
	bool INIT();
	void FREE();

	void CreateHandler(SOCKADDR_IN6 client, SOCKADDR_IN6 remote);
	void DeleteHandler(SOCKADDR_IN6 client);

	void Accept();
	void Handle(SOCKET client);

	void Read(SOCKET client, SocksHelper::PTCP remote);
	void Send(SOCKET client, SocksHelper::PTCP remote);
}

#endif
