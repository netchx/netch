namespace Netch.Models;

public sealed record SceneIconEntry(string Key, string Glyph, string LabelKey);

/// <summary>
///     Semantic scene icon keys backed by Segoe Fluent / MDL2 glyphs, with lossless migration from legacy emojis.
/// </summary>
public static class SceneIcon
{
    public const string Gamepad = "gamepad";
    public const string Globe = "globe";
    public const string Compass = "compass";
    public const string Console = "console";
    public const string Tools = "tools";
    public const string Chat = "chat";
    public const string Video = "video";
    public const string Music = "music";
    public const string Work = "work";
    public const string Speed = "speed";
    public const string Flash = "flash";
    public const string Lock = "lock";
    public const string Home = "home";
    public const string Desktop = "desktop";
    public const string Mobile = "mobile";
    public const string Cloud = "cloud";

    public const string Default = Gamepad;

    public static IReadOnlyList<SceneIconEntry> Presets { get; } =
    [
        new(Gamepad, "\uE7FC", "Game"),
        new(Globe, "\uE774", "Global network"),
        new(Compass, "\uE826", "Routing"),
        new(Console, "\uE990", "Console"),
        new(Tools, "\uE90F", "Tools"),
        new(Chat, "\uE8BD", "Chat"),
        new(Video, "\uE7F4", "Video"),
        new(Music, "\uE8D6", "Music"),
        new(Work, "\uE821", "Work"),
        new(Speed, "\uEC4A", "Accelerator"),
        new(Flash, "\uE945", "Low latency"),
        new(Lock, "\uE72E", "Security"),
        new(Home, "\uE80F", "Home"),
        new(Desktop, "\uE7F8", "Desktop"),
        new(Mobile, "\uE8EA", "Mobile"),
        new(Cloud, "\uE753", "Cloud")
    ];

    private static readonly Dictionary<string, SceneIconEntry> ByKey = BuildKeyMap();
    private static readonly Dictionary<string, string> ByLegacyOrGlyph = BuildLegacyAndGlyphMap();

    public static string NormalizeKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Default;

        var trimmed = raw.Trim().Replace("\uFE0F", string.Empty, StringComparison.Ordinal);
        if (trimmed.Length == 0)
            return Default;

        if (ByKey.TryGetValue(trimmed, out var byKey))
            return byKey.Key;

        if (ByLegacyOrGlyph.TryGetValue(trimmed, out var mappedKey))
            return mappedKey;

        return Default;
    }

    public static string ToGlyph(string? keyOrEmoji)
    {
        var key = NormalizeKey(keyOrEmoji);
        return ByKey.TryGetValue(key, out var entry) ? entry.Glyph : "\uE7FC";
    }

    private static Dictionary<string, SceneIconEntry> BuildKeyMap()
    {
        var map = new Dictionary<string, SceneIconEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Presets)
            map[entry.Key] = entry;
        return map;
    }

    private static Dictionary<string, string> BuildLegacyAndGlyphMap()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["🎮"] = Gamepad,
            ["🌐"] = Globe,
            ["🧭"] = Compass,
            ["🕹"] = Console,
            ["🛠"] = Tools,
            ["💬"] = Chat,
            ["📺"] = Video,
            ["🎵"] = Music,
            ["💼"] = Work,
            ["🚀"] = Speed,
            ["⚡"] = Flash,
            ["🔒"] = Lock,
            ["🏠"] = Home,
            ["🖥"] = Desktop,
            ["📱"] = Mobile,
            ["☁"] = Cloud
        };
        foreach (var entry in Presets)
            map[entry.Glyph] = entry.Key;
        return map;
    }
}
