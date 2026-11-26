#pragma once

#include <array>
#include <cstdint>
#include <string>
#include <string_view>
#include <vector>

struct sockaddr;

struct IpCidrRule
{
	uint16_t family = 0; // AF_INET or AF_INET6 (IPv4-mapped rules normalize to AF_INET)
	uint8_t prefixLength = 0;
	std::array<uint8_t, 16> networkBytes{};
	std::string canonicalText;
};

enum class IpFilterAction
{
	Proxy = 0,
	BypassByRule = 1,
	BypassNotInHandleList = 2
};

struct IpFilterDecision
{
	IpFilterAction action = IpFilterAction::Proxy;
	std::string matchedRule;
};

bool ParseIpCidrRule(std::wstring_view text, IpCidrRule& outRule);
bool MatchesIpCidrRule(const struct sockaddr* addr, const IpCidrRule& rule);
bool FormatNormalizedIpPort(const struct sockaddr* addr, std::string& outIp, uint16_t& outPort);
IpFilterDecision EvaluateIpFilter(const struct sockaddr* remoteAddr, const std::vector<IpCidrRule>& bypassRules, const std::vector<IpCidrRule>& handleRules);
