using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Netch.App.Services;

/// <summary>
///     Notification-area icon and global hotkeys on a hidden top-level window. Create it on the UI thread:
///     that thread's message loop delivers the window's messages, so events are raised on it.
/// </summary>
internal sealed unsafe class TrayIcon : IDisposable
{
    private const string WindowClass = "NetchTrayWindow";
    private const uint CallbackMessage = PInvoke.WM_APP + 1;
    private const uint NinSelect = PInvoke.WM_USER;
    private const uint NinKeySelect = NinSelect | 1;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmBottomAlign = 0x0020;
    private const uint TpmReturnCmd = 0x0100;

    // The window procedure must outlive every message, so it is rooted here.
    private static readonly WNDPROC WndProcDelegate = WndProc;
    private static TrayIcon? _instance;

    private readonly HWND _hwnd;
    private readonly uint _taskbarCreated;
    private readonly HICON _icon;
    private string _tooltip;
    private TrayMenuPresentation? _menuPresentation;

    public TrayIcon(string iconPath, string tooltip)
    {
        _instance = this;
        _tooltip = tooltip;

        var module = (HINSTANCE)PInvoke.GetModuleHandle((PCWSTR)null).Value;
        fixed (char* className = WindowClass)
        fixed (char* title = "Netch")
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = WndProcDelegate,
                hInstance = module,
                lpszClassName = className
            };
            if (PInvoke.RegisterClassEx(windowClass) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // A top-level window receives TaskbarCreated after Explorer restarts.
            _hwnd = PInvoke.CreateWindowEx(0, className, title, 0, 0, 0, 0, 0, HWND.Null, default, module, null);
            if (_hwnd == HWND.Null)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // Netch runs elevated; let the medium-integrity shell reach this window.
        _taskbarCreated = PInvoke.RegisterWindowMessage("TaskbarCreated");
        PInvoke.ChangeWindowMessageFilterEx(_hwnd, _taskbarCreated, WINDOW_MESSAGE_FILTER_ACTION.MSGFLT_ALLOW);
        PInvoke.ChangeWindowMessageFilterEx(_hwnd, CallbackMessage, WINDOW_MESSAGE_FILTER_ACTION.MSGFLT_ALLOW);

        var dpi = PInvoke.GetDpiForWindow(_hwnd);
        var size = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXSMICON, dpi);
        fixed (char* path = iconPath)
            _icon = (HICON)PInvoke.LoadImage(HINSTANCE.Null, path, GDI_IMAGE_TYPE.IMAGE_ICON, size, size, IMAGE_FLAGS.LR_LOADFROMFILE).Value;

        AddIcon();
    }

    /// <summary>Left click or Enter on the icon; the argument is the icon position in screen pixels.</summary>
    public event Action<Point>? Selected;

    /// <summary>Right click or the menu key on the icon.</summary>
    public event Action<Point>? ContextMenuRequested;

    /// <summary>A hotkey registered with <see cref="RegisterHotkey" /> was pressed; the argument is its id.</summary>
    public event Action<int>? HotkeyPressed;

    public string Tooltip
    {
        get => _tooltip;
        set
        {
            _tooltip = value;
            var data = NewData(NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP);
            PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, data);
        }
    }

    public void ShowBalloon(string title, string text, bool error = false)
    {
        var data = NewData(NOTIFY_ICON_DATA_FLAGS.NIF_INFO);
        data.szInfoTitle = Truncate(title, 63);
        data.szInfo = Truncate(text, 255);
        data.dwInfoFlags = error ? NOTIFY_ICON_INFOTIP_FLAGS.NIIF_ERROR : NOTIFY_ICON_INFOTIP_FLAGS.NIIF_INFO;
        PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, data);
    }

    /// <summary>Shows a native popup menu at <paramref name="at" />. Null entries are separators.</summary>
    /// <returns>The index of the chosen entry, or -1.</returns>
    public int ShowMenu(Point at, IReadOnlyList<MenuEntry?> entries)
    {
        var menu = PInvoke.CreatePopupMenu();
        using var presentation = new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast ? null :
            new TrayMenuPresentation(entries, _hwnd, App.MainWindow?.ActualTheme ?? Microsoft.UI.Xaml.ElementTheme.Light);
        _menuPresentation = presentation;
        try
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (presentation?.AddItem(menu, i) == true)
                    continue;

                if (entries[i] is not { } entry)
                {
                    PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, default(PCWSTR));
                    continue;
                }

                var flags = MENU_ITEM_FLAGS.MF_STRING;
                if (entry.Checked)
                    flags |= MENU_ITEM_FLAGS.MF_CHECKED;
                if (!entry.Enabled)
                    flags |= MENU_ITEM_FLAGS.MF_GRAYED;

                fixed (char* text = entry.Text)
                    PInvoke.AppendMenu(menu, flags, (nuint)(i + 1), text);
            }

            // Without the foreground window the menu would not close when the user clicks elsewhere.
            PInvoke.SetForegroundWindow(_hwnd);
            var chosen = (int)PInvoke.TrackPopupMenuEx(menu, TpmReturnCmd | TpmRightButton | TpmBottomAlign, at.X, at.Y, _hwnd, null);
            PInvoke.PostMessage(_hwnd, PInvoke.WM_NULL, default, default);
            return chosen - 1;
        }
        finally
        {
            _menuPresentation = null;
            PInvoke.DestroyMenu(menu);
        }
    }

    public bool RegisterHotkey(int id, HOT_KEY_MODIFIERS modifiers, uint key) =>
        PInvoke.RegisterHotKey(_hwnd, id, modifiers | HOT_KEY_MODIFIERS.MOD_NOREPEAT, key);

    public void UnregisterHotkey(int id) => PInvoke.UnregisterHotKey(_hwnd, id);

    public void Dispose()
    {
        PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, NewData(0));
        PInvoke.DestroyIcon(_icon);
        PInvoke.DestroyWindow(_hwnd);
        _instance = null;
    }

    private void AddIcon()
    {
        var data = NewData(NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP);
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = _icon;
        PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, data);

        data.Anonymous.uVersion = PInvoke.NOTIFYICON_VERSION_4;
        PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, data);
    }

    private NOTIFYICONDATAW NewData(NOTIFY_ICON_DATA_FLAGS flags) => new()
    {
        cbSize = (uint)sizeof(NOTIFYICONDATAW),
        hWnd = _hwnd,
        uID = 1,
        uFlags = flags,
        szTip = Truncate(_tooltip, 127)
    };

    private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length];

    private static LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        var self = _instance;
        try
        {
            if (self != null && hwnd == self._hwnd)
            {
                if (self._menuPresentation?.Handle(message, lParam) == true)
                    return new LRESULT(1);

                if (message == CallbackMessage)
                {
                    // NOTIFYICON_VERSION_4: the event is in the low word of lParam, the anchor point in wParam.
                    var anchor = new Point((short)(wParam.Value & 0xFFFF), (short)((wParam.Value >> 16) & 0xFFFF));
                    switch ((uint)(lParam.Value & 0xFFFF))
                    {
                        case NinSelect:
                        case NinKeySelect:
                            self.Selected?.Invoke(anchor);
                            break;
                        case PInvoke.WM_CONTEXTMENU:
                            self.ContextMenuRequested?.Invoke(anchor);
                            break;
                    }

                    return default;
                }

                if (message == PInvoke.WM_HOTKEY)
                {
                    self.HotkeyPressed?.Invoke((int)wParam.Value);
                    return default;
                }

                if (message == self._taskbarCreated)
                {
                    self.AddIcon();
                    return default;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Tray window message {Message} failed", message);
        }

        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }
}

internal sealed record MenuEntry(string Text, bool Checked = false, bool Enabled = true, string? Shortcut = null);
