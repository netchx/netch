using Microsoft.UI.Xaml.Markup;
using Netch.Utils;

namespace Netch.App.Helpers;

/// <summary>
///     XAML access to the shared <see cref="i18N" /> dictionary: <c>Text="{h:Tr Text=Settings}"</c>.
///     Keys are the English source strings, as in the WinForms front end.
/// </summary>
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class TrExtension : MarkupExtension
{
    public string Text { get; set; } = string.Empty;

    protected override object ProvideValue() => i18N.Translate(Text);
}
