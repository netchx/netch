using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Netch.App.Helpers;

internal static partial class WindowSize
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmGetWindowAttribute(nint hwnd, int attribute, out Rect bounds, int size);

    public static SizeInt32 InvisibleBorder(Window window)
    {
        const int extendedFrameBounds = 9;
        var hwnd = Win32Interop.GetWindowFromWindowId(window.AppWindow.Id);
        var size = window.AppWindow.Size;
        if (DwmGetWindowAttribute(hwnd, extendedFrameBounds, out var bounds, Marshal.SizeOf<Rect>()) != 0 ||
            bounds.Width <= 0 || bounds.Height <= 0)
            return default;
        return new SizeInt32(Math.Max(0, size.Width - bounds.Width), Math.Max(0, size.Height - bounds.Height));
    }

    public static void Center(Window window, Window anchor, int width, int height)
    {
        var border = InvisibleBorder(window);
        var workArea = DisplayArea.GetFromWindowId(anchor.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var fit = Math.Min(1, Math.Min((double)(workArea.Width - border.Width) / width,
            (double)(workArea.Height - border.Height) / height));
        width = (int)Math.Round(width * fit) + border.Width;
        height = (int)Math.Round(height * fit) + border.Height;
        window.AppWindow.MoveAndResize(new RectInt32(workArea.X + (workArea.Width - width) / 2,
            workArea.Y + (workArea.Height - height) / 2, width, height));
    }
}
