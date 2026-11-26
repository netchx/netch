using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Netch.App.Helpers;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class LogsPage : Page
{
    private static readonly string[] Sources = ["netch", "sing-box", "mihomo", "xray", "nf", "pcap2socks", "AioDNS", "tun2socks"];
    private static readonly Dictionary<string, long> ClearedOffsets = new(StringComparer.OrdinalIgnoreCase);

    private readonly TextBox _filter = Input(T("Filter logs", "过滤日志"), search: true);
    private readonly ComboBox _source = Choice(T("Source", "来源"), new[] { T("All sources", "全部来源") }.Concat(Sources));
    private readonly ComboBox _severity = Choice(T("Level", "级别"), ["Verbose", "Debug", "Info", "Warning", "Error"], 2);
    private readonly ToggleButton _follow = new()
    {
        Content = IconContent(Icons.ArrowDown, T("Auto-scroll", "自动滚动")),
        IsChecked = true
    };
    private readonly ListView _list = new() { SelectionMode = ListViewSelectionMode.Extended, Padding = new Thickness(0, 0, Ui.ScrollBarGutter, 0) };
    private readonly TextBlock _emptyText;
    private readonly ObservableCollection<ParsedLogLine> _visible = [];
    private readonly Dictionary<string, (long Length, DateTime WriteTime)> _versions = [];
    private readonly Dictionary<string, List<ParsedLogLine>> _lines = [];
    private readonly DispatcherQueueTimer _timer;

    public LogsPage()
    {
        InitializeComponent();
        _list.ItemsSource = _visible;
        _list.ContainerContentChanging += (_, e) =>
        {
            if (!e.InRecycleQueue && e.Item is ParsedLogLine line &&
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
                        <ColumnDefinition Width="96" />
                        <ColumnDefinition Width="48" />
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

        AutomationProperties.SetName(_follow, T("Auto-scroll", "自动滚动"));

        var clearButton = Button(T("Clear", "清除"), ClearLogs, glyph: Icons.Delete);
        AutomationProperties.SetName(clearButton, T("Clear logs", "清除日志"));

        _emptyText = Caption(T("No log entries", "暂无日志"));
        _emptyText.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyText.VerticalAlignment = VerticalAlignment.Center;
        _emptyText.Visibility = Visibility.Collapsed;

        var surface = new Grid { Children = { _list, _emptyText } };
        var terminal = Card(surface, 4);
        terminal.Padding = new Thickness(4, 8, 4, 4);
        terminal.Margin = new Thickness(0, 6, 0, -16);
        terminal.CornerRadius = new CornerRadius(8, 8, 0, 0);

        Content = Ui.Page(Body(Header(T("Logs", "日志"), _follow, clearButton,
            Button(T("Copy", "复制"), () =>
            {
                var text = string.Join(Environment.NewLine, _list.SelectedItems.Count > 0 ? _list.SelectedItems : _visible.Cast<object>());
                if (text.Length > 0)
                    Copy(text);
            }, glyph: Icons.Copy),
            Button(T("Open directory", "打开目录"), () => Utils.Utils.Open(Path.Combine(Global.NetchDir, "logging")), glyph: Icons.FolderOpen)),
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

    private void ClearLogs()
    {
        var dir = Path.Combine(Global.NetchDir, "logging");
        if (Directory.Exists(dir))
        {
            foreach (var path in Directory.EnumerateFiles(dir, "*.log")
                         .Where(path => !string.Equals(Path.GetFileName(path), "nf-connections.log", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var info = new FileInfo(path);
                    ClearedOffsets[path] = info.Length;
                    _versions[path] = (info.Length, info.LastWriteTimeUtc);
                }
                catch (IOException)
                {
                }
            }
        }

        _lines.Clear();
        _visible.Clear();
        _previous = null;
        UpdateEmptyState();
    }

    private void Refresh()
    {
        var changed = false;
        var dir = Path.Combine(Global.NetchDir, "logging");
        if (!Directory.Exists(dir))
        {
            UpdateEmptyState();
            return;
        }

        var files = Directory.EnumerateFiles(dir, "*.log")
            .Where(path => !string.Equals(Path.GetFileName(path), "nf-connections.log", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var removed in _versions.Keys.Where(path => !files.Contains(path)).ToArray())
        {
            _versions.Remove(removed);
            _lines.Remove(removed);
            ClearedOffsets.Remove(removed);
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

                var label = ResolveFileLabel(path);
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (ClearedOffsets.TryGetValue(path, out var clearedOffset) && stream.Length < clearedOffset)
                {
                    ClearedOffsets.Remove(path);
                    clearedOffset = 0;
                }

                const int tailBytes = 512 * 1024;
                var tailStart = Math.Max(0L, stream.Length - tailBytes);
                var startPos = Math.Max(clearedOffset, tailStart);
                var lines = new List<ParsedLogLine>();
                if (startPos < stream.Length)
                {
                    if (startPos > 0)
                        stream.Seek(startPos, SeekOrigin.Begin);
                    using var reader = new StreamReader(stream);
                    if (startPos > clearedOffset)
                        reader.ReadLine();

                    ParsedLogLine? last = null;
                    for (var raw = reader.ReadLine(); raw != null; raw = reader.ReadLine())
                    {
                        if (raw.Length == 0)
                            continue;
                        var parsed = LogLineParser.Parse(raw, label, last, file.LastWriteTime);
                        lines.Add(parsed);
                        last = parsed;
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

    private static string ResolveFileLabel(string path)
    {
        var rawName = Path.GetFileNameWithoutExtension(path);
        return rawName.ToLowerInvariant() switch
        {
            "application" => "netch",
            "singbox" or "sing-box" => "sing-box",
            "aiodns" => "AioDNS",
            "tun2socks" => "tun2socks",
            "pcap2socks" => "pcap2socks",
            var name when name.StartsWith("v2ray", StringComparison.OrdinalIgnoreCase)
                          || name.StartsWith("xray", StringComparison.OrdinalIgnoreCase) => "xray",
            _ => rawName
        };
    }

    private IReadOnlyList<ParsedLogLine>? _previous;

    private void Filter()
    {
        var source = _source.SelectedIndex <= 0 ? null : Sources[_source.SelectedIndex - 1];
        var next = _lines.Values.SelectMany(lines => lines)
            .Where(line => line.Level >= _severity.SelectedIndex &&
                           (source == null || line.Source.Equals(source, StringComparison.OrdinalIgnoreCase)) &&
                           line.ToString().Contains(_filter.Text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(line => line.Time, StringComparer.Ordinal).TakeLast(2000).ToArray();
        if (_previous != null && _previous.SequenceEqual(next))
        {
            UpdateEmptyState();
            return;
        }

        var selected = _list.SelectedItems.OfType<ParsedLogLine>().GroupBy(line => line)
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
        foreach (var retained in _list.SelectedItems.OfType<ParsedLogLine>())
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
        UpdateEmptyState();
        ScrollToEnd();
    }

    private void UpdateEmptyState() =>
        _emptyText.Visibility = _visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void ScrollToEnd()
    {
        if (_follow.IsChecked == true && _visible.Count > 0)
            _list.ScrollIntoView(_visible[^1]);
    }
}
