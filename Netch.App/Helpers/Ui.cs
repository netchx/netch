using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Netch.Utils;

namespace Netch.App.Helpers;

internal static class Ui
{
    public static string T(string text, string chinese) => i18N.LangCode == "zh-CN" && !i18N.Data.ContainsKey(text)
        ? chinese : i18N.Translate(text);

    public static string TF(string text, string chinese, params object[] args) => string.Format(T(text, chinese), args);

    public static T Resource<T>(string key) => (T)Application.Current.Resources[key];

    public static TextBlock Text(string value, double size = 14, bool strong = false, bool secondary = false) => new()
    {
        Text = value, FontSize = size,
        FontWeight = strong ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
        Style = Resource<Style>(secondary ? "NetchSecondaryText" : "NetchPrimaryText"),
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };

    public static TextBlock Caption(string value) => Text(value, 13, secondary: true);

    public static StackPanel Stack(params UIElement[] children) => Panel(Orientation.Vertical, 9, children);

    public static StackPanel Row(params UIElement[] children) => Panel(Orientation.Horizontal, 9, children);

    private static StackPanel Panel(Orientation orientation, double spacing, UIElement[] children)
    {
        var panel = new StackPanel { Orientation = orientation, Spacing = spacing };
        foreach (var child in children)
            panel.Children.Add(child);
        return panel;
    }

    public static Border Card(UIElement child, double padding = 12) => new()
    {
        Child = child, Padding = new Thickness(padding),
        Style = Resource<Style>("NetchCard")
    };

    public static Border Pill(string label, string? textStyle = null)
    {
        var text = Text(label, 12, secondary: true);
        if (textStyle != null)
            text.Style = Resource<Style>(textStyle);
        return new Border
        {
            Child = text, Style = Resource<Style>("NetchPill"),
            Padding = new Thickness(7, 1, 7, 1), VerticalAlignment = VerticalAlignment.Center
        };
    }

    public static Grid Split(UIElement main, UIElement actions, double spacing = 10)
    {
        var grid = new Grid { ColumnSpacing = spacing };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(main);
        Grid.SetColumn((FrameworkElement)actions, 1);
        grid.Children.Add(actions);
        return grid;
    }

    public static Grid TwoColumns(UIElement left, UIElement right, GridLength leftWidth, double gap = 12)
    {
        var grid = Split(left, right, gap);
        grid.ColumnDefinitions[0].Width = leftWidth;
        grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
        return grid;
    }

    public static Grid Body(params UIElement[] rows)
    {
        var grid = new Grid { RowSpacing = 11 };
        for (var i = 0; i < rows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = i == rows.Length - 1 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
            Grid.SetRow((FrameworkElement)rows[i], i);
            grid.Children.Add(rows[i]);
        }
        return grid;
    }

    public static FrameworkElement Page(UIElement body) => new Grid
    {
        Margin = Resource<Thickness>("PageMargin"),
        Children = { body }
    };

    public static Grid Header(string title, params UIElement[] actions) => Split(Text(title, 24, strong: true), Row(actions));

    public static FontIcon GlyphIcon(string glyph, double size = 14, Brush? foreground = null)
    {
        var icon = new FontIcon { Glyph = glyph, FontSize = size };
        if (foreground != null)
            icon.Foreground = foreground;
        return icon;
    }

    public static StackPanel IconContent(string glyph, string label, double iconSize = 13, double spacing = 6)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = spacing,
            Children =
            {
                new FontIcon { Glyph = glyph, FontSize = iconSize, VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center }
            }
        };
    }

    public static StackPanel StatusRow(string glyph, string text, string? textStyle = null, Brush? iconBrush = null, double iconSize = 12)
    {
        var label = Caption(text);
        if (textStyle != null)
            label.Style = Resource<Style>(textStyle);
        var icon = GlyphIcon(glyph, iconSize, iconBrush ?? label.Foreground);
        icon.VerticalAlignment = VerticalAlignment.Center;
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { icon, label }
        };
    }

    public static void SetButtonContent(ButtonBase button, string label, string? glyph = null, double iconSize = 13)
    {
        button.Content = glyph == null ? label : IconContent(glyph, label, iconSize);
        AutomationProperties.SetName(button, label);
    }

    public static Button IconButton(string glyph, string accessibleName, Action? action = null, FlyoutBase? flyout = null, Style? style = null, double iconSize = 14)
    {
        var button = new Button
        {
            Content = GlyphIcon(glyph, iconSize),
            Flyout = flyout
        };
        if (style != null)
            button.Style = style;
        if (action != null)
            button.Click += (_, _) => action();
        AutomationProperties.SetName(button, accessibleName);
        ToolTipService.SetToolTip(button, accessibleName);
        return button;
    }

    public static Button Button(string label, Action action, bool accent = false, string? glyph = null)
    {
        var button = new Button();
        SetButtonContent(button, label, glyph);
        if (accent)
            button.Style = Resource<Style>("AccentButtonStyle");
        button.Click += (_, _) => action();
        return button;
    }

    public static Button AsyncButton(string label, Func<Task> action, bool accent = false, Action? refreshEnabled = null, string? glyph = null)
    {
        var button = new Button();
        SetButtonContent(button, label, glyph);
        if (accent)
            button.Style = Resource<Style>("AccentButtonStyle");
        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await action(); }
            catch (Exception e)
            {
                Log.Warning(e, "{Action} failed", label);
                if (button.XamlRoot is { } root)
                    await ShowAsync(root, T("Operation failed", "操作失败"), Text(e.Message));
            }
            finally
            {
                if (refreshEnabled == null)
                    button.IsEnabled = true;
                else
                    refreshEnabled();
            }
        };
        return button;
    }

    public static TextBox Input(string label, string? text = null, bool search = false, bool multiline = false)
    {
        var box = new TextBox
        {
            AcceptsReturn = multiline,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Text = text ?? "",
            Header = search ? null : label,
            PlaceholderText = search ? label : ""
        };
        AutomationProperties.SetName(box, label);
        return box;
    }

    public static ComboBox Choice(string label, IEnumerable<string> choices, int index = 0)
    {
        var box = new ComboBox { ItemsSource = choices.ToArray(), SelectedIndex = index };
        AutomationProperties.SetName(box, label);
        return box;
    }

    public static ToggleSwitch CompactToggle(ToggleSwitch toggle, double height = 26)
    {
        toggle.OnContent = toggle.OffContent = null;
        toggle.MinWidth = toggle.MinHeight = 0;
        toggle.Height = height;
        var margin = (height - 20) / 2;
        toggle.Resources["ToggleSwitchPreContentMargin"] = margin;
        toggle.Resources["ToggleSwitchPostContentMargin"] = margin;
        return toggle;
    }

    public static MenuFlyout Menu(params (string Label, Action Action)[] actions)
    {
        var menu = new MenuFlyout();
        foreach (var (label, action) in actions)
        {
            var item = new MenuFlyoutItem { Text = label };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
        }
        return menu;
    }

    public static MenuFlyoutItem MenuItem(string label, string glyph, Action action, bool destructive = false)
    {
        var item = new MenuFlyoutItem
        {
            Text = label,
            Icon = new FontIcon { Glyph = glyph }
        };
        if (destructive)
            item.Style = Resource<Style>("NetchDangerMenuItem");
        item.Click += (_, _) => action();
        return item;
    }

    public static MenuFlyout IconMenu(params MenuFlyoutItemBase[] items)
    {
        var menu = new MenuFlyout();
        foreach (var item in items)
            menu.Items.Add(item);
        return menu;
    }

    public const double ScrollBarGutter = 16;

    public static ScrollViewer Scroll(UIElement content, double rightGutter = ScrollBarGutter) => new()
    {
        Content = rightGutter > 0 ? new Border { Padding = new Thickness(0, 0, rightGutter, 0), Child = content } : content,
        HorizontalScrollMode = ScrollMode.Disabled,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    };

    public static ListView Categories() => new()
    {
        SelectionMode = ListViewSelectionMode.Single,
        ItemContainerStyle = Resource<Style>("NetchCompactRow"),
        Margin = new Thickness(0, 0, -10, 0),
        Padding = new Thickness(0, 0, ScrollBarGutter, 0)
    };

    public static ListViewItem Category(string name, string? detail = null, string? count = null, string? detailStyle = null, string? detailGlyph = null)
    {
        var header = Text(name, strong: true);
        var top = count == null ? (UIElement)header : Split(header, Caption(count));
        UIElement? detailElement = null;
        if (detail != null)
        {
            var caption = Caption(detail);
            if (detailStyle != null)
                caption.Style = Resource<Style>(detailStyle);
            if (detailGlyph != null)
            {
                var icon = GlyphIcon(detailGlyph, 11, caption.Foreground);
                icon.VerticalAlignment = VerticalAlignment.Center;
                detailElement = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    Children = { icon, caption }
                };
            }
            else
            {
                detailElement = caption;
            }
        }
        var item = new ListViewItem
        {
            Content = detailElement == null ? top : new StackPanel { Spacing = 1, Children = { top, detailElement } },
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(item, name);
        return item;
    }

    public static Border RowIndicator(double height) => new()
    {
        Width = 3, Height = height, HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(-12, 0, 0, 0),
        Style = Resource<Style>("NetchRowIndicator"), IsHitTestVisible = false,
        Visibility = Visibility.Collapsed
    };

    public static void AddSelectionIndicator(ListViewItem item)
    {
        var content = (UIElement)item.Content;
        var indicator = RowIndicator(15);
        item.Content = null;
        item.Content = new Grid { Children = { content, indicator } };
        item.RegisterPropertyChangedCallback(ListViewItem.IsSelectedProperty, (_, _) =>
            indicator.Visibility = item.IsSelected ? Visibility.Visible : Visibility.Collapsed);
    }

    public static ListViewItem CategoryHeading(string label) => new()
    {
        Content = Caption(label), IsEnabled = false, MinHeight = 0, Height = 25,
        Margin = new Thickness(0, 0, 0, 2.5)
    };

    private static bool _dialogOpen;

    public static async Task<bool> ShowAsync(XamlRoot? root, string title, UIElement body, string? confirm = null, Func<string?>? apply = null, double? contentMaxWidth = null)
    {
        if (root is null || _dialogOpen)
            return false;

        if (body is TextBlock text)
        {
            text.TextWrapping = TextWrapping.Wrap;
            text.TextTrimming = TextTrimming.None;
            text.IsTextSelectionEnabled = true;
        }
        var scroll = Scroll(body);
        if (root.Size.Height > 0)
            scroll.MaxHeight = Math.Max(200, root.Size.Height - 230);
        var error = new InfoBar
        {
            Severity = InfoBarSeverity.Error, IsClosable = false, Margin = new Thickness(0, 0, 0, 9)
        };
        var content = Body(error, scroll);
        content.RowSpacing = 0;
        var dialog = new ContentDialog
        {
            XamlRoot = root, RequestedTheme = (root.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default,
            Style = Resource<Style>("DefaultContentDialogStyle"), Title = title,
            Content = content, PrimaryButtonText = confirm ?? "",
            CloseButtonText = confirm == null ? T("Close", "关闭") : T("Cancel", "取消"),
            DefaultButton = confirm == null ? ContentDialogButton.Close : ContentDialogButton.Primary
        };
        if (root.Size.Height > 0)
            dialog.Resources["ContentDialogMaxHeight"] = Math.Max(360, root.Size.Height - 40);
        if (contentMaxWidth.HasValue)
            dialog.Resources["ContentDialogMaxWidth"] = contentMaxWidth.Value;
        if (apply != null)
            dialog.PrimaryButtonClick += (_, e) =>
            {
                var message = apply();
                e.Cancel = message != null;
                error.Message = message ?? "";
                error.IsOpen = e.Cancel;
            };
        _dialogOpen = true;
        try
        {
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    public static void Copy(string value)
    {
        var data = new DataPackage();
        data.SetText(value);
        Clipboard.SetContent(data);
    }

    public static async Task<string> ClipboardTextAsync()
    {
        var data = Clipboard.GetContent();
        return data.Contains(StandardDataFormats.Text) ? await data.GetTextAsync() : "";
    }
}
