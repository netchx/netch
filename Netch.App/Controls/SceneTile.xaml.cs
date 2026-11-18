using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Netch.App.Controls;

/// <summary>A compact scene button for the tray flyout; the running scene is filled with the accent colour.</summary>
public sealed partial class SceneTile : UserControl
{
    public static readonly DependencyProperty TitleTextProperty = DependencyProperty.Register(nameof(TitleText), typeof(string), typeof(SceneTile),
        new PropertyMetadata(string.Empty, (d, e) => ((SceneTile)d).Title.Text = (string)e.NewValue));

    public static readonly DependencyProperty DetailTextProperty = DependencyProperty.Register(nameof(DetailText), typeof(string), typeof(SceneTile),
        new PropertyMetadata(string.Empty, (d, e) => ((SceneTile)d).Detail.Text = (string)e.NewValue));

    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State), typeof(string), typeof(SceneTile),
        new PropertyMetadata("Normal", (d, e) => VisualStateManager.GoToState((SceneTile)d, (string)e.NewValue, false)));

    public SceneTile()
    {
        InitializeComponent();
    }

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public string DetailText
    {
        get => (string)GetValue(DetailTextProperty);
        set => SetValue(DetailTextProperty, value);
    }

    /// <summary>"Normal", "Active" or "Unavailable".</summary>
    public string State
    {
        get => (string)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }
}
