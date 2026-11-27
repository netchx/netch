using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.ShareMode;
using Netch.Models.Modes.TunMode;
using Netch.Models.Modes.WebProxyMode;
using Netch.Utils;

namespace Netch.Services;

public sealed record ModeComposition(
    IReadOnlyList<Mode> SourceModes,
    IReadOnlyList<Mode> EffectiveModes,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<Redirector> SourceProcessModes,
    Redirector? EffectiveProcessMode,
    TunMode? EffectiveTunMode,
    WebProxyMode? EffectiveWebProxyMode,
    ShareMode? EffectiveShareMode);

public static class ModeComposer
{
    public static bool CanAddMode(IReadOnlyList<Mode> selected, Mode candidate, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(candidate);

        if (selected.Any(m => IsSameMode(m, candidate)))
        {
            reason = null;
            return true;
        }

        if ((selected.Any(m => m.Type == ModeType.TunMode) && candidate.Type != ModeType.TunMode) ||
            (candidate.Type == ModeType.TunMode && selected.Any(m => m.Type != ModeType.TunMode)))
        {
            reason = i18N.Translate("TUN mode cannot be combined with other mode types.");
            return false;
        }

        if (candidate.Type == ModeType.WebProxyMode && selected.Any(m => m.Type == ModeType.WebProxyMode))
        {
            reason = i18N.Translate("Only one Web Proxy mode can be selected.");
            return false;
        }

        if (candidate.Type == ModeType.ShareMode && selected.Any(m => m.Type == ModeType.ShareMode))
        {
            reason = i18N.Translate("Only one Share mode can be selected.");
            return false;
        }

        reason = null;
        return true;
    }

    public static bool ValidateSelection(IReadOnlyList<Mode>? modes, out string? error)
    {
        if (modes == null || modes.Count == 0)
        {
            error = i18N.Translate("Select at least one mode.");
            return false;
        }

        var unique = DeduplicateModes(modes);
        if (unique.Count == 0)
        {
            error = i18N.Translate("Select at least one mode.");
            return false;
        }

        var hasTun = unique.Any(m => m.Type == ModeType.TunMode);
        var hasNonTun = unique.Any(m => m.Type != ModeType.TunMode);
        if (hasTun && hasNonTun)
        {
            error = i18N.Translate("TUN mode cannot be combined with other mode types.");
            return false;
        }

        if (unique.Count(m => m.Type == ModeType.WebProxyMode) > 1)
        {
            error = i18N.Translate("Only one Web Proxy mode can be selected.");
            return false;
        }

        if (unique.Count(m => m.Type == ModeType.ShareMode) > 1)
        {
            error = i18N.Translate("Only one Share mode can be selected.");
            return false;
        }

        error = null;
        return true;
    }

    public static ModeComposition Compose(IReadOnlyList<Mode> modes)
    {
        if (!ValidateSelection(modes, out var error))
            throw new MessageException(error ?? i18N.Translate("Select at least one mode."));

        var sourceModes = DeduplicateModes(modes);
        var processModes = sourceModes.OfType<Redirector>().ToArray();
        var tunModes = sourceModes.OfType<TunMode>().ToArray();
        var webMode = sourceModes.OfType<WebProxyMode>().FirstOrDefault();
        var shareMode = sourceModes.OfType<ShareMode>().FirstOrDefault();

        var warnings = new List<string>();
        var effectiveProcess = processModes.Length > 0
            ? MergeProcessModes(processModes, webMode != null, warnings)
            : null;
        var effectiveTun = tunModes.Length > 0
            ? MergeTunModes(tunModes, warnings)
            : null;

        var effectiveModes = new List<Mode>(4);
        if (effectiveProcess != null)
            effectiveModes.Add(effectiveProcess);
        if (effectiveTun != null)
            effectiveModes.Add(effectiveTun);
        if (webMode != null)
            effectiveModes.Add(webMode);
        if (shareMode != null)
            effectiveModes.Add(shareMode);

        return new ModeComposition(
            sourceModes,
            effectiveModes,
            warnings,
            processModes,
            effectiveProcess,
            effectiveTun,
            webMode,
            shareMode);
    }

    private static Redirector MergeProcessModes(IReadOnlyList<Redirector> list, bool hasWebProxy, List<string> warnings)
    {
        if (list.Count == 1)
        {
            var single = list[0];
            if (!hasWebProxy || !single.FilterLoopback)
                return single;

            warnings.Add(i18N.Translate("Loopback filtering was disabled to prevent conflicts with Web Proxy mode."));
            return CloneRedirectorWithLoopbackDisabled(single);
        }

        var handle = new List<string>();
        var bypass = new List<string>();
        foreach (var mode in list)
        {
            foreach (var raw in mode.Handle)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var rule = raw.Trim();
                RemoveIgnoreCase(bypass, rule);
                AddUniqueIgnoreCase(handle, rule);
            }

            foreach (var raw in mode.Bypass)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var rule = raw.Trim();
                RemoveIgnoreCase(handle, rule);
                AddUniqueIgnoreCase(bypass, rule);
            }
        }

        var processHandlers = list.Where(m => m.Handle.Count > 0).ToList();
        var anyHasHandleIps = list.Any(m => m.HandleIPs.Count > 0);
        var hasUnrestrictedProcessHandler = processHandlers.Any(m => m.HandleIPs.Count == 0);
        var includeHandleIps = anyHasHandleIps && !hasUnrestrictedProcessHandler;
        if (anyHasHandleIps && hasUnrestrictedProcessHandler)
            warnings.Add(i18N.Translate("IP allowlists were relaxed to all IPs because another selected process mode proxies all IPs."));

        var handleIps = new List<string>();
        var bypassIps = new List<string>();
        foreach (var mode in list)
        {
            if (includeHandleIps)
            {
                foreach (var raw in mode.HandleIPs)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;
                    var normalized = NormalizeIpOrRaw(raw);
                    RemoveIgnoreCase(bypassIps, normalized);
                    AddUniqueIgnoreCase(handleIps, normalized);
                }
            }

            foreach (var raw in mode.BypassIPs)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var normalized = NormalizeIpOrRaw(raw);
                RemoveIgnoreCase(handleIps, normalized);
                AddUniqueIgnoreCase(bypassIps, normalized);
            }
        }

        var defaults = Global.Settings.Redirector;
        var filterDns = list.Any(m => m.FilterDNS ?? defaults.FilterDNS);
        var filterIcmp = list.Any(m => m.FilterICMP ?? defaults.FilterICMP);
        var anyLoopback = list.Any(m => m.FilterLoopback);
        if (hasWebProxy && anyLoopback)
            warnings.Add(i18N.Translate("Loopback filtering was disabled to prevent conflicts with Web Proxy mode."));

        var dnsHosts = new List<string>();
        var seenDnsHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in list)
        {
            if (!(m.FilterDNS ?? defaults.FilterDNS) || string.IsNullOrWhiteSpace(m.DNSHost))
                continue;

            var host = m.DNSHost.Trim();
            if (seenDnsHosts.Add(host))
                dnsHosts.Add(host);
        }

        if (dnsHosts.Count > 1)
            warnings.Add(i18N.Translate("Multiple DNS hosts were configured; using the first custom value."));

        var defaultDnsHost = defaults.DNSHost?.Trim();
        var chosenDnsHost = dnsHosts.FirstOrDefault(h => !string.Equals(h, defaultDnsHost, StringComparison.OrdinalIgnoreCase))
                            ?? dnsHosts.FirstOrDefault()
                            ?? defaults.DNSHost;

        var icmpDelays = new List<int>();
        foreach (var m in list)
        {
            if ((m.FilterICMP ?? defaults.FilterICMP) && m.ICMPDelay is { } delay)
                icmpDelays.Add(delay);
        }

        var chosenIcmpDelay = icmpDelays.Cast<int?>().FirstOrDefault(d => d != defaults.ICMPDelay)
                              ?? icmpDelays.Cast<int?>().FirstOrDefault()
                              ?? defaults.ICMPDelay;

        var merged = new Redirector
        {
            FullName = string.Empty,
            FilterTCP = list.Any(m => m.FilterTCP ?? defaults.FilterTCP),
            FilterUDP = list.Any(m => m.FilterUDP ?? defaults.FilterUDP),
            FilterICMP = filterIcmp,
            FilterDNS = filterDns,
            FilterParent = list.Any(m => m.FilterParent ?? defaults.FilterParent),
            DNSProxy = list.Any(m => m.DNSProxy ?? defaults.DNSProxy),
            HandleOnlyDNS = list.All(m => m.HandleOnlyDNS ?? defaults.HandleOnlyDNS),
            FilterLoopback = !hasWebProxy && anyLoopback,
            FilterIntranet = list.Any(m => m.FilterIntranet),
            DNSHost = chosenDnsHost,
            ICMPDelay = chosenIcmpDelay,
            Handle = handle,
            Bypass = bypass,
            HandleIPs = handleIps,
            BypassIPs = bypassIps
        };

        var title = string.Join(" + ", list.Select(m => m.i18NRemark));
        merged.Remark["en"] = title;
        merged.Remark[i18N.LangCode] = title;
        return merged;
    }

    private static TunMode MergeTunModes(IReadOnlyList<TunMode> list, List<string> warnings)
    {
        if (list.Count == 1)
            return list[0];

        var firstNet = list[0].Network ?? Global.Settings.TUNTAP;
        var hasAdapterConflict = list.Skip(1).Any(m => !AreTunAdapterSettingsEqual(firstNet, m.Network ?? Global.Settings.TUNTAP));
        if (hasAdapterConflict)
            warnings.Add(i18N.Translate("TUN adapter settings differ across modes; using the first mode's network configuration."));

        var networkBypassIps = new List<string>();
        var seenNetBypass = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in list)
        {
            var net = m.Network ?? Global.Settings.TUNTAP;
            foreach (var raw in net.BypassIPs)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var normalized = NormalizeIpOrRaw(raw);
                if (seenNetBypass.Add(normalized))
                    networkBypassIps.Add(normalized);
            }
        }

        var mergedNetwork = new TUNConfig
        {
            Address = firstNet.Address,
            DNS = firstNet.DNS,
            Gateway = firstNet.Gateway,
            Netmask = firstNet.Netmask,
            EnableIPv6 = firstNet.EnableIPv6,
            IPv6Address = firstNet.IPv6Address,
            IPv6Gateway = firstNet.IPv6Gateway,
            IPv6PrefixLength = firstNet.IPv6PrefixLength,
            ProxyDNS = firstNet.ProxyDNS,
            UseCustomDNS = firstNet.UseCustomDNS,
            BypassIPs = networkBypassIps
        };

        var handle = new List<string>();
        var seenHandle = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bypass = new List<string>();
        var seenBypass = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mode in list)
        {
            foreach (var raw in mode.Handle)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var normalized = NormalizeIpOrRaw(raw);
                if (seenHandle.Add(normalized))
                    handle.Add(normalized);
            }

            foreach (var raw in mode.Bypass)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var normalized = NormalizeIpOrRaw(raw);
                if (seenBypass.Add(normalized))
                    bypass.Add(normalized);
            }
        }

        if (handle.RemoveAll(h => seenBypass.Contains(h)) > 0)
            warnings.Add(i18N.Translate("Conflicting TUN routes were resolved in favor of bypass rules."));

        var merged = new TunMode
        {
            FullName = string.Empty,
            Network = mergedNetwork,
            Handle = handle,
            Bypass = bypass
        };

        var title = string.Join(" + ", list.Select(m => m.i18NRemark));
        merged.Remark["en"] = title;
        merged.Remark[i18N.LangCode] = title;
        return merged;
    }

    private static Redirector CloneRedirectorWithLoopbackDisabled(Redirector source) => new()
    {
        FullName = source.FullName,
        Remark = new Dictionary<string, string>(source.Remark),
        FilterICMP = source.FilterICMP,
        FilterTCP = source.FilterTCP,
        FilterUDP = source.FilterUDP,
        FilterDNS = source.FilterDNS,
        FilterParent = source.FilterParent,
        ICMPDelay = source.ICMPDelay,
        DNSProxy = source.DNSProxy,
        HandleOnlyDNS = source.HandleOnlyDNS,
        DNSHost = source.DNSHost,
        FilterLoopback = false,
        FilterIntranet = source.FilterIntranet,
        Bypass = [.. source.Bypass],
        Handle = [.. source.Handle],
        BypassIPs = [.. source.BypassIPs],
        HandleIPs = [.. source.HandleIPs]
    };

    private static bool AreTunAdapterSettingsEqual(TUNConfig a, TUNConfig b) =>
        string.Equals(a.Address, b.Address, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.Netmask, b.Netmask, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.Gateway, b.Gateway, StringComparison.OrdinalIgnoreCase) &&
        a.EnableIPv6 == b.EnableIPv6 &&
        string.Equals(a.IPv6Address, b.IPv6Address, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.IPv6Gateway, b.IPv6Gateway, StringComparison.OrdinalIgnoreCase) &&
        a.IPv6PrefixLength == b.IPv6PrefixLength &&
        a.UseCustomDNS == b.UseCustomDNS &&
        a.ProxyDNS == b.ProxyDNS &&
        string.Equals(a.DNS, b.DNS, StringComparison.OrdinalIgnoreCase);

    private static List<Mode> DeduplicateModes(IReadOnlyList<Mode> modes)
    {
        var unique = new List<Mode>(modes.Count);
        foreach (var mode in modes)
        {
            if (mode == null)
                continue;
            if (!unique.Any(existing => IsSameMode(existing, mode)))
                unique.Add(mode);
        }
        return unique;
    }

    private static bool IsSameMode(Mode left, Mode right) =>
        ReferenceEquals(left, right) ||
        (!string.IsNullOrWhiteSpace(left.FullName) &&
         !string.IsNullOrWhiteSpace(right.FullName) &&
         string.Equals(left.FullName, right.FullName, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeIpOrRaw(string raw)
    {
        var trimmed = raw.Trim();
        return RouteUtils.TryNormalizeIpOrCidr(trimmed, out var normalized) ? normalized : trimmed;
    }

    private static void AddUniqueIgnoreCase(List<string> list, string value)
    {
        if (!list.Exists(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
            list.Add(value);
    }

    private static void RemoveIgnoreCase(List<string> list, string value)
    {
        list.RemoveAll(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
    }
}
