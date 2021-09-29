#pragma once
#ifndef SOCKSHELPER_H
#define SOCKSHELPER_H
#include "Based.h"

namespace SocksHelper
{
	namespace Utils
	{
		SOCKET Connect();
		bool Handshake(SOCKET client);
		bool ReadAddr(SOCKET client, char type, PSOCKADDR addr);
	}

	typedef class TCP
	{
	public:
		TCP();
		TCP(SOCKET tcpSocket);
		~TCP();

		bool Connect(PSOCKADDR target);

		int Send(const char* buffer, int length);
		int Read(char* buffer, int length);

		SOCKET tcpSocket = INVALID_SOCKET;
	} *PTCP;

	typedef class UDP
	{
	public:
		UDP();
		UDP(SOCKET tcpSocket);
		~UDP();

		bool Associate();
		bool CreateUDP();

		int Send(PSOCKADDR target, const char* buffer, int length);
		int Read(PSOCKADDR target, char* buffer, int length);

		SOCKET tcpSocket = INVALID_SOCKET;
		SOCKET udpSocket = INVALID_SOCKET;
	private:
		void run();

		thread tcpThread;
		SOCKADDR_IN6 address = { 0 };
	} *PUDP;
};

#endif
