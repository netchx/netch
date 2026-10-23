#pragma once
#ifndef SOCKSHELPER_H
#define SOCKSHELPER_H
#include "Based.h"

namespace SocksHelper
{
	SOCKET Connect();
	bool Handshake(SOCKET client);
	bool SplitAddr(SOCKET client, PSOCKADDR_IN6 addr);

	typedef class TCP
	{
	public:
		~TCP();

		bool Connect(PSOCKADDR_IN6 target);

		int Send(const char* buffer, int length);
		int Read(char* buffer, int length);

		SOCKET tcpSocket = INVALID_SOCKET;
	} *PTCP;

	typedef class UDP : public std::enable_shared_from_this<UDP>
	{
	public:
		~UDP();

		static void Run(std::shared_ptr<UDP> remote);
		void Stop();
		bool CanRetry() const;

		bool Associate();
		bool CreateUDP();

		int Send(PSOCKADDR_IN6 target, const char* buffer, int length);
		int Read(PSOCKADDR_IN6 target, char* buffer, int length, int* payloadOffset, PTIMEVAL timeout);

		std::atomic<SOCKET> tcpSocket{ INVALID_SOCKET };
		std::atomic<SOCKET> udpSocket{ INVALID_SOCKET };
		std::atomic<bool> stopping{ false };
		std::atomic<long long> retryAfterMs{ 0 };
		std::mutex sendLock;

		SOCKADDR_IN6 address = { 0 };
	} *PUDP;
};

#endif
