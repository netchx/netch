#include "ConnectionTracker.h"
#include "Based.h"
#include "EventHandler.h"
#include "TCPHandler.h"

#include <algorithm>
#include <array>
#include <deque>
#include <mutex>
#include <unordered_map>
#include <vector>

static_assert(sizeof(ENDPOINT_ID) == sizeof(uint64_t), "ENDPOINT_ID must fit in uint64_t");

namespace ConnectionTracker
{
	namespace
	{
		constexpr size_t kMaxActiveConnections = 2048;
		constexpr size_t kMaxClosedConnections = 1000;
		constexpr size_t kMaxEvents = 4000;
		constexpr size_t kMaxDrainEvents = 500;
		constexpr uint64_t kUdpIdleTimeoutMs = 60000;
		constexpr uint64_t kSweepIntervalMs = 5000;
		constexpr uint64_t kDedupWindowMs = 10000;

		struct ConnectionRecord
		{
			uint64_t id = 0;
			Protocol protocol = Protocol::Tcp;
			uint64_t endpointId = 0;
			bool isError = false;
			bool closed = false;
			uint32_t pid = 0;
			std::string processName;
			std::string processPath;
			std::string parentName;
			std::string remoteIp;
			uint16_t remotePort = 0;
			uint64_t upBytes = 0;
			uint64_t downBytes = 0;
			uint64_t startUnixMs = 0;
			uint64_t endUnixMs = 0;
			uint64_t startSteadyMs = 0;
			uint64_t lastActivitySteadyMs = 0;
			std::string matchedRule;
			std::string ruleSource;
			std::string closeReason;
		};

		struct ConnectionEventRecord
		{
			uint64_t seq = 0;
			uint64_t timestampMs = 0;
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
		};

		struct UdpFlowKey
		{
			uint64_t endpointId = 0;
			std::string remoteIp;
			uint16_t remotePort = 0;

			bool operator==(const UdpFlowKey& other) const noexcept
			{
				return endpointId == other.endpointId && remotePort == other.remotePort && remoteIp == other.remoteIp;
			}
		};

		struct UdpFlowKeyHash
		{
			size_t operator()(const UdpFlowKey& k) const noexcept
			{
				size_t h = std::hash<uint64_t>{}(k.endpointId);
				h ^= std::hash<uint16_t>{}(k.remotePort) + 0x9e3779b97f4a7c15ULL + (h << 6) + (h >> 2);
				h ^= std::hash<std::string>{}(k.remoteIp) + 0x9e3779b97f4a7c15ULL + (h << 6) + (h >> 2);
				return h;
			}
		};

		struct DedupKey
		{
			uint32_t pid = 0;
			uint8_t protocol = 0;
			uint8_t action = 0;
			uint16_t remotePort = 0;
			std::string remoteIp;

			bool operator==(const DedupKey& other) const noexcept
			{
				return pid == other.pid && protocol == other.protocol && action == other.action &&
					remotePort == other.remotePort && remoteIp == other.remoteIp;
			}
		};

		struct DedupKeyHash
		{
			size_t operator()(const DedupKey& k) const noexcept
			{
				size_t h = std::hash<uint32_t>{}(k.pid);
				h ^= (static_cast<size_t>(k.protocol) << 16) ^ (static_cast<size_t>(k.action) << 8) ^ k.remotePort;
				h ^= std::hash<std::string>{}(k.remoteIp) + 0x9e3779b97f4a7c15ULL + (h << 6) + (h >> 2);
				return h;
			}
		};

		std::mutex g_mutex;
		uint64_t g_nextConnId = 1;
		uint64_t g_nextSeq = 1;
		uint64_t g_lastSweepSteadyMs = 0;
		std::unordered_map<uint64_t, ConnectionRecord> g_active;
		std::unordered_map<UdpFlowKey, uint64_t, UdpFlowKeyHash> g_udpFlowIndex;
		std::deque<ConnectionRecord> g_closed;
		std::deque<ConnectionEventRecord> g_events;
		std::unordered_map<DedupKey, uint64_t, DedupKeyHash> g_dedupLastEmitMs;

		uint64_t NowUnixMs()
		{
			FILETIME ft{};
			GetSystemTimeAsFileTime(&ft);
			ULARGE_INTEGER ui{};
			ui.LowPart = ft.dwLowDateTime;
			ui.HighPart = ft.dwHighDateTime;
			constexpr uint64_t kEpochDiff100Ns = 116444736000000000ULL;
			if (ui.QuadPart < kEpochDiff100Ns)
				return 0;
			return (ui.QuadPart - kEpochDiff100Ns) / 10000ULL;
		}

		uint64_t NowSteadyMs()
		{
			return GetTickCount64();
		}

		const char* ProtocolToString(Protocol protocol)
		{
			return protocol == Protocol::Tcp ? "TCP" : "UDP";
		}

		const char* ActionToString(EventAction action)
		{
			switch (action)
			{
			case EventAction::Proxy:
				return "PROXY";
			case EventAction::BypassIp:
				return "BYPASS_IP";
			case EventAction::BypassProc:
				return "BYPASS_PROC";
			case EventAction::DnsProxy:
				return "DNS_PROXY";
			case EventAction::DnsDirect:
				return "DNS_DIRECT";
			case EventAction::Close:
				return "CLOSE";
			case EventAction::Error:
				return "ERROR";
			default:
				return "PROXY";
			}
		}

		void AppendJsonString(std::string& out, std::string_view value)
		{
			out.push_back('"');
			for (unsigned char ch : value)
			{
				switch (ch)
				{
				case '"':
					out.append("\\\"");
					break;
				case '\\':
					out.append("\\\\");
					break;
				case '\b':
					out.append("\\b");
					break;
				case '\f':
					out.append("\\f");
					break;
				case '\n':
					out.append("\\n");
					break;
				case '\r':
					out.append("\\r");
					break;
				case '\t':
					out.append("\\t");
					break;
				default:
					if (ch < 0x20)
					{
						char esc[7]{};
						std::snprintf(esc, sizeof(esc), "\\u%04x", static_cast<unsigned int>(ch));
						out.append(esc, 6);
					}
					else
					{
						out.push_back(static_cast<char>(ch));
					}
					break;
				}
			}
			out.push_back('"');
		}

		void PushEventLocked(ConnectionEventRecord ev)
		{
			ev.seq = g_nextSeq++;
			if (g_events.size() >= kMaxEvents)
				g_events.pop_front();
			g_events.emplace_back(std::move(ev));
		}

		void PushClosedLocked(ConnectionRecord rec)
		{
			if (g_closed.size() >= kMaxClosedConnections)
				g_closed.pop_front();
			g_closed.emplace_back(std::move(rec));
		}

		void MoveActiveToClosedLocked(std::unordered_map<uint64_t, ConnectionRecord>::iterator it,
			uint64_t nowUnixMs,
			uint64_t nowSteadyMs,
			std::string_view reason,
			bool isError,
			std::string_view detail)
		{
			ConnectionRecord rec = std::move(it->second);
			g_active.erase(it);

			if (rec.protocol == Protocol::Udp)
			{
				g_udpFlowIndex.erase(UdpFlowKey{ rec.endpointId, rec.remoteIp, rec.remotePort });
			}

			rec.closed = true;
			rec.isError = isError;
			rec.endUnixMs = nowUnixMs;
			rec.closeReason.assign(reason);

			const uint64_t duration = (nowSteadyMs >= rec.startSteadyMs) ? (nowSteadyMs - rec.startSteadyMs) : 0;

			ConnectionEventRecord ev{};
			ev.timestampMs = nowUnixMs;
			ev.connectionId = rec.id;
			ev.action = isError ? EventAction::Error : EventAction::Close;
			ev.protocol = rec.protocol;
			ev.pid = rec.pid;
			ev.processName = rec.processName;
			ev.processPath = rec.processPath;
			ev.parentName = rec.parentName;
			ev.remoteIp = rec.remoteIp;
			ev.remotePort = rec.remotePort;
			ev.matchedRule = rec.matchedRule;
			ev.ruleSource = rec.ruleSource;
			ev.upBytes = rec.upBytes;
			ev.downBytes = rec.downBytes;
			ev.durationMs = duration;
			ev.detail = !detail.empty() ? std::string(detail) : rec.closeReason;

			PushClosedLocked(std::move(rec));
			PushEventLocked(std::move(ev));
		}

		void SweepExpiredUdpFlowsLocked(uint64_t nowUnixMs, uint64_t nowSteadyMs, bool force)
		{
			if (!force && (nowSteadyMs - g_lastSweepSteadyMs) < kSweepIntervalMs)
				return;

			g_lastSweepSteadyMs = nowSteadyMs;

			std::vector<uint64_t> expiredIds;
			for (const auto& [id, rec] : g_active)
			{
				if (rec.protocol == Protocol::Udp &&
					(nowSteadyMs - rec.lastActivitySteadyMs) >= kUdpIdleTimeoutMs)
				{
					expiredIds.push_back(id);
				}
			}

			for (uint64_t id : expiredIds)
			{
				auto it = g_active.find(id);
				if (it != g_active.end())
					MoveActiveToClosedLocked(it, nowUnixMs, nowSteadyMs, "UDP idle timeout", false, {});
			}
		}

		void EnsureActiveCapacityLocked(uint64_t nowUnixMs, uint64_t nowSteadyMs)
		{
			if (g_active.size() < kMaxActiveConnections)
				return;

			auto victim = g_active.end();
			bool victimIsUdp = false;
			for (auto it = g_active.begin(); it != g_active.end(); ++it)
			{
				const bool isUdp = (it->second.protocol == Protocol::Udp);
				if (victim == g_active.end())
				{
					victim = it;
					victimIsUdp = isUdp;
					continue;
				}
				if (isUdp && !victimIsUdp)
				{
					victim = it;
					victimIsUdp = true;
					continue;
				}
				if (isUdp == victimIsUdp && it->second.lastActivitySteadyMs < victim->second.lastActivitySteadyMs)
				{
					victim = it;
				}
			}

			if (victim != g_active.end())
				MoveActiveToClosedLocked(victim, nowUnixMs, nowSteadyMs, "Active tracker limit reached", false, {});
		}

		void AppendConnectionJson(std::string& out, const ConnectionRecord& rec)
		{
			out.append("{\"id\":");
			out.append(std::to_string(rec.id));
			out.append(",\"protocol\":\"");
			out.append(ProtocolToString(rec.protocol));
			out.append("\",\"state\":\"");
			if (!rec.closed)
				out.append("Active");
			else if (rec.isError)
				out.append("Error");
			else
				out.append("Closed");
			out.append("\",\"action\":\"PROXY\",\"pid\":");
			out.append(std::to_string(rec.pid));
			out.append(",\"processName\":");
			AppendJsonString(out, rec.processName);
			out.append(",\"processPath\":");
			AppendJsonString(out, rec.processPath);
			out.append(",\"parentName\":");
			AppendJsonString(out, rec.parentName);
			out.append(",\"remoteIp\":");
			AppendJsonString(out, rec.remoteIp);
			out.append(",\"remotePort\":");
			out.append(std::to_string(rec.remotePort));
			out.append(",\"upBytes\":");
			out.append(std::to_string(rec.upBytes));
			out.append(",\"downBytes\":");
			out.append(std::to_string(rec.downBytes));
			out.append(",\"startUnixMs\":");
			out.append(std::to_string(rec.startUnixMs));
			out.append(",\"endUnixMs\":");
			out.append(std::to_string(rec.endUnixMs));
			out.append(",\"matchedRule\":");
			AppendJsonString(out, rec.matchedRule);
			out.append(",\"ruleSource\":");
			AppendJsonString(out, rec.ruleSource);
			out.append(",\"closeReason\":");
			AppendJsonString(out, rec.closeReason);
			out.push_back('}');
		}

		void AppendEventJson(std::string& out, const ConnectionEventRecord& ev)
		{
			out.append("{\"seq\":");
			out.append(std::to_string(ev.seq));
			out.append(",\"timestampMs\":");
			out.append(std::to_string(ev.timestampMs));
			out.append(",\"connectionId\":");
			out.append(std::to_string(ev.connectionId));
			out.append(",\"action\":\"");
			out.append(ActionToString(ev.action));
			out.append("\",\"protocol\":\"");
			out.append(ProtocolToString(ev.protocol));
			out.append("\",\"pid\":");
			out.append(std::to_string(ev.pid));
			out.append(",\"processName\":");
			AppendJsonString(out, ev.processName);
			out.append(",\"processPath\":");
			AppendJsonString(out, ev.processPath);
			out.append(",\"parentName\":");
			AppendJsonString(out, ev.parentName);
			out.append(",\"remoteIp\":");
			AppendJsonString(out, ev.remoteIp);
			out.append(",\"remotePort\":");
			out.append(std::to_string(ev.remotePort));
			out.append(",\"matchedRule\":");
			AppendJsonString(out, ev.matchedRule);
			out.append(",\"ruleSource\":");
			AppendJsonString(out, ev.ruleSource);
			out.append(",\"upBytes\":");
			out.append(std::to_string(ev.upBytes));
			out.append(",\"downBytes\":");
			out.append(std::to_string(ev.downBytes));
			out.append(",\"durationMs\":");
			out.append(std::to_string(ev.durationMs));
			out.append(",\"detail\":");
			AppendJsonString(out, ev.detail);
			out.push_back('}');
		}

		size_t WriteJsonToCallerBuffer(const std::string& json, char* buffer, size_t capacity)
		{
			if (buffer != nullptr && capacity > json.size())
			{
				if (!json.empty())
					std::memcpy(buffer, json.data(), json.size());
				buffer[json.size()] = '\0';
			}
			return json.size();
		}
	}

	std::string WideToUtf8(std::wstring_view text)
	{
		if (text.empty())
			return {};

		const int srcLen = static_cast<int>(text.size());
		const int needed = WideCharToMultiByte(CP_UTF8, 0, text.data(), srcLen, nullptr, 0, nullptr, nullptr);
		if (needed <= 0)
			return {};

		std::string result(static_cast<size_t>(needed), '\0');
		WideCharToMultiByte(CP_UTF8, 0, text.data(), srcLen, result.data(), needed, nullptr, nullptr);
		return result;
	}

	std::string ExtractProcessBaseName(std::wstring_view path)
	{
		if (path.empty())
			return {};

		const size_t pos = path.find_last_of(L"\\/");
		const std::wstring_view base = (pos == std::wstring_view::npos) ? path : path.substr(pos + 1);
		return WideToUtf8(base);
	}

	void ResetActive()
	{
		const uint64_t nowUnix = NowUnixMs();
		const uint64_t nowSteady = NowSteadyMs();

		std::lock_guard<std::mutex> lg(g_mutex);
		while (!g_active.empty())
		{
			MoveActiveToClosedLocked(g_active.begin(), nowUnix, nowSteady, "Redirector reset", false, {});
		}
		g_udpFlowIndex.clear();
		g_dedupLastEmitMs.clear();
		g_lastSweepSteadyMs = nowSteady;
	}

	void OnRedirectorStopped()
	{
		const uint64_t nowUnix = NowUnixMs();
		const uint64_t nowSteady = NowSteadyMs();

		std::lock_guard<std::mutex> lg(g_mutex);
		while (!g_active.empty())
		{
			MoveActiveToClosedLocked(g_active.begin(), nowUnix, nowSteady, "Redirector stopped", false, {});
		}
		g_udpFlowIndex.clear();
		g_dedupLastEmitMs.clear();
	}

	uint64_t OpenTcpConnection(const ConnectionOpenInfo& info)
	{
		const uint64_t nowUnix = NowUnixMs();
		const uint64_t nowSteady = NowSteadyMs();

		std::lock_guard<std::mutex> lg(g_mutex);
		SweepExpiredUdpFlowsLocked(nowUnix, nowSteady, false);
		EnsureActiveCapacityLocked(nowUnix, nowSteady);

		const uint64_t connId = g_nextConnId++;
		ConnectionRecord rec{};
		rec.id = connId;
		rec.protocol = Protocol::Tcp;
		rec.endpointId = info.endpointId;
		rec.pid = info.pid;
		rec.processName = info.processName;
		rec.processPath = info.processPath;
		rec.parentName = info.parentName;
		rec.remoteIp = info.remoteIp;
		rec.remotePort = info.remotePort;
		rec.startUnixMs = nowUnix;
		rec.startSteadyMs = nowSteady;
		rec.lastActivitySteadyMs = nowSteady;
		rec.matchedRule = info.matchedRule;
		rec.ruleSource = info.ruleSource;

		ConnectionEventRecord ev{};
		ev.timestampMs = nowUnix;
		ev.connectionId = connId;
		ev.action = EventAction::Proxy;
		ev.protocol = Protocol::Tcp;
		ev.pid = rec.pid;
		ev.processName = rec.processName;
		ev.processPath = rec.processPath;
		ev.parentName = rec.parentName;
		ev.remoteIp = rec.remoteIp;
		ev.remotePort = rec.remotePort;
		ev.matchedRule = rec.matchedRule;
		ev.ruleSource = rec.ruleSource;

		g_active.emplace(connId, std::move(rec));
		PushEventLocked(std::move(ev));
		return connId;
	}

	uint64_t TouchOrOpenUdpFlow(const ConnectionOpenInfo& info, uint64_t upDeltaBytes)
	{
		const uint64_t nowUnix = NowUnixMs();
		const uint64_t nowSteady = NowSteadyMs();
		const UdpFlowKey flowKey{ info.endpointId, info.remoteIp, info.remotePort };

		std::lock_guard<std::mutex> lg(g_mutex);
		SweepExpiredUdpFlowsLocked(nowUnix, nowSteady, false);

		auto idxIt = g_udpFlowIndex.find(flowKey);
		if (idxIt != g_udpFlowIndex.end())
		{
			auto actIt = g_active.find(idxIt->second);
			if (actIt != g_active.end())
			{
				actIt->second.upBytes += upDeltaBytes;
				actIt->second.lastActivitySteadyMs = nowSteady;
				return actIt->second.id;
			}
			g_udpFlowIndex.erase(idxIt);
		}

		EnsureActiveCapacityLocked(nowUnix, nowSteady);

		const uint64_t connId = g_nextConnId++;
		ConnectionRecord rec{};
		rec.id = connId;
		rec.protocol = Protocol::Udp;
		rec.endpointId = info.endpointId;
		rec.pid = info.pid;
		rec.processName = info.processName;
		rec.processPath = info.processPath;
		rec.parentName = info.parentName;
		rec.remoteIp = info.remoteIp;
		rec.remotePort = info.remotePort;
		rec.upBytes = upDeltaBytes;
		rec.startUnixMs = nowUnix;
		rec.startSteadyMs = nowSteady;
		rec.lastActivitySteadyMs = nowSteady;
		rec.matchedRule = info.matchedRule;
		rec.ruleSource = info.ruleSource;

		ConnectionEventRecord ev{};
		ev.timestampMs = nowUnix;
		ev.connectionId = connId;
		ev.action = EventAction::Proxy;
		ev.protocol = Protocol::Udp;
		ev.pid = rec.pid;
		ev.processName = rec.processName;
		ev.processPath = rec.processPath;
		ev.parentName = rec.parentName;
		ev.remoteIp = rec.remoteIp;
		ev.remotePort = rec.remotePort;
		ev.matchedRule = rec.matchedRule;
		ev.ruleSource = rec.ruleSource;
		ev.upBytes = upDeltaBytes;

		g_active.emplace(connId, std::move(rec));
		g_udpFlowIndex.emplace(flowKey, connId);
		PushEventLocked(std::move(ev));
		return connId;
	}

	void AddBytes(uint64_t connId, uint64_t upDeltaBytes, uint64_t downDeltaBytes)
	{
		if (connId == 0 || (upDeltaBytes == 0 && downDeltaBytes == 0))
			return;

		const uint64_t nowSteady = NowSteadyMs();
		std::lock_guard<std::mutex> lg(g_mutex);
		auto it = g_active.find(connId);
		if (it == g_active.end())
			return;

		it->second.upBytes += upDeltaBytes;
		it->second.downBytes += downDeltaBytes;
		it->second.lastActivitySteadyMs = nowSteady;
	}

	void AddUdpDownloadBytes(uint64_t endpointId, const std::string& remoteIp, uint16_t remotePort, uint64_t downDeltaBytes)
	{
		if (downDeltaBytes == 0)
			return;

		const uint64_t nowSteady = NowSteadyMs();
		const UdpFlowKey flowKey{ endpointId, remoteIp, remotePort };

		std::lock_guard<std::mutex> lg(g_mutex);
		auto idxIt = g_udpFlowIndex.find(flowKey);
		if (idxIt == g_udpFlowIndex.end())
			return;

		auto actIt = g_active.find(idxIt->second);
		if (actIt == g_active.end())
			return;

		actIt->second.downBytes += downDeltaBytes;
		actIt->second.lastActivitySteadyMs = nowSteady;
	}

	void MarkClosed(uint64_t connId, std::string_view reason, bool isError, std::string_view detail)
	{
		if (connId == 0)
			return;

		const uint64_t nowUnix = NowUnixMs();
		const uint64_t nowSteady = NowSteadyMs();

		std::lock_guard<std::mutex> lg(g_mutex);
		auto it = g_active.find(connId);
		if (it == g_active.end())
			return;

		MoveActiveToClosedLocked(it, nowUnix, nowSteady, reason, isError, detail);
	}

	void CloseUdpEndpoint(uint64_t endpointId, std::string_view reason)
	{
		const uint64_t nowUnix = NowUnixMs();
		const uint64_t nowSteady = NowSteadyMs();

		std::lock_guard<std::mutex> lg(g_mutex);
		std::vector<uint64_t> ids;
		for (const auto& [key, connId] : g_udpFlowIndex)
		{
			if (key.endpointId == endpointId)
				ids.push_back(connId);
		}

		for (uint64_t connId : ids)
		{
			auto it = g_active.find(connId);
			if (it != g_active.end())
				MoveActiveToClosedLocked(it, nowUnix, nowSteady, reason, false, {});
		}
	}

	void RecordEvent(const ConnectionEventInput& input)
	{
		const uint64_t nowUnix = NowUnixMs();
		const uint64_t nowSteady = NowSteadyMs();

		std::lock_guard<std::mutex> lg(g_mutex);
		if (input.deduplicate)
		{
			const DedupKey key{
				input.pid,
				static_cast<uint8_t>(input.protocol),
				static_cast<uint8_t>(input.action),
				input.remotePort,
				input.remoteIp
			};

			auto dedupIt = g_dedupLastEmitMs.find(key);
			if (dedupIt != g_dedupLastEmitMs.end() && (nowSteady - dedupIt->second) < kDedupWindowMs)
				return;

			g_dedupLastEmitMs[key] = nowSteady;
			if (g_dedupLastEmitMs.size() > 4096)
			{
				for (auto it = g_dedupLastEmitMs.begin(); it != g_dedupLastEmitMs.end();)
				{
					if ((nowSteady - it->second) >= kDedupWindowMs)
						it = g_dedupLastEmitMs.erase(it);
					else
						++it;
				}
				if (g_dedupLastEmitMs.size() > 4096)
					g_dedupLastEmitMs.clear();
			}
		}

		ConnectionEventRecord ev{};
		ev.timestampMs = nowUnix;
		ev.connectionId = input.connectionId;
		ev.action = input.action;
		ev.protocol = input.protocol;
		ev.pid = input.pid;
		ev.processName = input.processName;
		ev.processPath = input.processPath;
		ev.parentName = input.parentName;
		ev.remoteIp = input.remoteIp;
		ev.remotePort = input.remotePort;
		ev.matchedRule = input.matchedRule;
		ev.ruleSource = input.ruleSource;
		ev.upBytes = input.upBytes;
		ev.downBytes = input.downBytes;
		ev.durationMs = input.durationMs;
		ev.detail = input.detail;
		PushEventLocked(std::move(ev));
	}

	size_t GetConnectionsJson(char* buffer, size_t capacity)
	{
		std::vector<ConnectionRecord> activeSnapshot;
		std::vector<ConnectionRecord> closedSnapshot;
		{
			const uint64_t nowUnix = NowUnixMs();
			const uint64_t nowSteady = NowSteadyMs();
			std::lock_guard<std::mutex> lg(g_mutex);
			SweepExpiredUdpFlowsLocked(nowUnix, nowSteady, true);

			activeSnapshot.reserve(g_active.size());
			for (const auto& [id, rec] : g_active)
				activeSnapshot.push_back(rec);

			closedSnapshot.assign(g_closed.begin(), g_closed.end());
		}

		std::sort(activeSnapshot.begin(), activeSnapshot.end(), [](const ConnectionRecord& a, const ConnectionRecord& b) {
			return a.id > b.id;
		});

		std::string json;
		json.reserve((activeSnapshot.size() + closedSnapshot.size()) * 220 + 32);
		json.append("{\"active\":[");
		for (size_t i = 0; i < activeSnapshot.size(); ++i)
		{
			if (i > 0) json.push_back(',');
			AppendConnectionJson(json, activeSnapshot[i]);
		}
		json.append("],\"closed\":[");
		for (auto it = closedSnapshot.rbegin(); it != closedSnapshot.rend(); ++it)
		{
			if (it != closedSnapshot.rbegin()) json.push_back(',');
			AppendConnectionJson(json, *it);
		}
		json.append("]}");

		return WriteJsonToCallerBuffer(json, buffer, capacity);
	}

	size_t DrainConnectionEventsJson(uint64_t afterSeq, char* buffer, size_t capacity)
	{
		std::vector<ConnectionEventRecord> batch;
		uint64_t nextSeq = afterSeq;
		bool hasMore = false;

		{
			std::lock_guard<std::mutex> lg(g_mutex);
			auto it = std::upper_bound(g_events.begin(), g_events.end(), afterSeq,
				[](uint64_t seqValue, const ConnectionEventRecord& ev) {
					return seqValue < ev.seq;
				});

			while (it != g_events.end() && batch.size() < kMaxDrainEvents)
			{
				batch.push_back(*it);
				nextSeq = it->seq;
				++it;
			}

			hasMore = (it != g_events.end());
		}

		std::string json;
		json.reserve(batch.size() * 240 + 48);
		json.append("{\"nextSeq\":");
		json.append(std::to_string(nextSeq));
		json.append(",\"hasMore\":");
		json.append(hasMore ? "true" : "false");
		json.append(",\"events\":[");
		for (size_t i = 0; i < batch.size(); ++i)
		{
			if (i > 0) json.push_back(',');
			AppendEventJson(json, batch[i]);
		}
		json.append("]}");

		return WriteJsonToCallerBuffer(json, buffer, capacity);
	}

	bool CloseConnection(uint64_t connId)
	{
		Protocol protocol = Protocol::Tcp;
		uint64_t endpointId = 0;
		{
			std::lock_guard<std::mutex> lg(g_mutex);
			auto it = g_active.find(connId);
			if (it == g_active.end())
				return false;
			protocol = it->second.protocol;
			endpointId = it->second.endpointId;
		}

		if (protocol == Protocol::Tcp)
		{
			TCPHandler::CloseSession(connId);
			MarkClosed(connId, "Closed by user");
		}
		else
		{
			eh_closeUdpFlow(static_cast<ENDPOINT_ID>(endpointId), connId);
			MarkClosed(connId, "Closed by user");
		}

		return true;
	}

	void CloseAllConnections()
	{
		TCPHandler::CloseAllSessions();
		eh_closeAllUdpFlows();

		const uint64_t nowUnix = NowUnixMs();
		const uint64_t nowSteady = NowSteadyMs();
		std::lock_guard<std::mutex> lg(g_mutex);
		while (!g_active.empty())
		{
			MoveActiveToClosedLocked(g_active.begin(), nowUnix, nowSteady, "Closed all by user", false, {});
		}
		g_udpFlowIndex.clear();
	}

	void ClearClosedConnections()
	{
		std::lock_guard<std::mutex> lg(g_mutex);
		g_closed.clear();
	}
}
