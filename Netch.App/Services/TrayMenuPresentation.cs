using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Netch.App.Services;

internal sealed unsafe class TrayMenuPresentation : IDisposable
{
    private const uint Selected = 0x0001;
    private const int MeasureItem = 0x002C;
    private const int DrawItem = 0x002B;
    private const uint Menu = 1;
    private const uint MsaaMenuSignature = 0xAA0DF00D;

    [StructLayout(LayoutKind.Sequential)]
    private struct Measure
    {
        public uint ControlType, ControlId, ItemId, Width, Height;
        public nuint Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Draw
    {
        public uint ControlType, ControlId, ItemId, Action, State;
        public nint Owner, DeviceContext;
        public RECT Bounds;
        public nuint Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccessibleItem
    {
        public uint Signature, TextLength;
        public nint Text;
    }

    private readonly AccessibleItem* _accessibleItems;
    private readonly IReadOnlyList<MenuEntry?> _entries;
    private readonly double _scale;
    private readonly Font _font;
    private readonly SolidBrush _background;
    private readonly SolidBrush _foreground;
    private readonly SolidBrush _muted;
    private readonly SolidBrush _selection;
    private readonly SolidBrush _separator;
    private readonly SolidBrush _accent;

    public TrayMenuPresentation(IReadOnlyList<MenuEntry?> entries, HWND hwnd, ElementTheme theme)
    {
        _entries = entries;
        _scale = PInvoke.GetDpiForWindow(hwnd) / 96.0;
        var resources = (ResourceDictionary)Application.Current.Resources.ThemeDictionaries[theme == ElementTheme.Dark ? "Dark" : "Light"];
        _background = Brush("TrayMenuBackground");
        _foreground = Brush("TrayMenuForeground");
        _muted = Brush("TrayMenuSecondary");
        _selection = Brush("TrayMenuSelection");
        _separator = Brush("NetchStroke");
        _accent = Brush("TrayMenuAccent");
        _font = new Font("Segoe UI", Pixels(12), GraphicsUnit.Pixel);
        _accessibleItems = (AccessibleItem*)NativeMemory.AllocZeroed((nuint)entries.Count, (nuint)sizeof(AccessibleItem));

        SolidBrush Brush(string key)
        {
            var color = ((SolidColorBrush)resources[key]).Color;
            return new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }

    private int Pixels(double dip) => (int)Math.Round(dip * _scale);

    public bool AddItem(HMENU menu, int index)
    {
        var entry = _entries[index];
        var text = entry?.Text ?? "";
        var accessible = _accessibleItems + index;
        accessible->Signature = MsaaMenuSignature;
        accessible->TextLength = (uint)text.Length;
        accessible->Text = Marshal.StringToHGlobalUni(text);
        var item = new MENUITEMINFOW
        {
            cbSize = (uint)sizeof(MENUITEMINFOW),
            fMask = MENU_ITEM_MASK.MIIM_FTYPE | MENU_ITEM_MASK.MIIM_STRING | MENU_ITEM_MASK.MIIM_ID |
                MENU_ITEM_MASK.MIIM_STATE | MENU_ITEM_MASK.MIIM_DATA,
            fType = MENU_ITEM_TYPE.MFT_OWNERDRAW,
            fState = entry?.Enabled == true ? MENU_ITEM_STATE.MFS_ENABLED : MENU_ITEM_STATE.MFS_DISABLED,
            wID = (uint)(index + 1),
            dwItemData = (nuint)accessible,
            dwTypeData = (char*)accessible->Text
        };
        if (entry?.Checked == true)
            item.fState |= MENU_ITEM_STATE.MFS_CHECKED;
        return PInvoke.InsertMenuItem(menu, (uint)index, true, &item);
    }

    public bool Handle(uint message, LPARAM args)
    {
        if (message == MeasureItem)
        {
            var item = (Measure*)args.Value;
            if (item->ControlType != Menu || item->Data == 0)
                return false;

            var index = (int)item->ItemId - 1;
            item->Width = (uint)Pixels(200);
            item->Height = (uint)Pixels(_entries[index] != null ? 29 : index == _entries.Count - 2 ? 8 : 9);
            return true;
        }

        if (message != DrawItem)
            return false;

        var row = (Draw*)args.Value;
        if (row->ControlType != Menu || row->Data == 0)
            return false;

        var entry = _entries[(int)row->ItemId - 1];
        var bounds = Rectangle.FromLTRB(row->Bounds.left, row->Bounds.top, row->Bounds.right, row->Bounds.bottom);
        using var graphics = Graphics.FromHdc(row->DeviceContext);
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        graphics.FillRectangle(_background, bounds);
        if (((uint)row->State & Selected) != 0)
            graphics.FillRectangle(_selection, Rectangle.Inflate(bounds, -Pixels(4), -Pixels(1)));

        var inset = Pixels(12);
        if (entry == null)
        {
            graphics.FillRectangle(_separator, new Rectangle(bounds.X + Pixels(5), bounds.Y + bounds.Height / 2,
                bounds.Width - Pixels(10), Math.Max(1, Pixels(1))));
            return true;
        }

        using var format = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter
        };
        var textBounds = new RectangleF(bounds.X + inset, bounds.Y, bounds.Width - 2 * inset, bounds.Height);
        var label = entry.Text.Replace("&&", "&", StringComparison.Ordinal);
        if (entry.Checked)
        {
            var previousSmoothing = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var dotSize = Math.Max(4f, (float)(5.5 * _scale));
            var dotY = bounds.Y + (bounds.Height - dotSize) / 2f;
            graphics.FillEllipse(_accent, textBounds.X, dotY, dotSize, dotSize);
            var dotAdvance = dotSize + Math.Max(4f, (float)(6 * _scale));
            textBounds.X += dotAdvance;
            textBounds.Width -= dotAdvance;

            var tickWidth = Pixels(19);
            var checkLeft = textBounds.Right - tickWidth + (float)(3 * _scale);
            var checkMidY = bounds.Y + bounds.Height / 2f + (float)(2.5 * _scale);
            using var pen = new Pen(_accent.Color, Math.Max(1.4f, (float)(1.6 * _scale)))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            graphics.DrawLines(pen,
            [
                new PointF(checkLeft, checkMidY - (float)(2.2 * _scale)),
                new PointF(checkLeft + (float)(2.6 * _scale), checkMidY),
                new PointF(checkLeft + (float)(7.5 * _scale), checkMidY - (float)(5.5 * _scale))
            ]);

            graphics.SmoothingMode = previousSmoothing;
            textBounds.Width -= tickWidth;
        }

        if (entry.Shortcut is { } shortcut)
        {
            var shortcutWidth = graphics.MeasureString(shortcut, _font).Width;
            using var shortcutFormat = new StringFormat(format) { Alignment = StringAlignment.Far };
            graphics.DrawString(shortcut, _font, _muted, textBounds, shortcutFormat);
            textBounds.Width -= shortcutWidth + inset;
        }
        graphics.DrawString(label, _font, entry.Enabled ? _foreground : _muted, textBounds, format);
        return true;
    }

    public void Dispose()
    {
        _font.Dispose();
        _background.Dispose();
        _foreground.Dispose();
        _muted.Dispose();
        _selection.Dispose();
        _separator.Dispose();
        _accent.Dispose();
        for (var i = 0; i < _entries.Count; i++)
            Marshal.FreeHGlobal(_accessibleItems[i].Text);
        NativeMemory.Free(_accessibleItems);
    }
}
