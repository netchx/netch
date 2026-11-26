using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using Netch.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Netch.Services;

public sealed record SystemProxySnapshot(
    uint Flags,
    string? ProxyServer,
    string? ProxyBypass,
    string? AutoConfigUrl)
{
    public const uint ProxyTypeDirect = 0x1;
    public const uint ProxyTypeProxy = 0x2;
    public const uint ProxyTypeAutoProxyUrl = 0x4;

    public bool IsGlobalProxy => (Flags & ProxyTypeProxy) != 0 && !string.IsNullOrWhiteSpace(ProxyServer);

    public bool IsPac => (Flags & ProxyTypeAutoProxyUrl) != 0 && !string.IsNullOrWhiteSpace(AutoConfigUrl);

    public bool IsDirect => !IsGlobalProxy && !IsPac && ((Flags & ProxyTypeDirect) != 0 || Flags == 0);
}

public interface ISystemProxyService
{
    SystemProxySnapshot Query();

    void SetGlobal(string proxyServer, string? bypass);

    void SetPac(string pacUrl);

    void SetDirect();

    void Restore(SystemProxySnapshot snapshot);
}

public sealed class WinInetSystemProxyService : ISystemProxyService
{
    private const uint InternetOptionRefresh = 37;
    private const uint InternetOptionSettingsChanged = 39;
    private const uint InternetOptionPerConnectionOption = 75;

    private const uint InternetPerConnectionFlags = 1;
    private const uint InternetPerConnectionProxyServer = 2;
    private const uint InternetPerConnectionProxyBypass = 3;
    private const uint InternetPerConnectionAutoConfigUrl = 4;
    private const uint InternetPerConnectionFlagsUi = 10;

    public static readonly WinInetSystemProxyService Instance = new();

    private WinInetSystemProxyService()
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InternetPerConnectionOptionValue
    {
        [FieldOffset(0)]
        public uint dwValue;

        [FieldOffset(0)]
        public IntPtr pszValue;

        [FieldOffset(0)]
        public long ftValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InternetPerConnectionOption
    {
        public uint dwOption;
        public InternetPerConnectionOptionValue Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InternetPerConnectionOptionList
    {
        public uint dwSize;
        public IntPtr pszConnection;
        public uint dwOptionCount;
        public uint dwOptionError;
        public IntPtr pOptions;
    }

    public SystemProxySnapshot Query()
    {
        if (!TryQueryOptions(InternetPerConnectionFlagsUi, out var snapshot) &&
            !TryQueryOptions(InternetPerConnectionFlags, out snapshot))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to query Windows system proxy settings.");
        }

        return snapshot;
    }

    public void SetGlobal(string proxyServer, string? bypass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyServer);
        ApplyOptions(
            SystemProxySnapshot.ProxyTypeProxy | SystemProxySnapshot.ProxyTypeDirect,
            proxyServer,
            bypass ?? string.Empty,
            string.Empty);
    }

    public void SetPac(string pacUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pacUrl);
        ApplyOptions(
            new (uint Option, uint? DwValue, string? StrValue)[]
            {
                (InternetPerConnectionFlags, SystemProxySnapshot.ProxyTypeAutoProxyUrl | SystemProxySnapshot.ProxyTypeDirect, null),
                (InternetPerConnectionAutoConfigUrl, null, pacUrl)
            });
    }

    public void SetDirect()
    {
        ApplyOptions(
            SystemProxySnapshot.ProxyTypeDirect,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    public void Restore(SystemProxySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var flags = snapshot.Flags == 0 ? SystemProxySnapshot.ProxyTypeDirect : snapshot.Flags;
        ApplyOptions(
            flags,
            snapshot.ProxyServer ?? string.Empty,
            snapshot.ProxyBypass ?? string.Empty,
            snapshot.AutoConfigUrl ?? string.Empty);
    }

    private static unsafe bool TryQueryOptions(uint flagsOption, out SystemProxySnapshot snapshot)
    {
        var options = new InternetPerConnectionOption[4];
        options[0].dwOption = flagsOption;
        options[1].dwOption = InternetPerConnectionProxyServer;
        options[2].dwOption = InternetPerConnectionProxyBypass;
        options[3].dwOption = InternetPerConnectionAutoConfigUrl;

        try
        {
            fixed (InternetPerConnectionOption* pOptions = options)
            {
                var list = new InternetPerConnectionOptionList
                {
                    dwSize = (uint)sizeof(InternetPerConnectionOptionList),
                    pszConnection = IntPtr.Zero,
                    dwOptionCount = (uint)options.Length,
                    dwOptionError = 0,
                    pOptions = (IntPtr)pOptions
                };

                var size = list.dwSize;
                if (!PInvoke.InternetQueryOption(null, InternetOptionPerConnectionOption, &list, &size))
                {
                    snapshot = new SystemProxySnapshot(SystemProxySnapshot.ProxyTypeDirect, null, null, null);
                    return false;
                }
            }

            var flags = options[0].Value.dwValue;
            var server = Marshal.PtrToStringUni(options[1].Value.pszValue);
            var bypass = Marshal.PtrToStringUni(options[2].Value.pszValue);
            var autoConfig = Marshal.PtrToStringUni(options[3].Value.pszValue);
            snapshot = new SystemProxySnapshot(flags, server, bypass, autoConfig);
            return true;
        }
        finally
        {
            for (var i = 1; i < options.Length; i++)
            {
                var ptr = options[i].Value.pszValue;
                if (ptr != IntPtr.Zero)
                    PInvoke.GlobalFree(new HGLOBAL((void*)ptr));
            }
        }
    }

    private static void ApplyOptions(uint flags, string proxyServer, string proxyBypass, string autoConfigUrl)
    {
        ApplyOptions(
            [
                (InternetPerConnectionFlags, flags, null),
                (InternetPerConnectionProxyServer, null, proxyServer),
                (InternetPerConnectionProxyBypass, null, proxyBypass),
                (InternetPerConnectionAutoConfigUrl, null, autoConfigUrl)
            ]);
    }

    private static unsafe void ApplyOptions((uint Option, uint? DwValue, string? StrValue)[] specs)
    {
        var options = new InternetPerConnectionOption[specs.Length];
        var allocated = new List<IntPtr>(specs.Length);

        try
        {
            for (var i = 0; i < specs.Length; i++)
            {
                options[i].dwOption = specs[i].Option;
                if (specs[i].DwValue is { } dw)
                {
                    options[i].Value.dwValue = dw;
                }
                else
                {
                    var ptr = Marshal.StringToHGlobalUni(specs[i].StrValue ?? string.Empty);
                    allocated.Add(ptr);
                    options[i].Value.pszValue = ptr;
                }
            }

            fixed (InternetPerConnectionOption* pOptions = options)
            {
                var list = new InternetPerConnectionOptionList
                {
                    dwSize = (uint)sizeof(InternetPerConnectionOptionList),
                    pszConnection = IntPtr.Zero,
                    dwOptionCount = (uint)options.Length,
                    dwOptionError = 0,
                    pOptions = (IntPtr)pOptions
                };

                if (!PInvoke.InternetSetOption(null, InternetOptionPerConnectionOption, &list, list.dwSize))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to update Windows system proxy settings.");
            }

            PInvoke.InternetSetOption(null, InternetOptionSettingsChanged, null, 0);
            PInvoke.InternetSetOption(null, InternetOptionRefresh, null, 0);
        }
        finally
        {
            foreach (var ptr in allocated)
                Marshal.FreeHGlobal(ptr);
        }
    }
}

public sealed record SystemProxyRecoveryState(
    SystemProxySnapshot Previous,
    string AppliedKind,
    string AppliedEndpoint);

public static class SystemProxyStateStore
{
    public static string DefaultStatePath => Path.Combine(Configuration.DataDirectoryFullName, "system-proxy.state.json");

    public static void SaveState(SystemProxyRecoveryState state, string? stateFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        var path = string.IsNullOrWhiteSpace(stateFilePath) ? DefaultStatePath : stateFilePath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var temp = path + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(state, Global.NewCustomJsonSerializerOptions()));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    public static void ClearState(string? stateFilePath = null)
    {
        var path = string.IsNullOrWhiteSpace(stateFilePath) ? DefaultStatePath : stateFilePath;
        if (File.Exists(path))
            File.Delete(path);
    }

    public static bool MatchesApplied(SystemProxySnapshot current, string appliedKind, string appliedEndpoint)
    {
        if (string.Equals(appliedKind, "Global", StringComparison.OrdinalIgnoreCase))
            return current.IsGlobalProxy && string.Equals(current.ProxyServer, appliedEndpoint, StringComparison.OrdinalIgnoreCase);

        if (string.Equals(appliedKind, "Pac", StringComparison.OrdinalIgnoreCase))
            return current.IsPac && string.Equals(current.AutoConfigUrl, appliedEndpoint, StringComparison.OrdinalIgnoreCase);

        return false;
    }

    public static bool IsOwnedProxy(SystemProxySnapshot snapshot, string appliedEndpoint)
    {
        if (snapshot.IsGlobalProxy && string.Equals(snapshot.ProxyServer, appliedEndpoint, StringComparison.OrdinalIgnoreCase))
            return true;

        if (snapshot.IsPac && snapshot.AutoConfigUrl is { } pac)
        {
            if (string.Equals(pac, appliedEndpoint, StringComparison.OrdinalIgnoreCase))
                return true;

            if (Uri.TryCreate(pac, UriKind.Absolute, out var uri) &&
                uri.IsLoopback &&
                uri.AbsolutePath.TrimEnd('/').Equals("/pac", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static void RestoreSafely(ISystemProxyService service, SystemProxyRecoveryState state, string? stateFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            var current = service.Query();
            if (!MatchesApplied(current, state.AppliedKind, state.AppliedEndpoint))
            {
                Log.Information("System proxy was modified externally; skipping restore.");
                return;
            }

            if (IsOwnedProxy(state.Previous, state.AppliedEndpoint))
                service.SetDirect();
            else
                service.Restore(state.Previous);
        }
        finally
        {
            ClearState(stateFilePath);
        }
    }

    public static void RecoverStaleState(ISystemProxyService? service = null, string? stateFilePath = null)
    {
        var path = string.IsNullOrWhiteSpace(stateFilePath) ? DefaultStatePath : stateFilePath;
        if (!File.Exists(path))
            return;

        try
        {
            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<SystemProxyRecoveryState>(json, Global.NewCustomJsonSerializerOptions());
            if (state == null)
            {
                ClearState(path);
                return;
            }

            RestoreSafely(service ?? WinInetSystemProxyService.Instance, state, path);
            Log.Information("Recovered stale system proxy state from previous session.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to recover stale system proxy state");
            try { ClearState(path); }
            catch (IOException) { }
        }
    }
}
