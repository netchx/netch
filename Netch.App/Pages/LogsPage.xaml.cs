using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Netch.App.Helpers;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class LogsPage : Page
{
    private static readonly string[] Sources = ["netch", "sing-box", "mihomo", "xray", "nf", "pcap2socks", "AioDNS", "tun2socks"];
    private static readonly Regex Entry = new(@"^\[(?<time>[^]]+)\]\[(?<level>[^]]+)\]\s*(?<message>.*)$", RegexOptions.Compiled);
    private readonly TextBox _filter = Input(T("Filter logs", "过滤日志"), search: true);
    private readonly ComboBox _source = Choice(T("Source", "来源"), new[] { T("All sources", "全部来源") }.Concat(Sources));
    private readonly ComboBox _severity = Choice(T("Level", "级别"), ["Verbose", "Debug", "Info", "Warning", "Error"], 2);
    private readonly ToggleButton _follow = new() { Content = "⤓ " + T("Auto-scroll", "自动滚动"), IsChecked = true };
    private readonly ListView _list = new() { SelectionMode = ListViewSelectionMode.Extended };
    private readonly ObservableCollection<Line> _visible = [];
    private readonly Dictionary<string, (long Length, DateTime WriteTime)> _versions = [];
    private readonly Dictionary<string, List<Line>> _lines = [];
    private readonly DispatcherQueueTimer _timer;
    private sealed record Line(string Time, int Level, string Source, string Message)
    {
        public string LevelLabel => Level switch
        {
            0 => "TRACE", 1 => "DEBUG", 3 => "WARN", 4 => "ERROR", _ => "INFO"
        };

        public string DisplayTime => Time[(Time.LastIndexOf(' ') + 1)..];

        public override string ToString() => $"{Time}  {LevelLabel,-5}  [{Source}]  {Message}";
    }

    public LogsPage()
    {
        InitializeComponent();
        _list.ItemsSource = _visible;
        _list.ContainerContentChanging += (_, e) =>
        {
            if (!e.InRecycleQueue && e.Item is Line line &&
                e.ItemContainer.ContentTemplateRoot is Grid row && row.FindName("Level") is TextBlock text)
                text.Style = Resource<Style>(line.Level switch
                {
                    3 => "NetchWarningText",
                    4 => "NetchErrorText",
                    _ => "NetchPrimaryText"
                });
        };
        _list.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Grid ColumnSpacing="8">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="92" />
                        <ColumnDefinition Width="45" />
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <TextBlock FontFamily="Consolas" FontSize="13" Foreground="{ThemeResource TextFillColorTertiaryBrush}"
                               Text="{Binding DisplayTime}" />
                    <TextBlock x:Name="Level" Grid.Column="1" FontFamily="Consolas" FontSize="13" FontWeight="SemiBold"
                               Text="{Binding LevelLabel}" />
                    <Border Grid.Column="2" Background="{ThemeResource SubtleFillColorSecondaryBrush}" Padding="6,0"
                            CornerRadius="4" VerticalAlignment="Top">
                        <TextBlock FontFamily="Consolas" FontSize="12" Text="{Binding Source}" />
                    </Border>
                    <TextBlock Grid.Column="3" FontFamily="Consolas" FontSize="13" Text="{Binding Message}" TextWrapping="Wrap" />
                </Grid>
            </DataTemplate>
            """);
        _list.ItemContainerStyle = Resource<Style>("NetchLogItem");
        _filter.TextChanged += (_, _) => Filter();
        _source.SelectionChanged += (_, _) => Filter();
        _severity.SelectionChanged += (_, _) => Filter();
        _follow.Checked += (_, _) => ScrollToEnd();
        var terminal = Card(_list, 4);
        terminal.Padding = new Thickness(4, 8, 4, 4);
        terminal.Margin = new Thickness(0, 6, 0, -16);
        terminal.CornerRadius = new CornerRadius(8, 8, 0, 0);
        Content = Ui.Page(Body(Header(T("Logs", "日志"), _follow, Button(T("⧉ Copy", "⧉ 复制"), () =>
            Copy(string.Join(Environment.NewLine, _list.SelectedItems.Count > 0 ? _list.SelectedItems : _visible.Cast<object>()))),
            Button(T("📂 Open folder", "📂 打开目录"), () => Utils.Utils.Open(Path.Combine(Global.NetchDir, "logging")))),
            Split(_filter, Row(_source, _severity)), terminal));
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Refresh();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is true)
            _severity.SelectedIndex = 4;
        Refresh();
        _timer.Start();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) => _timer.Stop();

    private void Refresh()
    {
        var changed = false;
        var dir = Path.Combine(Global.NetchDir, "logging");
        var files = Directory.EnumerateFiles(dir, "*.log").ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var removed in _versions.Keys.Where(path => !files.Contains(path)).ToArray())
        {
            _versions.Remove(removed);
            _lines.Remove(removed);
            changed = true;
        }

        foreach (var path in files)
        {
            try
            {
                var file = new FileInfo(path);
                var stamp = (file.Length, file.LastWriteTimeUtc);
                if (_versions.TryGetValue(path, out var previous) && stamp == previous)
                    continue;

                var label = Path.GetFileNameWithoutExtension(path) switch
                {
                    "application" => "netch",
                    "singbox" => "sing-box",
                    var name when name.StartsWith("v2ray", StringComparison.OrdinalIgnoreCase) => "xray",
                    var name => name
                };
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                const int tailBytes = 512 * 1024;
                var truncated = stream.Length > tailBytes;
                if (truncated)
                    stream.Seek(-tailBytes, SeekOrigin.End);
                using var reader = new StreamReader(stream);
                if (truncated)
                    reader.ReadLine();
                var lines = new List<Line>();
                for (var raw = reader.ReadLine(); raw != null; raw = reader.ReadLine())
                {
                    if (raw.Length == 0)
                        continue;
                    var match = Entry.Match(raw);
                    if (match.Success)
                    {
                        var message = match.Groups["message"].Value;
                        var source = label == "netch" && (message.Contains("netfilter2", StringComparison.OrdinalIgnoreCase)
                                                         || message.StartsWith("Redirector", StringComparison.OrdinalIgnoreCase)) ? "nf" : label;
                        lines.Add(new Line(match.Groups["time"].Value, Level(match.Groups["level"].Value), source, message));
                    }
                    else
                    {
                        var last = lines.LastOrDefault();
                        var level = raw.Contains("error", StringComparison.OrdinalIgnoreCase) ? 4
                            : raw.Contains("warn", StringComparison.OrdinalIgnoreCase) ? 3 : last?.Level ?? 2;
                        lines.Add(new Line(last?.Time ?? "", level, last?.Source ?? label, raw));
                    }
                }
                _versions[path] = stamp;
                _lines[path] = lines;
                changed = true;
            }
            catch (IOException)
            {
                // A core can replace its log while reconnecting. Read its new contents on the next tick.
            }
        }

        if (changed)
            Filter();
    }

    private static int Level(string label) => label.ToLowerInvariant() switch
    {
        "verbose" or "trace" => 0,
        "debug" => 1,
        "warning" or "warn" => 3,
        "error" or "fatal" => 4,
        _ => 2
    };

    private IReadOnlyList<Line>? _previous;

    private void Filter()
    {
        var source = _source.SelectedIndex <= 0 ? null : Sources[_source.SelectedIndex - 1];
        var next = _lines.Values.SelectMany(lines => lines)
            .Where(line => line.Level >= _severity.SelectedIndex &&
                           (source == null || line.Source.Equals(source, StringComparison.OrdinalIgnoreCase)) &&
                           line.ToString().Contains(_filter.Text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(line => line.Time, StringComparer.Ordinal).TakeLast(2000).ToArray();
        if (_previous != null && _previous.SequenceEqual(next))
            return;

        var selected = _list.SelectedItems.OfType<Line>().GroupBy(line => line)
            .ToDictionary(group => group.Key, group => group.Count());
        if (next.Length > 0 && _visible.Count > 0 && _visible[0] != next[0])
        {
            var first = _visible.IndexOf(next[0]);
            if (first > 0 && _visible.Skip(first).SequenceEqual(next.Take(_visible.Count - first)))
                for (var i = 0; i < first; i++)
                    _visible.RemoveAt(0);
        }

        var prefix = 0;
        while (prefix < _visible.Count && prefix < next.Length && _visible[prefix] == next[prefix])
            prefix++;
        while (_visible.Count > prefix)
            _visible.RemoveAt(_visible.Count - 1);
        foreach (var retained in _list.SelectedItems.OfType<Line>())
            if (selected.TryGetValue(retained, out var count))
                selected[retained] = count - 1;
        foreach (var line in next.Skip(prefix))
        {
            _visible.Add(line);
            if (selected.TryGetValue(line, out var count) && count > 0)
            {
                _list.SelectedItems.Add(line);
                selected[line] = count - 1;
            }
        }
        _previous = next;
        ScrollToEnd();
    }

    private void ScrollToEnd()
    {
        if (_follow.IsChecked == true && _visible.Count > 0)
            _list.ScrollIntoView(_visible[^1]);
    }
}
