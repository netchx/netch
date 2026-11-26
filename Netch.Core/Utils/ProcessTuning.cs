using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Netch.Utils;

/// <summary>
///     Scheduling tweaks for the processes on the forwarding path while
///     acceleration is running. Games keep every core busy; at normal priority
///     the proxy core competes with game threads and packets wait in run queues.
///     Hidden or minimized processes are also candidates for Windows power
///     throttling (EcoQoS), which lowers clock speed and timer precision.
/// </summary>
public static class ProcessTuning
{
    private const int ProcessPowerThrottling = 4;
    private const uint ProcessPowerThrottlingCurrentVersion = 1;
    private const uint ThrottlingExecutionSpeed = 0x1;
    private const uint ThrottlingIgnoreTimerResolution = 0x4;

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessPowerThrottlingState
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessInformation(IntPtr hProcess,
        int processInformationClass,
        ref ProcessPowerThrottlingState processInformation,
        uint processInformationSize);

    public static bool Enabled => Global.Settings.GameProcessBoost;

    /// <summary>
    ///     Raise <paramref name="process" /> to AboveNormal and opt it out of power
    ///     throttling. A process already running above normal is left alone.
    /// </summary>
    public static void Boost(Process process, string name)
    {
        if (!Enabled)
            return;

        try
        {
            if (process.HasExited)
                return;

            if (process.PriorityClass is ProcessPriorityClass.Normal or ProcessPriorityClass.BelowNormal or ProcessPriorityClass.Idle)
                process.PriorityClass = ProcessPriorityClass.AboveNormal;

            SetPowerThrottling(process.Handle, false);
            Log.Debug("Tuned process {Name} ({Id}) for low latency", name, process.Id);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Tune process {Name} failed", name);
        }
    }

    /// <summary>
    ///     Undo <see cref="Boost" /> for a process that keeps running after
    ///     acceleration stops (Netch itself).
    /// </summary>
    public static void Restore(Process process)
    {
        try
        {
            if (process.HasExited)
                return;

            if (process.PriorityClass == ProcessPriorityClass.AboveNormal)
                process.PriorityClass = ProcessPriorityClass.Normal;

            SetPowerThrottling(process.Handle, true);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Restore process priority failed");
        }
    }

    private static void SetPowerThrottling(IntPtr handle, bool systemManaged)
    {
        // ControlMask = 0 hands the decision back to Windows; setting a flag in
        // ControlMask with the same flag cleared in StateMask opts out.
        var state = new ProcessPowerThrottlingState
        {
            Version = ProcessPowerThrottlingCurrentVersion,
            ControlMask = systemManaged ? 0 : ThrottlingExecutionSpeed | ThrottlingIgnoreTimerResolution,
            StateMask = 0
        };

        var size = (uint)Marshal.SizeOf<ProcessPowerThrottlingState>();
        if (SetProcessInformation(handle, ProcessPowerThrottling, ref state, size) || systemManaged)
            return;

        // Windows 10 does not know the timer resolution flag; retry without it.
        state.ControlMask = ThrottlingExecutionSpeed;
        if (!SetProcessInformation(handle, ProcessPowerThrottling, ref state, size))
            Log.Debug("SetProcessInformation(ProcessPowerThrottling) failed: {Error}", Marshal.GetLastWin32Error());
    }
}
