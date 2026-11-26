#include "IpFilter.h"

#include <winsock2.h>
#include <ws2tcpip.h>

#include <cstring>

namespace
{
	std::wstring_view TrimWhitespace(std::wstring_view sv)
	{
		while (!sv.empty() && (sv.front() == L' ' || sv.front() == L'\t' || sv.front() == L'\r' || sv.front() == L'\n'))
			sv.remove_prefix(1);
		while (!sv.empty() && (sv.back() == L' ' || sv.back() == L'\t' || sv.back() == L'\r' || sv.back() == L'\n'))
			sv.remove_suffix(1);
		return sv;
	}

	bool ParsePrefixLength(std::wstring_view text, uint8_t maxPrefix, uint8_t& outPrefix)
	{
		if (text.empty() || text.size() > 3)
			return false;

		uint32_t value = 0;
		for (wchar_t ch : text)
		{
			if (ch < L'0' || ch > L'9')
				return false;
			value = value * 10u + static_cast<uint32_t>(ch - L'0');
			if (value > maxPrefix)
				return false;
		}

		outPrefix = static_cast<uint8_t>(value);
		return true;
	}

	void ApplyPrefixMask(std::array<uint8_t, 16>& bytes, size_t byteLen, uint8_t prefixLength)
	{
		const size_t fullBytes = prefixLength / 8;
		const uint8_t remBits = prefixLength % 8;

		if (fullBytes < byteLen)
		{
			size_t zeroStart = fullBytes;
			if (remBits != 0)
			{
				const uint8_t mask = static_cast<uint8_t>(0xFFu << (8u - remBits));
				bytes[fullBytes] &= mask;
				zeroStart = fullBytes + 1;
			}
			for (size_t i = zeroStart; i < byteLen; ++i)
				bytes[i] = 0;
		}

		for (size_t i = byteLen; i < bytes.size(); ++i)
			bytes[i] = 0;
	}

	bool ExtractNormalizedAddress(const struct sockaddr* addr, uint16_t& outFamily, std::array<uint8_t, 16>& outBytes, uint16_t& outPort)
	{
		if (addr == nullptr)
			return false;

		outBytes.fill(0);

		if (addr->sa_family == AF_INET)
		{
			const auto* sin = reinterpret_cast<const sockaddr_in*>(addr);
			std::memcpy(outBytes.data(), &sin->sin_addr, 4);
			outFamily = AF_INET;
			outPort = ntohs(sin->sin_port);
			return true;
		}

		if (addr->sa_family == AF_INET6)
		{
			const auto* sin6 = reinterpret_cast<const sockaddr_in6*>(addr);
			if (IN6_IS_ADDR_V4MAPPED(&sin6->sin6_addr))
			{
				std::memcpy(outBytes.data(), sin6->sin6_addr.u.Byte + 12, 4);
				outFamily = AF_INET;
			}
			else
			{
				std::memcpy(outBytes.data(), &sin6->sin6_addr, 16);
				outFamily = AF_INET6;
			}
			outPort = ntohs(sin6->sin6_port);
			return true;
		}

		return false;
	}

	bool MatchesNormalized(uint16_t family, const std::array<uint8_t, 16>& addrBytes, const IpCidrRule& rule)
	{
		if (family != rule.family)
			return false;

		const size_t fullBytes = rule.prefixLength / 8;
		const uint8_t remBits = rule.prefixLength % 8;

		for (size_t i = 0; i < fullBytes; ++i)
		{
			if (addrBytes[i] != rule.networkBytes[i])
				return false;
		}

		if (remBits != 0)
		{
			const uint8_t mask = static_cast<uint8_t>(0xFFu << (8u - remBits));
			if ((addrBytes[fullBytes] & mask) != rule.networkBytes[fullBytes])
				return false;
		}

		return true;
	}

	std::string FormatCanonicalCidr(uint16_t family, const std::array<uint8_t, 16>& bytes, uint8_t prefixLength)
	{
		char addrBuf[INET6_ADDRSTRLEN]{};
		if (InetNtopA(family, bytes.data(), addrBuf, sizeof(addrBuf)) == nullptr)
			return {};

		return std::string(addrBuf) + "/" + std::to_string(prefixLength);
	}
}

bool ParseIpCidrRule(std::wstring_view text, IpCidrRule& outRule)
{
	text = TrimWhitespace(text);
	if (text.empty())
		return false;

	const size_t slashPos = text.find(L'/');
	std::wstring_view addrPart = (slashPos == std::wstring_view::npos) ? text : text.substr(0, slashPos);
	const bool hasPrefix = (slashPos != std::wstring_view::npos);
	std::wstring_view prefixPart = hasPrefix ? text.substr(slashPos + 1) : std::wstring_view{};

	if (hasPrefix && prefixPart.find(L'/') != std::wstring_view::npos)
		return false;

	addrPart = TrimWhitespace(addrPart);
	if (addrPart.size() > 2 && addrPart.front() == L'[' && addrPart.back() == L']')
	{
		addrPart.remove_prefix(1);
		addrPart.remove_suffix(1);
	}

	if (addrPart.empty())
		return false;

	const std::wstring addrWide(addrPart);

	IN_ADDR in4{};
	if (InetPtonW(AF_INET, addrWide.c_str(), &in4) == 1)
	{
		uint8_t prefix = 32;
		if (hasPrefix && !ParsePrefixLength(TrimWhitespace(prefixPart), 32, prefix))
			return false;

		outRule = {};
		outRule.family = AF_INET;
		outRule.prefixLength = prefix;
		std::memcpy(outRule.networkBytes.data(), &in4, 4);
		ApplyPrefixMask(outRule.networkBytes, 4, prefix);
		outRule.canonicalText = FormatCanonicalCidr(AF_INET, outRule.networkBytes, prefix);
		return !outRule.canonicalText.empty();
	}

	IN6_ADDR in6{};
	if (InetPtonW(AF_INET6, addrWide.c_str(), &in6) == 1)
	{
		uint8_t prefix = 128;
		if (hasPrefix && !ParsePrefixLength(TrimWhitespace(prefixPart), 128, prefix))
			return false;

		outRule = {};
		if (IN6_IS_ADDR_V4MAPPED(&in6) && prefix >= 96)
		{
			const uint8_t mappedPrefix = static_cast<uint8_t>(prefix - 96);
			outRule.family = AF_INET;
			outRule.prefixLength = mappedPrefix;
			std::memcpy(outRule.networkBytes.data(), in6.u.Byte + 12, 4);
			ApplyPrefixMask(outRule.networkBytes, 4, mappedPrefix);
			outRule.canonicalText = FormatCanonicalCidr(AF_INET, outRule.networkBytes, mappedPrefix);
			return !outRule.canonicalText.empty();
		}

		outRule.family = AF_INET6;
		outRule.prefixLength = prefix;
		std::memcpy(outRule.networkBytes.data(), &in6, 16);
		ApplyPrefixMask(outRule.networkBytes, 16, prefix);
		outRule.canonicalText = FormatCanonicalCidr(AF_INET6, outRule.networkBytes, prefix);
		return !outRule.canonicalText.empty();
	}

	return false;
}

bool MatchesIpCidrRule(const struct sockaddr* addr, const IpCidrRule& rule)
{
	uint16_t family = 0;
	std::array<uint8_t, 16> addrBytes{};
	uint16_t port = 0;
	if (!ExtractNormalizedAddress(addr, family, addrBytes, port))
		return false;

	return MatchesNormalized(family, addrBytes, rule);
}

bool FormatNormalizedIpPort(const struct sockaddr* addr, std::string& outIp, uint16_t& outPort)
{
	uint16_t family = 0;
	std::array<uint8_t, 16> addrBytes{};
	if (!ExtractNormalizedAddress(addr, family, addrBytes, outPort))
		return false;

	char addrBuf[INET6_ADDRSTRLEN]{};
	if (InetNtopA(family, addrBytes.data(), addrBuf, sizeof(addrBuf)) == nullptr)
		return false;

	outIp.assign(addrBuf);
	return true;
}

IpFilterDecision EvaluateIpFilter(const struct sockaddr* remoteAddr, const std::vector<IpCidrRule>& bypassRules, const std::vector<IpCidrRule>& handleRules)
{
	uint16_t family = 0;
	std::array<uint8_t, 16> addrBytes{};
	uint16_t port = 0;
	if (!ExtractNormalizedAddress(remoteAddr, family, addrBytes, port))
		return {};

	for (const auto& rule : bypassRules)
	{
		if (MatchesNormalized(family, addrBytes, rule))
			return { IpFilterAction::BypassByRule, rule.canonicalText };
	}

	if (!handleRules.empty())
	{
		for (const auto& rule : handleRules)
		{
			if (MatchesNormalized(family, addrBytes, rule))
				return { IpFilterAction::Proxy, rule.canonicalText };
		}

		return { IpFilterAction::BypassNotInHandleList, {} };
	}

	return { IpFilterAction::Proxy, {} };
}
