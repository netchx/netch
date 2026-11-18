using Netch.Enums;
using Netch.Models.Modes;

namespace Netch.Services;

/// <summary>
///     Notifications from the core to whichever UI is attached (WinForms today, WinUI later).
///     The core never touches UI types; front ends subscribe to these events and marshal to
///     their own UI thread. Handlers may be invoked from any thread.
/// </summary>
public static class AppEvents
{
    private static State _state = State.Waiting;

    /// <summary>Current run state, owned by the front end that drives start/stop.</summary>
    public static State State
    {
        get => _state;
        set
        {
            if (_state == value)
                return;

            _state = value;
            StateChanged?.Invoke(value);
        }
    }

    public static event Action<State>? StateChanged;

    /// <summary>Transient progress text, e.g. "Starting sing-box".</summary>
    public static event Action<string>? StatusReported;

    /// <summary>User-facing notification (tray balloon / toast). Second argument: true = info, false = error.</summary>
    public static event Action<string, bool>? NotificationRequested;

    /// <summary>The mode collection in <see cref="Global.Modes" /> was reloaded or re-sorted.</summary>
    public static event Action? ModesChanged;

    /// <summary>A single mode was removed from <see cref="Global.Modes" />.</summary>
    public static event Action<Mode>? ModeDeleted;

    /// <summary>Traffic counters became available (true) or were reset (false).</summary>
    public static event Action<bool>? BandwidthAvailabilityChanged;

    /// <summary>Total bytes transferred in the current session, reported about once per second.</summary>
    public static event Action<ulong>? BandwidthUpdated;

    /// <summary>Share mode (pcap2socks) console output session: opened, shown after start, closed on stop.</summary>
    public static event Action? ShareModeLogOpened;

    public static event Action? ShareModeLogShown;

    public static event Action? ShareModeLogClosed;

    public static event Action<string>? ShareModeLogLine;

    public static void ReportStatus(string text) => StatusReported?.Invoke(text);

    public static void Notify(string text, bool info = true) => NotificationRequested?.Invoke(text, info);

    public static void RaiseModesChanged() => ModesChanged?.Invoke();

    public static void RaiseModeDeleted(Mode mode) => ModeDeleted?.Invoke(mode);

    public static void SetBandwidthAvailable(bool available) => BandwidthAvailabilityChanged?.Invoke(available);

    public static void ReportBandwidth(ulong totalBytes) => BandwidthUpdated?.Invoke(totalBytes);

    public static void OpenShareModeLog() => ShareModeLogOpened?.Invoke();

    public static void ShowShareModeLog() => ShareModeLogShown?.Invoke();

    public static void CloseShareModeLog() => ShareModeLogClosed?.Invoke();

    public static void AppendShareModeLog(string line) => ShareModeLogLine?.Invoke(line);
}
