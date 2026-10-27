using System.Diagnostics.CodeAnalysis;

namespace Netch;

public static class Extension
{
    public static bool IsNotEmpty([NotNullWhen(false)] this string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static string TrimEx(this string? value)
    {
        return value == null ? string.Empty : value.Trim();
    }

    public static string AppendQuotes(this string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : $"\"{value}\"";
    }

    public static int ToInt(this string? value, int defaultValue = 0)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}

