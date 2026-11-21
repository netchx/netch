namespace Netch.Models;

/// <summary>
///     Non-negative values represent measured latency in milliseconds.
///     Negative sentinel values distinguish untested, active, and failure states.
/// </summary>
public static class DelayStatus
{
    public const int Untested = -1;
    public const int DnsFailed = -2;
    public const int Unreachable = -3;
    public const int Timeout = -4;
    public const int Testing = -5;

    public static bool IsValid(int delay) => delay >= 0;

    public static bool IsFailure(int delay) => delay is DnsFailed or Unreachable or Timeout;
}
