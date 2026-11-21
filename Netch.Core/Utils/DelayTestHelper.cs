using Microsoft.VisualStudio.Threading;
using Netch.Models;
using Netch.Services;
using Timer = System.Timers.Timer;

namespace Netch.Utils;

public static class DelayTestHelper
{
    private static readonly Timer Timer;

    private static readonly AsyncSemaphore Lock = new(1);

    private static readonly AsyncSemaphore PoolLock = new(16);

    public static readonly NumberRange Range = new(0, int.MaxValue / 1000);

    private static bool _enabled = true;

    static DelayTestHelper()
    {
        Timer = new Timer
        {
            Interval = 10000,
            AutoReset = true
        };

        Timer.Elapsed += (_, _) => PerformTestAsync().Forget();
    }

    public static bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            UpdateTick();
        }
    }

    /// <param name="waitFinish">if does not get lock, block until last release</param>
    public static async Task PerformTestAsync(bool waitFinish = false)
    {
        if (Lock.CurrentCount == 0)
        {
            if (waitFinish)
            {
                using var _ = await Lock.EnterAsync();
            }
            return;
        }

        using var guard = await Lock.EnterAsync();
        await RunTestsAsync(Global.Settings.Server.ToArray());
    }

    public static async Task PerformTestAsync(IEnumerable<Server> targets, bool waitFinish = false)
    {
        var servers = targets as Server[] ?? targets.ToArray();
        if (servers.Length == 0)
            return;

        if (!waitFinish && Lock.CurrentCount == 0)
            return;

        using var _ = await Lock.EnterAsync();
        await RunTestsAsync(servers);
    }

    private static async Task RunTestsAsync(Server[] servers)
    {
        if (servers.Length == 0)
            return;

        try
        {
            var tasks = servers.Select(async s =>
            {
                using (await PoolLock.EnterAsync())
                {
                    var previous = s.Delay;
                    return await s.PingAsync() != previous;
                }
            });

            if ((await Task.WhenAll(tasks)).Any(changed => changed))
                AppEvents.RaiseNodeDelaysChanged();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static void UpdateTick(bool performTestAtOnce = false)
    {
        UpdateTick(Global.Settings.DetectionTick, performTestAtOnce);
    }

    /// <param name="interval">interval(seconds), 0 disable, MaxValue <c>int.MaxValue/1000</c></param>
    /// <param name="performTestAtOnce"></param>
    private static void UpdateTick(int interval, bool performTestAtOnce = false)
    {
        Timer.Stop();

        var enable = Enabled && interval > 0 && Range.InRange(interval);
        if (enable)
        {
            Timer.Interval = interval * 1000;
            Timer.Start();
            if (performTestAtOnce)
                PerformTestAsync().Forget();
        }
    }
}
