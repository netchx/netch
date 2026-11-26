#pragma once

#include <cstddef>
#include <cstdint>
#include <string>
#include <string_view>

namespace ConnectionTracker
{
	enum class Protocol : uint8_t
	{
		Tcp = 0,
		Udp = 1
	};

	enum class EventAction : uint8_t
	{
		Proxy = 0,
		BypassIp = 1,
		BypassProc = 2,
		DnsProxy = 3,
		DnsDirect = 4,
		Close = 5,
		Error = 6
	};

	struct ConnectionOpenInfo
	{
		Protocol protocol = Protocol::Tcp;
		uint64_t endpointId = 0;
		uint32_t pid = 0;
		std::string processName;
		std::string processPath;
		std::string parentName;
		std::string remoteIp;
		uint16_t remotePort = 0;
		std::string matchedRule;
		std::string ruleSource;
	};

	struct ConnectionEventInput
	{
		uint64_t connectionId = 0;
		EventAction action = EventAction::Proxy;
		Protocol protocol = Protocol::Tcp;
		uint32_t pid = 0;
		std::string processName;
		std::string processPath;
		std::string parentName;
		std::string remoteIp;
		uint16_t remotePort = 0;
		std::string matchedRule;
		std::string ruleSource;
		uint64_t upBytes = 0;
		uint64_t downBytes = 0;
		uint64_t durationMs = 0;
		std::string detail;
		bool deduplicate = false;
	};

	std::string WideToUtf8(std::wstring_view text);
	std::string ExtractProcessBaseName(std::wstring_view path);

	void ResetActive();
	void OnRedirectorStopped();

	uint64_t OpenTcpConnection(const ConnectionOpenInfo& info);
	uint64_t TouchOrOpenUdpFlow(const ConnectionOpenInfo& info, uint64_t upDeltaBytes);
	void AddBytes(uint64_t connId, uint64_t upDeltaBytes, uint64_t downDeltaBytes);
	void AddUdpDownloadBytes(uint64_t endpointId, const std::string& remoteIp, uint16_t remotePort, uint64_t downDeltaBytes);

	void MarkClosed(uint64_t connId, std::string_view reason, bool isError = false, std::string_view detail = {});
	void CloseUdpEndpoint(uint64_t endpointId, std::string_view reason);

	void RecordEvent(const ConnectionEventInput& input);

	size_t GetConnectionsJson(char* buffer, size_t capacity);
	size_t DrainConnectionEventsJson(uint64_t afterSeq, char* buffer, size_t capacity);

	bool CloseConnection(uint64_t connId);
	void CloseAllConnections();
	void ClearClosedConnections();
}
