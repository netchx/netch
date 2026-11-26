#pragma once
#ifndef SOCKSHELPER_H
#define SOCKSHELPER_H
#include "Based.h"
#include "IocpPool.h"

namespace SocksHelper
{
	SOCKET Connect();
	bool Handshake(SOCKET client);
	bool SplitAddr(SOCKET client, PSOCKADDR_IN6 addr, bool resolveDomain = true);
	bool UnpackSocks5UdpPacket(const char* buffer, int length, PSOCKADDR_IN6 target, int* payloadOffset, int* payloadLength);

	typedef class TCP
	{
	public:
		~TCP();

		bool Connect(PSOCKADDR_IN6 target);
		void Close();

		int Send(const char* buffer, int length);
		int Read(char* buffer, int length);

		std::atomic<SOCKET> tcpSocket{ INVALID_SOCKET };

	private:
		std::mutex ioLock_;
	} *PTCP;

	typedef class UDP : public std::enable_shared_from_this<UDP>
	{
	public:
		using PacketCallback = std::function<void(const SOCKADDR_IN6& remote, const char* payload, int length)>;
		using CloseCallback = std::function<void()>;

		~UDP();

		void Stop();
		bool CanRetry() const;

		bool Associate();
		bool CreateUDP();
		bool StartAsync(PacketCallback onPacket, CloseCallback onClose = {});

		int Send(PSOCKADDR_IN6 target, const char* buffer, int length);
		int Read(PSOCKADDR_IN6 target, char* buffer, int length, int* payloadOffset, PTIMEVAL timeout);

		static void WaitAllIoDrained();

		std::atomic<SOCKET> tcpSocket{ INVALID_SOCKET };
		std::atomic<SOCKET> udpSocket{ INVALID_SOCKET };
		std::atomic<bool> stopping{ false };
		std::atomic<bool> ready{ false };
		std::atomic<long long> retryAfterMs{ 0 };
		std::mutex sendLock;

		SOCKADDR_IN6 address = { 0 };

	private:
		struct CtrlRecvOp : IocpOperation
		{
			std::shared_ptr<UDP> owner;
			char buf[1]{};
			DWORD flags = 0;
		};

		struct UdpRecvOp : IocpOperation
		{
			std::shared_ptr<UDP> owner;
			std::vector<char> buf = std::vector<char>(65535);
			SOCKADDR_IN6 fromAddr{};
			INT fromLen = sizeof(SOCKADDR_IN6);
			DWORD flags = 0;
		};

		static void CtrlComplete(IocpOperation* op, DWORD error, DWORD bytes);
		static void RecvComplete(IocpOperation* op, DWORD error, DWORD bytes);

		bool PostCtrlRecv();
		bool PostUdpRecv();
		void NotifyClosed();

		std::mutex ioLock_;
		PacketCallback onPacket_;
		CloseCallback onClose_;
		std::atomic<bool> closeNotified_{ false };
		std::unique_ptr<CtrlRecvOp> ctrlOp_;
		std::unique_ptr<UdpRecvOp> recvOp_;
	} *PUDP;
};

#endif
