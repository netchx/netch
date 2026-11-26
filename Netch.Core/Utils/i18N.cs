using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Netch.Utils;

public static class i18N
{
    private static readonly string[] BuiltInLanguages = ["zh-CN", "zh-TW", "en-US", "ar", "ja-JP", "ru", "fa-IR"];

    private static readonly HashSet<string> KnownCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
        .Select(c => c.Name)
        .Where(name => !string.IsNullOrEmpty(name))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> NativeLanguageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zh-CN"] = "简体中文",
        ["zh-Hans"] = "简体中文",
        ["zh-TW"] = "繁體中文",
        ["zh-Hant"] = "繁體中文",
        ["en-US"] = "English",
        ["en"] = "English",
        ["ar"] = "العربية",
        ["ja-JP"] = "日本語",
        ["ja"] = "日本語",
        ["ru"] = "Русский",
        ["fa-IR"] = "فارسی",
        ["fa"] = "فارسی"
    };

    static i18N()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    ///     数据
    /// </summary>
    public static Hashtable Data = new();

    public static string LangCode { get; private set; } = "en-US";

    /// <summary>
    ///     加载
    /// </summary>
    /// <param name="value">语言代码</param>
    public static void Load(string value)
    {
        var languages = GetTranslateList().Skip(1).ToList();
        var requested = value == "System" ? CultureInfo.CurrentUICulture.Name : value;
        var culture = TryGetCulture(requested);
        var traditional = culture?.TwoLetterISOLanguageName == "zh" &&
            (culture.Parent.Name == "zh-Hant" || culture.Name == "zh-Hant") ? "zh-TW" : null;
        bool SameCulture(string language) => language.Equals(requested, StringComparison.OrdinalIgnoreCase);
        bool SameLanguage(string language) => TryGetCulture(language)?.TwoLetterISOLanguageName == culture?.TwoLetterISOLanguageName;
        var builtIn = BuiltInLanguages.FirstOrDefault(SameCulture) ?? traditional ?? BuiltInLanguages.FirstOrDefault(SameLanguage);
        var code = languages.FirstOrDefault(SameCulture) ?? builtIn ?? languages.FirstOrDefault(SameLanguage)
            ?? "en-US";

        var table = new Hashtable();
        if (builtIn is not (null or "en-US"))
        {
            var text = EmbedUtils.GetEmbedText($"Netch.Resources.{builtIn}");
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            if (dictionary != null)
                foreach (var (key, translation) in dictionary)
                    table[key] = translation;
        }

        var external = Path.Combine("i18n", code);
        if (File.Exists(external) && code != "en-US")
        {
            try
            {
                var overrides = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(external));
                if (overrides != null)
                    foreach (var (key, translation) in overrides)
                        if (!string.IsNullOrWhiteSpace(translation) && PreservesFormat(key, translation))
                            table[key] = translation;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
            {
                Log.Warning(e, "Could not load language file {Language}", code);
                code = builtIn ?? "en-US";
            }
        }

        Data = table;
        LangCode = code;
    }

    private static bool PreservesFormat(string key, string translation)
    {
        if (!key.Contains('{'))
            return true;
        try
        {
            return CompositeFormat.Parse(key).MinimumArgumentCount == CompositeFormat.Parse(translation).MinimumArgumentCount;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static CultureInfo? TryGetCulture(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        try { return CultureInfo.GetCultureInfo(name); }
        catch (CultureNotFoundException) { return null; }
    }

    /// <summary>
    ///     翻译
    /// </summary>
    /// <param name="text">需要翻译的文本</param>
    /// <returns>翻译完毕的文本</returns>
    public static string Translate(params object[] text)
    {
        var a = new StringBuilder();
        foreach (var t in text)
            if (t is string)
                a.Append(Data[t]?.ToString() ?? t);
            else
                a.Append(t);

        return a.ToString();
    }

    public static string TranslateFormat(string format, params object[] args)
    {
        for (var i = 0; i < args.Length; i++)
            if (args[i] is string)
                args[i] = Translate((string)args[i]);

        return string.Format(Translate(format), args);
    }

    public static string GetDisplayName(string code)
    {
        if (NativeLanguageNames.TryGetValue(code, out var name))
            return name;

        var culture = TryGetCulture(code);
        if (culture == null)
            return code;

        if (culture.TwoLetterISOLanguageName == "zh")
            return culture.Parent.Name == "zh-Hant" || culture.Name == "zh-Hant" ? "繁體中文" : "简体中文";

        var neutral = culture;
        while (!neutral.IsNeutralCulture && neutral.Parent != CultureInfo.InvariantCulture)
            neutral = neutral.Parent;

        if (NativeLanguageNames.TryGetValue(neutral.Name, out name))
            return name;

        var native = neutral.NativeName;
        return native.Length > 0 && char.IsLower(native[0])
            ? neutral.TextInfo.ToUpper(native[0]) + native[1..]
            : native;
    }

    public static List<string> GetTranslateList()
    {
        return Directory.Exists("i18n")
            ? ["System", .. BuiltInLanguages.Union(
                Directory.EnumerateFiles("i18n").Select(Path.GetFileName).OfType<string>()
                    .Where(name => KnownCultures.Contains(name) || (name.Contains('-') && TryGetCulture(name) != null)),
                StringComparer.OrdinalIgnoreCase)]
            : ["System", .. BuiltInLanguages];
    }

}
