using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Netch.Properties;

namespace Netch.Utils;

public static class i18N
{
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
        string text;
        var languages = GetTranslateList().Skip(1).ToList();

        LangCode = value.Equals("System") ? CultureInfo.CurrentCulture.Name : value;

        if (!languages.Contains(LangCode))
        {
            var oldLangCode = LangCode;
            LangCode = languages.FirstOrDefault(s => GetLanguage(s).Equals(GetLanguage(LangCode))) ?? "en-US";
            Log.Information("Not found language {OldLangCode}, use {LangCode} instead", oldLangCode, LangCode);
        }

        switch (LangCode)
        {
            case "en-US":
                Data.Clear();
                return;
            case "zh-CN":
                text = Encoding.UTF8.GetString(Resources.zh_CN);
                break;
            default:
                text = File.ReadAllText($"i18n\\{LangCode}");
                break;
        }

        var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(text)!;

        if (!dictionary.Any())
        {
            Log.Error("Invalid language file \"{LangCode}\"", LangCode);
            return;
        }

        Data = new Hashtable();
        foreach (var v in dictionary)
            Data.Add(v.Key, v.Value);
    }

    private static string GetLanguage(string culture)
    {
        if (!culture.Contains('-'))
            return "";

        return culture.Substring(0, culture.IndexOf('-'));
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

    public static List<string> GetTranslateList()
    {
        var translateFile = new List<string> { "System", "zh-CN", "en-US" };

        if (!Directory.Exists("i18n"))
            return translateFile;

        translateFile.AddRange(Directory.GetFiles("i18n", "*").Select(fileName => fileName.Substring(5)));
        return translateFile;
    }

    public static void TranslateForm(in Control c)
    {
        Utils.ComponentIterator(c,
            component =>
            {
                switch (component)
                {
                    case TextBoxBase:
                    case ListControl:
                        break;
                    case Control control:
                        control.Text = Translate(control.Text);
                        break;
                    case ToolStripItem toolStripItem:
                        toolStripItem.Text = Translate(toolStripItem.Text);
                        break;
                    case ColumnHeader columnHeader:
                        columnHeader.Text = Translate(columnHeader.Text);
                        break;
                }
            });
    }
}