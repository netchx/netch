using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netch.App.ViewModels;

namespace Netch.App.Helpers;

public sealed partial class SceneWallTemplateSelector : DataTemplateSelector
{
    public DataTemplate SceneTemplate { get; set; } = null!;

    public DataTemplate AddTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item) => item is SceneCard ? SceneTemplate : AddTemplate;

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}
