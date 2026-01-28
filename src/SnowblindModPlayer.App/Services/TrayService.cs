using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using SnowblindModPlayer.Core.Services;

namespace SnowblindModPlayer.Services;

/// <summary>
/// Enhanced native Windows tray icon using Shell_NotifyIcon P/Invoke.
/// Supports: custom icon, double-click, context menu, balloon notifications, dynamic video menu.
/// </summary>
public class TrayService : ITrayService
{
    // Shell_NotifyIcon message IDs
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    
    // Notification icon flags
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIF_INFO = 0x00000010;
    
    // Window messages
    private const uint WM_USER = 0x0400;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    
    // Notification balloon flags
    private const uint NIIF_INFO = 0x00000001;
    private const uint NIIF_WARNING = 0x00000002;
    private const uint NIIF_ERROR = 0x00000003;

    [StructLayout(LayoutKind.Sequential)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
        IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    private const int GWL_WNDPROC = -4;
    private const uint MF_STRING = 0x00000000;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint MF_POPUP = 0x00000010;
    private const uint TPM_LEFTALIGN = 0x0000;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;
    
    // Menu command IDs
    private const int CMD_SHOW = 1;
    private const int CMD_PLAY_DEFAULT = 2;
    private const int CMD_VIDEOS_SUBMENU = 3;
    private const int CMD_STOP = 4;
    private const int CMD_EXIT = 5;
    private const int CMD_VIDEO_BASE = 100; // 100-199 for dynamic video entries

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private IntPtr _hwnd;
    private NOTIFYICONDATA _nid;
    private Action? _onShowRequested;
    private Action? _onExitRequested;
    private Func<Task>? _onPlayDefaultRequested;
    private Func<string, Task>? _onPlayVideoRequested;
    private Func<Task>? _onStopRequested;
    private Func<Task<List<VideoItem>>>? _getVideosForMenu;
    private IntPtr _trayHIcon;
    private IntPtr _oldWndProc;
    private WndProcDelegate? _wndProcDelegate;

    public void Initialize(
        Action onShowRequested,
        Action onExitRequested,
        Func<Task>? onPlayDefaultRequested = null,
        Func<string, Task>? onPlayVideoRequested = null,
        Func<Task>? onStopRequested = null,
        Func<Task<List<VideoItem>>>? getVideosForMenu = null)
    {
        _onShowRequested = onShowRequested;
        _onExitRequested = onExitRequested;
        _onPlayDefaultRequested = onPlayDefaultRequested;
        _onPlayVideoRequested = onPlayVideoRequested;
        _onStopRequested = onStopRequested;
        _getVideosForMenu = getVideosForMenu;

        try
        {
            // Create hidden window to handle messages
            _hwnd = CreateWindowEx(0, "STATIC", "SnowblindModPlayer_Tray", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("? Failed to create window for tray icon");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"? Window created: {_hwnd}");

            // Subclass the window to handle messages
            _wndProcDelegate = WndProcHandler;
            _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
            System.Diagnostics.Debug.WriteLine($"? Window subclassed");

            // Load custom icon
            var icon = LoadTrayIcon();
            _trayHIcon = icon?.Handle ?? IntPtr.Zero;

            // Create notification icon data
            _nid = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP,
                uCallbackMessage = WM_USER + 1,
                hIcon = _trayHIcon != IntPtr.Zero ? _trayHIcon : System.Drawing.SystemIcons.Application.Handle,
                szTip = "Snowblind-Mod Player"
            };

            // Add icon to tray
            if (Shell_NotifyIcon(NIM_ADD, ref _nid))
            {
                System.Diagnostics.Debug.WriteLine("? Tray icon added successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("? Failed to add tray icon");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? TrayService.Initialize failed: {ex.Message}");
        }
    }

    private IntPtr WndProcHandler(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // Check for tray icon callback messages
        if (msg == WM_USER + 1)
        {
            // lParam contains the message; mask low word
            uint message = (uint)(lParam.ToInt64() & 0xFFFF);
            
            System.Diagnostics.Debug.WriteLine($"? Tray message received: {message} (LBtn={WM_LBUTTONDBLCLK}, RBtn={WM_RBUTTONUP})");
            
            // Double-click: show window
            if (message == WM_LBUTTONDBLCLK)
            {
                System.Diagnostics.Debug.WriteLine("? Tray icon double-click detected");
                _onShowRequested?.Invoke();
                return IntPtr.Zero;
            }
            // Right-click: show context menu
            else if (message == WM_RBUTTONUP)
            {
                System.Diagnostics.Debug.WriteLine("? Tray icon right-click detected");
                ShowContextMenu();
                return IntPtr.Zero;
            }
        }

        // Call the original window procedure
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private Icon? LoadTrayIcon()
    {
        try
        {
            // Prefer embedded .ico (multi-size) for native tray
            var packUri = "pack://application:,,,/Assets/Icon.ico";
            var resource = System.Windows.Application.GetResourceStream(new Uri(packUri));
            if (resource?.Stream != null)
            {
                // Load specific icon size for tray (32x32 for better visibility)
                var icon = new Icon(resource.Stream, new System.Drawing.Size(32, 32));
                
                System.Diagnostics.Debug.WriteLine($"? Custom tray icon loaded from resources (32x32)");
                return icon;
            }

            // Fallback: PNG
            var pngUri = "pack://application:,,,/Assets/tray_icon.png";
            var pngResource = System.Windows.Application.GetResourceStream(new Uri(pngUri));
            if (pngResource?.Stream != null)
            {
                using var bmp = new Bitmap(pngResource.Stream);
                var hicon = bmp.GetHicon();
                var icon = Icon.FromHandle(hicon);
                System.Diagnostics.Debug.WriteLine("? Custom tray icon loaded from tray_icon.png");
                return icon;
            }

            // Try file system fallback
            var filePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icon.ico");
            if (File.Exists(filePath))
            {
                var icon = new Icon(filePath, new System.Drawing.Size(32, 32));
                System.Diagnostics.Debug.WriteLine($"? Custom tray icon loaded from file: {filePath} (32x32)");
                return icon;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to load custom icon: {ex.Message}");
        }

        return null;
    }

    private async void ShowContextMenu()
    {
        try
        {
            var menu = CreatePopupMenu();
            if (menu == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("? Failed to create context menu");
                return;
            }

            // Main menu items (per SPEC_FINAL 5.1)
            AppendMenu(menu, MF_STRING, (IntPtr)CMD_SHOW, "Show");
            AppendMenu(menu, MF_STRING, (IntPtr)CMD_PLAY_DEFAULT, "Play Default");
            
            // Videos submenu
            var videosMenu = CreatePopupMenu();
            var videos = _getVideosForMenu != null ? await _getVideosForMenu() : new List<VideoItem>();
            
            if (videos.Count == 0)
            {
                AppendMenu(videosMenu, MF_STRING, IntPtr.Zero, "(no videos)");
            }
            else
            {
                // List already sorted: default first, then alphabetical
                for (int i = 0; i < videos.Count; i++)
                {
                    var video = videos[i];
                    // Mark default video with [DEFAULT] prefix for better compatibility
                    var displayName = video.IsDefault ? $"[DEFAULT] {video.DisplayName}" : video.DisplayName;
                    AppendMenu(videosMenu, MF_STRING, (IntPtr)(CMD_VIDEO_BASE + i), displayName);
                }
            }
            
            // Attach videos submenu to main menu (MF_POPUP flag)
            AppendMenu(menu, MF_POPUP, videosMenu, "Play Video");
            
            AppendMenu(menu, MF_STRING, (IntPtr)CMD_STOP, "Stop");
            AppendMenu(menu, MF_SEPARATOR, IntPtr.Zero, null);
            AppendMenu(menu, MF_STRING, (IntPtr)CMD_EXIT, "Exit");

            // Get cursor position and track menu
            GetCursorPos(out POINT pt);
            SetForegroundWindow(_hwnd);
            int cmd = TrackPopupMenuEx(menu, TPM_LEFTALIGN | TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.x, pt.y, _hwnd, IntPtr.Zero);
            System.Diagnostics.Debug.WriteLine($"? Context menu command: {cmd}");

            // Handle command
            HandleMenuCommand(cmd, videos);

            DestroyMenu(menu);
            DestroyMenu(videosMenu);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Failed to show context menu: {ex.Message}");
        }
    }

    private async void HandleMenuCommand(int cmd, List<VideoItem> videos)
    {
        try
        {
            if (cmd == CMD_SHOW)
            {
                _onShowRequested?.Invoke();
            }
            else if (cmd == CMD_PLAY_DEFAULT)
            {
                if (_onPlayDefaultRequested != null)
                    await _onPlayDefaultRequested();
            }
            else if (cmd == CMD_STOP)
            {
                if (_onStopRequested != null)
                    await _onStopRequested();
            }
            else if (cmd == CMD_EXIT)
            {
                _onExitRequested?.Invoke();
            }
            else if (cmd >= CMD_VIDEO_BASE && cmd < CMD_VIDEO_BASE + 100)
            {
                var index = cmd - CMD_VIDEO_BASE;
                if (index < videos.Count && _onPlayVideoRequested != null)
                {
                    var sorted = videos.OrderBy(v => v.DisplayName).ToList();
                    await _onPlayVideoRequested(sorted[index].Id);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error handling menu command {cmd}: {ex.Message}");
        }
    }

    public void ShowNotification(string title, string message)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"?? ShowNotification called: title='{title}', message='{message}'");
            System.Diagnostics.Debug.WriteLine($"   App visibility: MainWindow={Application.Current?.MainWindow?.IsVisible}, ShowInTaskbar={Application.Current?.MainWindow?.ShowInTaskbar}");
            
            // Ensure flags include NIF_INFO
            _nid.uFlags |= NIF_INFO;
            _nid.szInfo = message;
            _nid.szInfoTitle = title;
            _nid.dwInfoFlags = NIIF_INFO; // Use NIIF_INFO for informational icon

            System.Diagnostics.Debug.WriteLine($"   Calling Shell_NotifyIcon(NIM_MODIFY)...");
            
            bool success = Shell_NotifyIcon(NIM_MODIFY, ref _nid);
            
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"? Shell_NotifyIcon succeeded - toast should appear");
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"? Shell_NotifyIcon FAILED - Win32 Error: {errorCode}");
                System.Diagnostics.Debug.WriteLine($"   hWnd: {_nid.hWnd}, uID: {_nid.uID}, uFlags: {_nid.uFlags}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? ShowNotification exception: {ex.Message}");
        }
    }

    public void SetMainWindowVisible(bool isVisible)
    {
        var window = System.Windows.Application.Current?.MainWindow;
        if (window == null) return;

        if (isVisible)
        {
            window.ShowInTaskbar = true;
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
            window.Focus();
        }
        else
        {
            window.ShowInTaskbar = false;
            window.Hide();
        }
    }

    public void Dispose()
    {
        try
        {
            if (_hwnd != IntPtr.Zero)
            {
                // Restore old window proc
                if (_oldWndProc != IntPtr.Zero)
                {
                    SetWindowLongPtr(_hwnd, GWL_WNDPROC, _oldWndProc);
                }

                Shell_NotifyIcon(NIM_DELETE, ref _nid);
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            if (_trayHIcon != IntPtr.Zero)
            {
                DestroyIcon(_trayHIcon);
                _trayHIcon = IntPtr.Zero;
            }

            _wndProcDelegate = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error disposing TrayService: {ex.Message}");
        }
    }
}
