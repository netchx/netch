#include "Based.h"

atomic<bool> filterLoopback{ false };
atomic<bool> filterIntranet{ false };
atomic<bool> filterParent{ false };
atomic<bool> filterICMP{ true };
atomic<bool> filterTCP{ true };
atomic<bool> filterUDP{ true };
atomic<bool> filterDNS{ true };

atomic<DWORD> icmping{ 0 };

atomic<bool> dnsOnly{ false };
atomic<bool> dnsProx{ true };
string dnsHost = "1.1.1.1";
atomic<USHORT> dnsPort{ 53 };

wstring tgtHost = L"127.0.0.1";
wstring tgtPort = L"1080";
string tgtUsername = "";
string tgtPassword = "";
shared_mutex configLock;

atomic<bool> trackConnections{ true };
atomic<bool> trackUnmatched{ false };

// Rules are compiled when they are received from the managed UI. Matching
// against these objects avoids constructing a wregex for every socket event.
vector<ProcessRuleEntry> bypassList;
vector<ProcessRuleEntry> handleList;
vector<IpCidrRule> bypassIpList;
vector<IpCidrRule> handleIpList;
shared_mutex ruleLock;

namespace
{
	struct StagedRuleBatch
	{
		bool active = false;
		bool clearNames = false;
		bool clearIps = false;
		vector<ProcessRuleEntry> bypassNames;
		vector<ProcessRuleEntry> handleNames;
		vector<IpCidrRule> bypassIps;
		vector<IpCidrRule> handleIps;
	};

	mutex g_ruleStagingMutex;
	StagedRuleBatch g_stagedRules;

	string WidePatternToUtf8(const wchar_t* value)
	{
		if (value == nullptr || *value == L'\0')
			return {};

		const int needed = WideCharToMultiByte(CP_UTF8, 0, value, -1, nullptr, 0, nullptr, nullptr);
		if (needed <= 1)
			return {};

		string utf8(static_cast<size_t>(needed - 1), '\0');
		WideCharToMultiByte(CP_UTF8, 0, value, -1, utf8.data(), needed, nullptr, nullptr);
		return utf8;
	}

	RuleOpResult AddProcessRule(const wchar_t* value, bool isBypass)
	{
		if (value == nullptr)
			return RuleOpResult::Invalid;

		ProcessRuleEntry entry{};
		try
		{
			// Windows paths are case-insensitive; so are the rules.
			entry.regex = wregex(value, regex_constants::ECMAScript | regex_constants::icase | regex_constants::optimize);
			entry.patternUtf8 = WidePatternToUtf8(value);
		}
		catch (const regex_error&)
		{
			return RuleOpResult::Invalid;
		}

		lock_guard<mutex> stagingGuard(g_ruleStagingMutex);
		if (g_stagedRules.active)
		{
			auto& target = isBypass ? g_stagedRules.bypassNames : g_stagedRules.handleNames;
			target.emplace_back(std::move(entry));
			return RuleOpResult::Staged;
		}

		unique_lock<shared_mutex> ul(ruleLock);
		auto& liveTarget = isBypass ? bypassList : handleList;
		liveTarget.emplace_back(std::move(entry));
		return RuleOpResult::Applied;
	}

	RuleOpResult AddIpRule(const wchar_t* value, bool isBypass)
	{
		if (value == nullptr)
			return RuleOpResult::Invalid;

		IpCidrRule rule{};
		if (!ParseIpCidrRule(value, rule))
			return RuleOpResult::Invalid;

		lock_guard<mutex> stagingGuard(g_ruleStagingMutex);
		if (g_stagedRules.active)
		{
			auto& target = isBypass ? g_stagedRules.bypassIps : g_stagedRules.handleIps;
			target.emplace_back(std::move(rule));
			return RuleOpResult::Staged;
		}

		unique_lock<shared_mutex> ul(ruleLock);
		auto& liveTarget = isBypass ? bypassIpList : handleIpList;
		liveTarget.emplace_back(std::move(rule));
		return RuleOpResult::Applied;
	}
}

bool RuleBeginTransaction()
{
	lock_guard<mutex> stagingGuard(g_ruleStagingMutex);
	g_stagedRules = {};
	g_stagedRules.active = true;
	return true;
}

bool RuleCommitTransaction()
{
	lock_guard<mutex> stagingGuard(g_ruleStagingMutex);
	if (!g_stagedRules.active)
		return false;

	{
		unique_lock<shared_mutex> ul(ruleLock);

		if (g_stagedRules.clearNames)
		{
			bypassList = std::move(g_stagedRules.bypassNames);
			handleList = std::move(g_stagedRules.handleNames);
		}
		else
		{
			for (auto& entry : g_stagedRules.bypassNames)
				bypassList.emplace_back(std::move(entry));
			for (auto& entry : g_stagedRules.handleNames)
				handleList.emplace_back(std::move(entry));
		}

		if (g_stagedRules.clearIps)
		{
			bypassIpList = std::move(g_stagedRules.bypassIps);
			handleIpList = std::move(g_stagedRules.handleIps);
		}
		else
		{
			for (auto& rule : g_stagedRules.bypassIps)
				bypassIpList.emplace_back(std::move(rule));
			for (auto& rule : g_stagedRules.handleIps)
				handleIpList.emplace_back(std::move(rule));
		}
	}

	g_stagedRules = {};
	return true;
}

void RuleResetTransaction()
{
	lock_guard<mutex> stagingGuard(g_ruleStagingMutex);
	g_stagedRules = {};
}

RuleOpResult RuleClearNames()
{
	lock_guard<mutex> stagingGuard(g_ruleStagingMutex);
	if (g_stagedRules.active)
	{
		g_stagedRules.clearNames = true;
		g_stagedRules.bypassNames.clear();
		g_stagedRules.handleNames.clear();
		return RuleOpResult::Staged;
	}

	unique_lock<shared_mutex> ul(ruleLock);
	bypassList.clear();
	handleList.clear();
	return RuleOpResult::Applied;
}

RuleOpResult RuleAddBypassName(const wchar_t* value)
{
	return AddProcessRule(value, true);
}

RuleOpResult RuleAddHandleName(const wchar_t* value)
{
	return AddProcessRule(value, false);
}

RuleOpResult RuleClearIps()
{
	lock_guard<mutex> stagingGuard(g_ruleStagingMutex);
	if (g_stagedRules.active)
	{
		g_stagedRules.clearIps = true;
		g_stagedRules.bypassIps.clear();
		g_stagedRules.handleIps.clear();
		return RuleOpResult::Staged;
	}

	unique_lock<shared_mutex> ul(ruleLock);
	bypassIpList.clear();
	handleIpList.clear();
	return RuleOpResult::Applied;
}

RuleOpResult RuleAddBypassIp(const wchar_t* value)
{
	return AddIpRule(value, true);
}

RuleOpResult RuleAddHandleIp(const wchar_t* value)
{
	return AddIpRule(value, false);
}
