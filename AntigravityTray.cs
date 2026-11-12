using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace AntigravityTray
{
    static class Program
    {
        private static Mutex _mutex;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_HOTKEY = 0x0312;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                SetProcessDpiAwarenessContext((IntPtr)(-4));
            }
            catch
            {
                try { SetProcessDPIAware(); } catch { }
            }

            if (args != null && args.Length > 0 && args[0].Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
            {
                Uninstaller.Run();
                return;
            }

            bool isNewInstance;
            _mutex = new Mutex(true, "Global\\AntigravityTray_Companion_Mutex", out isNewInstance);

            if (!isNewInstance)
            {
                // Another instance is already running. Signal existing window and exit gracefully.
                // This prevents modal MessageBox dialogs from hanging process orchestrators like ProcessPilot.
                IntPtr hMsgWnd = FindWindow(null, "AntigravityTray_MsgWnd");
                if (hMsgWnd != IntPtr.Zero)
                {
                    PostMessage(hMsgWnd, WM_HOTKEY, (IntPtr)9001, IntPtr.Zero);
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());
        }
    }

    public class TrayApplicationContext : ApplicationContext
    {
        // Win32 Constants
        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const uint WM_CLOSE = 0x0010;
        private const uint WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int VK_F4 = 0x73;
        private const int VK_MENU = 0x12; // Alt key
        private const uint GA_ROOT = 2;
        private const uint SC_CLOSE = 0xF060;
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint MF_DISABLED = 0x00000002;
        private const uint MF_ENABLED = 0x00000000;

        // P/Invoke
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // Fields
        private readonly NotifyIcon _trayIcon;
        private readonly System.Windows.Forms.Timer _monitorTimer;
        private readonly HiddenMessageWindow _messageWindow;
        private bool _firstMinimizeBalloonShown = false;
        private bool _firstCloseBalloonShown = false;
        private bool _autoHideOnStartup = false;
        private IntPtr _targetHwnd = IntPtr.Zero;
        private int _targetPid = 0;
        private string _configuredUnlockerPath = null;
        private bool _closeToTrayEnabled = true;

        // Hooks
        private readonly HookProc _mouseHookProc;
        private readonly HookProc _keyboardHookProc;
        private IntPtr _mouseHookHandle = IntPtr.Zero;
        private IntPtr _keyboardHookHandle = IntPtr.Zero;

        // Constants
        public const string APP_VERSION = "1.1.14";
        public const string GITHUB_REPO = "gda96/AntigravityTray";
        private readonly SynchronizationContext _syncContext;
        private UpdateInfo _pendingUpdateInfo = null;
        private bool _isCheckingUpdates = false;

        // UI Menu items
        private readonly ToolStripMenuItem _itemStatus;
        private readonly ToolStripMenuItem _itemPath;
        private readonly ToolStripMenuItem _itemToggle;
        private readonly ToolStripMenuItem _itemLaunch;
        private readonly ToolStripMenuItem _itemRestart;
        private readonly ToolStripMenuItem _itemSettings;
        private readonly ToolStripMenuItem _itemCloseToTray;
        private readonly ToolStripMenuItem _itemAutostart;
        private readonly ToolStripMenuItem _itemCheckUpdates;

        public TrayApplicationContext()
        {
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _configuredUnlockerPath = GetConfiguredUnlockerPath();
            _closeToTrayEnabled = IsCloseToTrayEnabled();

            // Hotkey Window (Ctrl + Shift + A)
            _messageWindow = new HiddenMessageWindow(this);
            RegisterHotKey(_messageWindow.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)Keys.A);

            // Install low-level hooks for intercepting close button [X] and Alt+F4
            _mouseHookProc = LowLevelMouseCallback;
            _keyboardHookProc = LowLevelKeyboardCallback;
            InstallHooks();

            // Context Menu
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = SystemFonts.MenuFont ?? new Font("Segoe UI", 9F, FontStyle.Regular);
            menu.RenderMode = ToolStripRenderMode.System;

            _itemStatus = new ToolStripMenuItem("Статус: Определение...") { Enabled = false };
            menu.Items.Add(_itemStatus);

            _itemPath = new ToolStripMenuItem(GetShortPathDisplay(_configuredUnlockerPath)) { Enabled = false };
            menu.Items.Add(_itemPath);

            menu.Items.Add(new ToolStripSeparator());

            _itemToggle = new ToolStripMenuItem("Показать / Скрыть", null, OnToggleClick)
            {
                Font = new Font(menu.Font, FontStyle.Bold)
            };
            menu.Items.Add(_itemToggle);

            _itemLaunch = new ToolStripMenuItem("Запустить Unlocker", null, OnLaunchClick);
            menu.Items.Add(_itemLaunch);

            _itemRestart = new ToolStripMenuItem("Перезапустить Unlocker", null, OnRestartClick);
            menu.Items.Add(_itemRestart);

            menu.Items.Add(new ToolStripSeparator());

            _itemCloseToTray = new ToolStripMenuItem("Сворачивать при нажатии «Х» (вместо закрытия)", null, OnCloseToTrayToggleClick);
            _itemCloseToTray.Checked = _closeToTrayEnabled;
            menu.Items.Add(_itemCloseToTray);

            _itemSettings = new ToolStripMenuItem("⚙️ Настройки: указать путь к Unlocker...", null, OnSettingsClick);
            menu.Items.Add(_itemSettings);

            _itemAutostart = new ToolStripMenuItem("Автозапуск с Windows", null, OnAutostartToggleClick);
            _itemAutostart.Checked = IsAutostartEnabled();
            menu.Items.Add(_itemAutostart);

            _itemCheckUpdates = new ToolStripMenuItem("🔄 Проверить обновления...", null, OnCheckUpdatesClick);
            menu.Items.Add(_itemCheckUpdates);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem itemExit = new ToolStripMenuItem("Выход", null, OnExitClick);
            menu.Items.Add(itemExit);

            // Icon
            Icon appIcon = LoadAppIcon();

            _trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                ContextMenuStrip = menu,
                Text = "Antigravity Unlocker (Запуск...)",
                Visible = true
            };

            _trayIcon.MouseClick += OnTrayIconMouseClick;
            _trayIcon.DoubleClick += OnTrayIconDoubleClick;
            _trayIcon.BalloonTipClicked += OnTrayBalloonClicked;

            // Monitor Timer (1 second interval for 0% CPU footprint)
            _monitorTimer = new System.Windows.Forms.Timer();
            _monitorTimer.Interval = 1000;
            _monitorTimer.Tick += OnMonitorTick;
            _monitorTimer.Start();

            // Initial background update check after 3 seconds so startup remains instant
            System.Windows.Forms.Timer initialUpdateTimer = new System.Windows.Forms.Timer();
            initialUpdateTimer.Interval = 3000;
            initialUpdateTimer.Tick += (s, e) =>
            {
                initialUpdateTimer.Stop();
                initialUpdateTimer.Dispose();
                CheckForUpdates(true);
            };
            initialUpdateTimer.Start();

            // Periodic update check every 6 hours
            System.Windows.Forms.Timer periodicUpdateTimer = new System.Windows.Forms.Timer();
            periodicUpdateTimer.Interval = 6 * 3600 * 1000;
            periodicUpdateTimer.Tick += (s, e) => CheckForUpdates(true);
            periodicUpdateTimer.Start();

            // Auto-launch Unlocker if configured and not already running
            CheckAndAutoLaunchUnlocker();

            _trayIcon.ShowBalloonTip(
                2000,
                "Antigravity Tray активен",
                "Кликните для скрытия/разворачивания или используйте Ctrl + Shift + A.",
                ToolTipIcon.Info
            );
        }

        private void CheckAndAutoLaunchUnlocker()
        {
            IntPtr hwnd = FindUnlockerWindow();
            if (hwnd == IntPtr.Zero)
            {
                if (!string.IsNullOrEmpty(_configuredUnlockerPath) && File.Exists(_configuredUnlockerPath))
                {
                    _autoHideOnStartup = true;
                    LaunchUnlocker();
                }
            }
        }

        private void InstallHooks()
        {
            try
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    IntPtr hMod = GetModuleHandle(curModule.ModuleName);
                    _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, hMod, 0);
                    _keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProc, hMod, 0);
                }
            }
            catch { }
        }

        private void UninstallHooks()
        {
            if (_mouseHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookHandle);
                _mouseHookHandle = IntPtr.Zero;
            }
            if (_keyboardHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookHandle);
                _keyboardHookHandle = IntPtr.Zero;
            }
        }

        private IntPtr LowLevelMouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                if (msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP)
                {
                    if (_closeToTrayEnabled && _targetHwnd != IntPtr.Zero && IsWindow(_targetHwnd))
                    {
                        MSLLHOOKSTRUCT mouseData = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                        RECT r;
                        if (GetWindowRect(_targetHwnd, out r))
                        {
                            // Generous close button bounds (75px wide, 50px high) to reliably capture across all DPI scales
                            int closeW = 75;
                            int closeH = 50;

                            bool inCloseX = (mouseData.pt.x >= (r.Right - closeW) && mouseData.pt.x <= r.Right + 5);
                            bool inCloseY = (mouseData.pt.y >= (r.Top - 5) && mouseData.pt.y <= (r.Top + closeH));

                            if (inCloseX && inCloseY)
                            {
                                IntPtr wndAtPoint = WindowFromPoint(mouseData.pt);
                                if (wndAtPoint == _targetHwnd || GetAncestor(wndAtPoint, GA_ROOT) == _targetHwnd)
                                {
                                    if (msg == WM_LBUTTONDOWN)
                                    {
                                        // Hide window to tray immediately on press
                                        ShowWindow(_targetHwnd, SW_HIDE);

                                        if (!_firstCloseBalloonShown)
                                        {
                                            _firstCloseBalloonShown = true;
                                            _trayIcon.ShowBalloonTip(
                                                2500,
                                                "Antigravity Unlocker",
                                                "Окно спрятано в трей при нажатии крестика и работает в фоне. Для закрытия выберите «Выход» в меню трея.",
                                                ToolTipIcon.Info
                                            );
                                        }
                                    }

                                    // SWALLOW BOTH BUTTON DOWN AND BUTTON UP so DWM never triggers close!
                                    return (IntPtr)1;
                                }
                            }
                        }
                    }
                }
            }

            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        private IntPtr LowLevelKeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN || msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    if (_closeToTrayEnabled && _targetHwnd != IntPtr.Zero && IsWindow(_targetHwnd))
                    {
                        KBDLLHOOKSTRUCT kbdData = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                        // Check for Alt + F4
                        if (kbdData.vkCode == VK_F4 && (GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
                        {
                            IntPtr fgWnd = GetForegroundWindow();
                            uint fgPid = 0;
                            if (fgWnd != IntPtr.Zero)
                            {
                                GetWindowThreadProcessId(fgWnd, out fgPid);
                            }

                            bool isFgUnlocker = (fgWnd == _targetHwnd || 
                                                GetAncestor(fgWnd, GA_ROOT) == _targetHwnd || 
                                                (_targetPid > 0 && fgPid == (uint)_targetPid));

                            if (isFgUnlocker)
                            {
                                if (msg == WM_SYSKEYDOWN || msg == WM_KEYDOWN)
                                {
                                    ShowWindow(_targetHwnd, SW_HIDE);

                                    if (!_firstCloseBalloonShown)
                                    {
                                        _firstCloseBalloonShown = true;
                                        _trayIcon.ShowBalloonTip(
                                            2500,
                                            "Antigravity Unlocker",
                                            "Окно спрятано в трей (Alt + F4) и работает в фоне.",
                                            ToolTipIcon.Info
                                        );
                                    }
                                }

                                return (IntPtr)1; // Swallow Alt+F4 completely so Unlocker never closes!
                            }
                        }
                    }
                }
            }

            return CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }

        private string GetShortPathDisplay(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return "Путь не задан";
            try
            {
                return "Файл: " + Path.GetFileName(fullPath);
            }
            catch
            {
                return fullPath;
            }
        }

        private Icon LoadAppIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch { }

            try
            {
                Icon exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (exeIcon != null) return exeIcon;
            }
            catch { }

            return SystemIcons.Application;
        }

        private void OnMonitorTick(object sender, EventArgs e)
        {
            // Verify existing cached window and process
            if (_targetHwnd != IntPtr.Zero)
            {
                if (!IsWindow(_targetHwnd))
                {
                    _targetHwnd = IntPtr.Zero;
                    _targetPid = 0;
                }
                else if (_targetPid > 0)
                {
                    try
                    {
                        Process p = Process.GetProcessById(_targetPid);
                        if (p.HasExited)
                        {
                            _targetHwnd = IntPtr.Zero;
                            _targetPid = 0;
                        }
                    }
                    catch
                    {
                        _targetHwnd = IntPtr.Zero;
                        _targetPid = 0;
                    }
                }
            }

            // Only perform EnumWindows search if we do NOT currently track a valid window
            IntPtr hwnd = _targetHwnd;
            if (hwnd == IntPtr.Zero)
            {
                hwnd = FindUnlockerWindow();
                _targetHwnd = hwnd;
            }

            if (hwnd != IntPtr.Zero)
            {
                _targetHwnd = hwnd;

                if (_autoHideOnStartup)
                {
                    _autoHideOnStartup = false;
                    ShowWindow(hwnd, SW_HIDE);
                    _trayIcon.ShowBalloonTip(
                        2500,
                        "Antigravity Unlocker",
                        "Программа автоматически запущена от имени Администратора и работает в трее.",
                        ToolTipIcon.Info
                    );
                }

                if (_closeToTrayEnabled)
                {
                    IntPtr hMenu = GetSystemMenu(hwnd, false);
                    if (hMenu != IntPtr.Zero)
                    {
                        EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_DISABLED | MF_GRAYED);
                    }
                }

                if (IsIconic(hwnd))
                {
                    ShowWindow(hwnd, SW_HIDE);

                    if (!_firstMinimizeBalloonShown)
                    {
                        _firstMinimizeBalloonShown = true;
                        _trayIcon.ShowBalloonTip(
                            2500,
                            "Antigravity Unlocker",
                            "Окно свёрнуто в трей и продолжает работать в фоне. Кликните по иконке у часов, чтобы развернуть.",
                            ToolTipIcon.Info
                        );
                    }
                }

                bool isVis = IsWindowVisible(hwnd);
                if (isVis)
                {
                    _itemStatus.Text = "Статус: Окно активно на экране";
                    _itemToggle.Text = "Скрыть в трей";
                    UpdateTrayText("Antigravity Unlocker (Активен на экране)");
                }
                else
                {
                    _itemStatus.Text = "Статус: Работает в трее (в фоне)";
                    _itemToggle.Text = "Развернуть окно";
                    UpdateTrayText("Antigravity Unlocker (Работает в фоне)");
                }

                _itemLaunch.Enabled = false;
                _itemRestart.Enabled = true;
            }
            else
            {
                _targetHwnd = IntPtr.Zero;
                _targetPid = 0;
                _itemStatus.Text = "Статус: Unlocker не запущен";
                _itemToggle.Text = "Развернуть / Запустить";
                _itemLaunch.Enabled = true;
                _itemRestart.Enabled = false;
                UpdateTrayText("Antigravity Unlocker (Не запущен)");
            }

            _itemPath.Text = GetShortPathDisplay(_configuredUnlockerPath);
        }

        private void UpdateTrayText(string text)
        {
            if (text.Length > 63) text = text.Substring(0, 63);
            if (_trayIcon.Text != text)
            {
                _trayIcon.Text = text;
            }
        }

        public void ToggleVisibility()
        {
            IntPtr hwnd = FindUnlockerWindow();
            if (hwnd == IntPtr.Zero)
            {
                _targetHwnd = IntPtr.Zero;
                _targetPid = 0;
                LaunchUnlocker();
                return;
            }

            if (IsWindowVisible(hwnd) && !IsIconic(hwnd))
            {
                ShowWindow(hwnd, SW_HIDE);
            }
            else
            {
                ShowWindow(hwnd, SW_RESTORE);
                ShowWindow(hwnd, SW_SHOW);
                SetForegroundWindow(hwnd);
            }
        }

        private void OnTrayIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ToggleVisibility();
            }
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            ToggleVisibility();
        }

        private void OnToggleClick(object sender, EventArgs e)
        {
            ToggleVisibility();
        }

        private void OnLaunchClick(object sender, EventArgs e)
        {
            LaunchUnlocker();
        }

        private void OnRestartClick(object sender, EventArgs e)
        {
            KillUnlocker();
            Thread.Sleep(600);
            LaunchUnlocker();
        }

        private void OnCloseToTrayToggleClick(object sender, EventArgs e)
        {
            _closeToTrayEnabled = !_closeToTrayEnabled;
            _itemCloseToTray.Checked = _closeToTrayEnabled;
            SetCloseToTrayEnabled(_closeToTrayEnabled);

            if (_targetHwnd != IntPtr.Zero && IsWindow(_targetHwnd))
            {
                if (_closeToTrayEnabled)
                {
                    IntPtr hMenu = GetSystemMenu(_targetHwnd, false);
                    if (hMenu != IntPtr.Zero)
                    {
                        EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_DISABLED | MF_GRAYED);
                    }
                }
                else
                {
                    GetSystemMenu(_targetHwnd, true);
                }
            }
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Укажите исполняемый файл Antigravity Unlocker";
                ofd.Filter = "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*";
                if (!string.IsNullOrEmpty(_configuredUnlockerPath) && File.Exists(_configuredUnlockerPath))
                {
                    ofd.InitialDirectory = Path.GetDirectoryName(_configuredUnlockerPath);
                    ofd.FileName = Path.GetFileName(_configuredUnlockerPath);
                }
                else
                {
                    ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _configuredUnlockerPath = ofd.FileName;
                    SaveConfiguredUnlockerPath(_configuredUnlockerPath);
                    _itemPath.Text = GetShortPathDisplay(_configuredUnlockerPath);

                    MessageBox.Show(
                        "Путь к Antigravity Unlocker сохранён:\n" + _configuredUnlockerPath,
                        "Настройки сохранены",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
        }

        private void OnAutostartToggleClick(object sender, EventArgs e)
        {
            bool currentState = IsAutostartEnabled();
            SetAutostart(!currentState);
            _itemAutostart.Checked = !currentState;
        }

        private void OnCheckUpdatesClick(object sender, EventArgs e)
        {
            if (_pendingUpdateInfo != null)
            {
                ShowUpdateDialog(_pendingUpdateInfo);
            }
            else
            {
                CheckForUpdates(false);
            }
        }

        private void OnTrayBalloonClicked(object sender, EventArgs e)
        {
            if (_pendingUpdateInfo != null)
            {
                ShowUpdateDialog(_pendingUpdateInfo);
            }
        }

        public void CheckForUpdates(bool silent)
        {
            if (_isCheckingUpdates)
            {
                if (!silent)
                {
                    MessageBox.Show(
                        "Проверка обновлений уже выполняется...",
                        "Обновление",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                return;
            }

            _isCheckingUpdates = true;
            if (!silent)
            {
                _itemCheckUpdates.Text = "🔄 Проверка обновлений...";
                _itemCheckUpdates.Enabled = false;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                UpdateInfo info = null;
                string errorMsg = null;

                try
                {
                    info = FetchLatestReleaseInfo(GITHUB_REPO);
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                }

                _syncContext.Post(__ =>
                {
                    _isCheckingUpdates = false;
                    _itemCheckUpdates.Enabled = true;

                    if (info != null && IsNewerVersion(info.TagName, APP_VERSION))
                    {
                        _pendingUpdateInfo = info;
                        _itemCheckUpdates.Text = "⭐ Доступно обновление " + info.TagName + "!";
                        _itemCheckUpdates.Font = new Font(_itemCheckUpdates.Font, FontStyle.Bold);

                        if (silent)
                        {
                            _trayIcon.ShowBalloonTip(
                                5000,
                                "Доступно обновление Antigravity Tray " + info.TagName,
                                "Нажмите здесь, чтобы посмотреть список изменений и обновить программу.",
                                ToolTipIcon.Info
                            );
                        }
                        else
                        {
                            ShowUpdateDialog(info);
                        }
                    }
                    else
                    {
                        _itemCheckUpdates.Text = "🔄 Проверить обновления...";
                        _itemCheckUpdates.Font = new Font(_itemCheckUpdates.Font, FontStyle.Regular);

                        if (!silent)
                        {
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                MessageBox.Show(
                                    "Не удалось проверить обновления:\n" + errorMsg + "\n\nПроверьте подключение к Интернету или посетите страницу GitHub.",
                                    "Проверка обновлений",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                            }
                            else
                            {
                                MessageBox.Show(
                                    "У вас установлена самая последняя актуальная версия Antigravity Tray Companion (v" + APP_VERSION + ").\n\nОбновлений не требуется.",
                                    "Обновление программы",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );
                            }
                        }
                    }
                }, null);
            });
        }

        private void ShowUpdateDialog(UpdateInfo info)
        {
            try
            {
                using (UpdateForm form = new UpdateForm(info, LoadAppIcon()))
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка открытия окна обновления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static UpdateInfo FetchLatestReleaseInfo(string repo)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (WebClient client = new WebClient())
            {
                client.Headers["User-Agent"] = "AntigravityTray-Updater/" + APP_VERSION;
                client.Headers["Accept"] = "application/vnd.github.v3+json";
                client.Encoding = Encoding.UTF8;

                string url = "https://api.github.com/repos/" + repo + "/releases/latest";
                string json = client.DownloadString(url);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Dictionary<string, object> dict = serializer.Deserialize<Dictionary<string, object>>(json);

                if (dict == null) return null;

                UpdateInfo info = new UpdateInfo();
                info.TagName = dict.ContainsKey("tag_name") ? Convert.ToString(dict["tag_name"]) : "";
                info.Version = info.TagName.TrimStart('v', 'V');
                info.Title = dict.ContainsKey("name") ? Convert.ToString(dict["name"]) : info.TagName;
                info.Changelog = dict.ContainsKey("body") ? Convert.ToString(dict["body"]) : "";
                info.HtmlUrl = dict.ContainsKey("html_url") ? Convert.ToString(dict["html_url"]) : "https://github.com/" + repo;

                if (dict.ContainsKey("assets") && dict["assets"] is ArrayList)
                {
                    ArrayList assets = (ArrayList)dict["assets"];
                    foreach (object item in assets)
                    {
                        if (item is Dictionary<string, object>)
                        {
                            Dictionary<string, object> asset = (Dictionary<string, object>)item;
                            string assetName = asset.ContainsKey("name") ? Convert.ToString(asset["name"]) : "";
                            string assetUrl = asset.ContainsKey("browser_download_url") ? Convert.ToString(asset["browser_download_url"]) : "";
                            long assetSize = 0;
                            if (asset.ContainsKey("size")) long.TryParse(Convert.ToString(asset["size"]), out assetSize);

                            if (assetName.Equals("AntigravityTray_Setup.exe", StringComparison.OrdinalIgnoreCase))
                            {
                                info.SetupDownloadUrl = assetUrl;
                                info.SetupSize = assetSize;
                            }
                            else if (assetName.Equals("AntigravityTray.exe", StringComparison.OrdinalIgnoreCase))
                            {
                                info.ExeDownloadUrl = assetUrl;
                                info.ExeSize = assetSize;
                            }
                        }
                    }
                }

                return info;
            }
        }

        public static bool IsNewerVersion(string latestTag, string currentVersion)
        {
            if (string.IsNullOrEmpty(latestTag)) return false;
            string vClean = latestTag.Trim().TrimStart('v', 'V');
            string cClean = currentVersion.Trim().TrimStart('v', 'V');

            Version vLatest, vCurrent;
            if (Version.TryParse(NormalizeVersionString(vClean), out vLatest) &&
                Version.TryParse(NormalizeVersionString(cClean), out vCurrent))
            {
                return vLatest > vCurrent;
            }

            return string.Compare(vClean, cClean, StringComparison.OrdinalIgnoreCase) > 0;
        }

        private static string NormalizeVersionString(string v)
        {
            string[] parts = v.Split('.');
            if (parts.Length == 1) return v + ".0.0.0";
            if (parts.Length == 2) return v + ".0.0";
            if (parts.Length == 3) return v + ".0";
            return v;
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            _monitorTimer.Stop();

            if (_targetHwnd != IntPtr.Zero && IsWindow(_targetHwnd))
            {
                GetSystemMenu(_targetHwnd, true);
            }

            DialogResult result = MessageBox.Show(
                "Закрыть Antigravity Unlocker перед выходом?",
                "Выход из Antigravity Tray",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Cancel)
            {
                _monitorTimer.Start();
                return;
            }

            if (result == DialogResult.Yes)
            {
                KillUnlocker();
            }

            UninstallHooks();
            UnregisterHotKey(_messageWindow.Handle, HOTKEY_ID);
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }

        public void ExitWithoutPrompt()
        {
            _monitorTimer.Stop();
            UninstallHooks();
            UnregisterHotKey(_messageWindow.Handle, HOTKEY_ID);
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }

        private IntPtr FindUnlockerWindow()
        {
            IntPtr foundHwnd = IntPtr.Zero;

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                // Lightning fast filter 1: Class check (Rust egui/winit is always "Window Class")
                StringBuilder sbClass = new StringBuilder(64);
                GetClassName(hWnd, sbClass, 64);
                string cls = sbClass.ToString();
                if (cls != "Window Class") return true;

                // Lightning fast filter 2: Title check (Rust egui starts with "Antigravity Unlocker")
                StringBuilder sbTitle = new StringBuilder(256);
                GetWindowText(hWnd, sbTitle, 256);
                string title = sbTitle.ToString();
                if (string.IsNullOrEmpty(title) || !title.StartsWith("Antigravity Unlocker", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Filter 3: Process check (only executed for actual candidates!)
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (pid == Process.GetCurrentProcess().Id) return true;

                Process p = null;
                try
                {
                    p = Process.GetProcessById((int)pid);
                }
                catch
                {
                    return true;
                }

                if (p == null) return true;

                string pName = p.ProcessName.ToLowerInvariant();

                // Strict exclusion: never match browsers or proxy service
                if (pName == "ag_dns" || pName == "browser" || pName == "chrome" || pName == "msedge") return true;

                // Verify Unlocker process
                bool isUnlocker = false;
                if (!string.IsNullOrEmpty(_configuredUnlockerPath))
                {
                    try
                    {
                        if (string.Equals(p.MainModule.FileName, _configuredUnlockerPath, StringComparison.OrdinalIgnoreCase))
                        {
                            isUnlocker = true;
                        }
                    }
                    catch { }
                }

                if (!isUnlocker && (pName.StartsWith("ag_") || pName == "ag_unlocker" || pName == "antigravity_unlocker"))
                {
                    isUnlocker = true;
                }

                if (isUnlocker)
                {
                    _targetPid = (int)pid;
                    foundHwnd = hWnd;
                    return false; // Found! Stop enumeration immediately.
                }

                return true;
            }, IntPtr.Zero);

            return foundHwnd;
        }

        private void KillUnlocker()
        {
            if (_targetHwnd != IntPtr.Zero)
            {
                SendMessage(_targetHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }

            if (_targetPid > 0)
            {
                try
                {
                    Process p = Process.GetProcessById(_targetPid);
                    if (!p.WaitForExit(700))
                    {
                        p.Kill();
                    }
                }
                catch { }
            }

            Process[] allProcs = Process.GetProcesses();
            foreach (Process p in allProcs)
            {
                try
                {
                    string pName = p.ProcessName.ToLowerInvariant();
                    // NEVER kill the background proxy service (ag_dns)!
                    if (pName == "ag_dns") continue;

                    if (pName.StartsWith("ag_") || pName == "ag_unlocker" || pName == "antigravity_unlocker")
                    {
                        p.Kill();
                    }
                }
                catch { }
            }
        }

        private string GetConfiguredUnlockerPath()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\AntigravityTray"))
                {
                    if (key != null)
                    {
                        string path = key.GetValue("UnlockerPath") as string;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            return path;
                        }
                    }
                }
            }
            catch { }

            string[] scanDirs = new string[] {
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            foreach (string dir in scanDirs)
            {
                if (!Directory.Exists(dir)) continue;

                try
                {
                    string[] files = Directory.GetFiles(dir, "AG_*.exe");
                    if (files.Length > 0)
                    {
                        Array.Sort(files);
                        string latest = files[files.Length - 1];
                        SaveConfiguredUnlockerPath(latest);
                        return latest;
                    }

                    files = Directory.GetFiles(dir, "*unlocker*.exe");
                    if (files.Length > 0)
                    {
                        string latest = files[0];
                        SaveConfiguredUnlockerPath(latest);
                        return latest;
                    }
                }
                catch { }
            }

            return null;
        }

        private void SaveConfiguredUnlockerPath(string path)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AntigravityTray"))
                {
                    if (key != null)
                    {
                        key.SetValue("UnlockerPath", path);
                    }
                }
            }
            catch { }
        }

        private bool IsCloseToTrayEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\AntigravityTray"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("CloseToTray");
                        if (val != null) return Convert.ToInt32(val) != 0;
                    }
                }
            }
            catch { }
            return true; // Default: enabled
        }

        private void SetCloseToTrayEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AntigravityTray"))
                {
                    if (key != null)
                    {
                        key.SetValue("CloseToTray", enabled ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private void LaunchUnlocker()
        {
            if (string.IsNullOrEmpty(_configuredUnlockerPath) || !File.Exists(_configuredUnlockerPath))
            {
                OnSettingsClick(this, EventArgs.Empty);
            }

            if (!string.IsNullOrEmpty(_configuredUnlockerPath) && File.Exists(_configuredUnlockerPath))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = _configuredUnlockerPath,
                        WorkingDirectory = Path.GetDirectoryName(_configuredUnlockerPath),
                        UseShellExecute = true,
                        Verb = "runas" // Launch developer's program as Administrator
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Не удалось запустить от имени Администратора:\n" + _configuredUnlockerPath + "\n\n" + ex.Message,
                        "Ошибка запуска",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private bool IsAutostartEnabled()
        {
            try
            {
                // 1. Check Task Scheduler task
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = "/query /tn \"AntigravityTray\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(1500);
                        if (p.ExitCode == 0) return true;
                    }
                }
                catch { }

                // 2. Check HKCU Run
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null && key.GetValue("AntigravityTray") != null)
                    {
                        return true;
                    }
                }

                // 3. Check HKLM Run
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null && key.GetValue("AntigravityTray") != null)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void SetAutostart(bool enable)
        {
            try
            {
                if (enable)
                {
                    // Create highest-privilege task for seamless elevated autostart on logon
                    try
                    {
                        ProcessStartInfo psiTask = new ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = string.Format("/create /tn \"AntigravityTray\" /tr \"\\\"{0}\\\"\" /sc onlogon /rl highest /f", Application.ExecutablePath),
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (Process p = Process.Start(psiTask))
                        {
                            p.WaitForExit(3000);
                        }
                    }
                    catch { }

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null) key.SetValue("AntigravityTray", "\"" + Application.ExecutablePath + "\"");
                    }
                }
                else
                {
                    try
                    {
                        ProcessStartInfo psiTask = new ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = "/delete /tn \"AntigravityTray\" /f",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (Process p = Process.Start(psiTask))
                        {
                            p.WaitForExit(3000);
                        }
                    }
                    catch { }

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null) key.DeleteValue("AntigravityTray", false);
                    }

                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null) key.DeleteValue("AntigravityTray", false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось обновить параметр автозапуска:\n" + ex.Message,
                    "Настройки",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private class HiddenMessageWindow : NativeWindow
        {
            private readonly TrayApplicationContext _parent;

            public HiddenMessageWindow(TrayApplicationContext parent)
            {
                _parent = parent;
                CreateParams cp = new CreateParams { Caption = "AntigravityTray_MsgWnd" };
                CreateHandle(cp);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
                {
                    _parent.ToggleVisibility();
                }
                else if (m.Msg == WM_CLOSE)
                {
                    _parent.ExitWithoutPrompt();
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }

    public static class Uninstaller
    {
        public static void Run()
        {
            DialogResult res = MessageBox.Show(
                "Вы действительно хотите удалить Antigravity Tray Companion с этого компьютера?",
                "Удаление Antigravity Tray",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (res != DialogResult.Yes) return;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null) key.DeleteValue("AntigravityTray", false);
                }

                Registry.CurrentUser.DeleteSubKeyTree(@"Software\AntigravityTray", false);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\AntigravityTray", false);

                string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Antigravity Tray.lnk");
                if (File.Exists(desktopShortcut)) File.Delete(desktopShortcut);

                string startMenuShortcut = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    "Antigravity Tray Companion.lnk"
                );
                if (File.Exists(startMenuShortcut)) File.Delete(startMenuShortcut);

                MessageBox.Show(
                    "Antigravity Tray Companion успешно удален.\nПапка программы может быть удалена вручную.",
                    "Удаление завершено",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class UpdateInfo
    {
        public string TagName { get; set; }
        public string Version { get; set; }
        public string Title { get; set; }
        public string Changelog { get; set; }
        public string HtmlUrl { get; set; }
        public string SetupDownloadUrl { get; set; }
        public string ExeDownloadUrl { get; set; }
        public long SetupSize { get; set; }
        public long ExeSize { get; set; }
    }

    public class UpdateForm : Form
    {
        private readonly UpdateInfo _info;
        private readonly ProgressBar _progressBar;
        private readonly Label _lblProgress;
        private readonly Button _btnUpdate;
        private readonly Button _btnGitHub;
        private readonly Button _btnClose;
        private readonly RichTextBox _txtChangelog;
        private WebClient _webClient = null;

        public UpdateForm(UpdateInfo info, Icon icon)
        {
            _info = info;

            Text = "Обновление Antigravity Tray Companion";
            Size = new Size(580, 480);
            MinimumSize = new Size(520, 420);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            BackColor = Color.FromArgb(248, 249, 250);
            if (icon != null) Icon = icon;

            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Banner Panel (Top)
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(24, 28, 36),
                Padding = new Padding(20, 14, 20, 10)
            };

            Label lblTitle = new Label
            {
                Text = "🚀 Доступно обновление " + _info.TagName,
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255),
                AutoSize = true,
                Location = new Point(18, 14)
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = string.Format("Текущая версия: v{0}  •  Новая версия: {1}  •  GitHub Releases",
                    TrayApplicationContext.APP_VERSION, _info.TagName),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 185, 205),
                AutoSize = true,
                Location = new Point(20, 46)
            };
            pnlHeader.Controls.Add(lblSub);
            Controls.Add(pnlHeader);

            // Middle: Changelog
            Label lblChangelogTitle = new Label
            {
                Text = "Что нового в этой версии:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(20, 98),
                AutoSize = true
            };
            Controls.Add(lblChangelogTitle);

            _txtChangelog = new RichTextBox
            {
                Location = new Point(20, 124),
                Size = new Size(524, 210),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Text = string.IsNullOrEmpty(_info.Changelog) ? "Описание обновления не указано." : _info.Changelog.Replace("\r\n", "\n").Replace("\n", "\r\n"),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Controls.Add(_txtChangelog);

            // Bottom Progress section
            _lblProgress = new Label
            {
                Location = new Point(20, 344),
                Size = new Size(524, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 70, 70),
                Text = "",
                Visible = false
            };
            Controls.Add(_lblProgress);

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 366),
                Size = new Size(524, 18),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visible = false
            };
            Controls.Add(_progressBar);

            // Action Buttons Panel
            _btnUpdate = new Button
            {
                Text = "⚡ Обновить сейчас",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(20, 394),
                Size = new Size(160, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            _btnUpdate.Click += OnStartUpdateClick;
            Controls.Add(_btnUpdate);

            _btnGitHub = new Button
            {
                Text = "🌐 Открыть GitHub",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Location = new Point(190, 394),
                Size = new Size(130, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            _btnGitHub.Click += (s, e) =>
            {
                try
                {
                    string target = !string.IsNullOrEmpty(_info.HtmlUrl) ? _info.HtmlUrl : "https://github.com/gda96/AntigravityTray";
                    Process.Start(target);
                }
                catch { }
            };
            Controls.Add(_btnGitHub);

            _btnClose = new Button
            {
                Text = "Позже",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Location = new Point(434, 394),
                Size = new Size(110, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.Cancel
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            CancelButton = _btnClose;
        }

        private void OnStartUpdateClick(object sender, EventArgs e)
        {
            _btnUpdate.Enabled = false;
            _btnClose.Enabled = false;
            _lblProgress.Visible = true;
            _progressBar.Visible = true;
            _progressBar.Value = 0;
            _lblProgress.Text = "Подготовка к загрузке обновления...";

            bool isInstalledInProgramFiles = AppDomain.CurrentDomain.BaseDirectory.StartsWith(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                StringComparison.OrdinalIgnoreCase
            );

            string downloadUrl = "";
            string tempFile = "";
            bool isSetupMode = false;

            if (isInstalledInProgramFiles || !string.IsNullOrEmpty(_info.SetupDownloadUrl))
            {
                if (!string.IsNullOrEmpty(_info.SetupDownloadUrl))
                {
                    downloadUrl = _info.SetupDownloadUrl;
                    tempFile = Path.Combine(Path.GetTempPath(), "AntigravityTray_Setup_New.exe");
                    isSetupMode = true;
                }
                else
                {
                    downloadUrl = _info.ExeDownloadUrl;
                    tempFile = Path.Combine(Path.GetTempPath(), "AntigravityTray_New.exe");
                    isSetupMode = false;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(_info.ExeDownloadUrl))
                {
                    downloadUrl = _info.ExeDownloadUrl;
                    tempFile = Path.Combine(Path.GetTempPath(), "AntigravityTray_New.exe");
                    isSetupMode = false;
                }
                else
                {
                    downloadUrl = _info.SetupDownloadUrl;
                    tempFile = Path.Combine(Path.GetTempPath(), "AntigravityTray_Setup_New.exe");
                    isSetupMode = true;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl))
            {
                MessageBox.Show(
                    "В данном релизе не найден файл установщика или исполняемый файл.\n\nПожалуйста, перейдите на страницу релиза на GitHub для скачивания вручную.",
                    "Ошибка обновления",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                _btnUpdate.Enabled = true;
                _btnClose.Enabled = true;
                _progressBar.Visible = false;
                _lblProgress.Visible = false;
                return;
            }

            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch { }

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            _webClient = new WebClient();
            _webClient.Headers["User-Agent"] = "AntigravityTray-Updater/" + TrayApplicationContext.APP_VERSION;

            _webClient.DownloadProgressChanged += (s, args) =>
            {
                _progressBar.Value = args.ProgressPercentage;
                double mbReceived = args.BytesReceived / (1024.0 * 1024.0);
                double mbTotal = args.TotalBytesToReceive / (1024.0 * 1024.0);
                if (mbTotal > 0.05)
                {
                    _lblProgress.Text = string.Format("Загрузка: {0}% ({1:F2} МБ из {2:F2} МБ)", args.ProgressPercentage, mbReceived, mbTotal);
                }
                else
                {
                    _lblProgress.Text = string.Format("Загрузка: {0:F2} МБ...", mbReceived);
                }
            };

            _webClient.DownloadFileCompleted += (s, args) =>
            {
                if (args.Error != null)
                {
                    MessageBox.Show(
                        "Ошибка скачивания обновления:\n" + args.Error.Message,
                        "Ошибка загрузки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    _btnUpdate.Enabled = true;
                    _btnClose.Enabled = true;
                    _progressBar.Visible = false;
                    _lblProgress.Text = "Загрузка прервана.";
                    return;
                }

                _lblProgress.Text = "Загрузка завершена! Применение обновления...";
                _progressBar.Value = 100;

                try
                {
                    if (isSetupMode)
                    {
                        // Launch Installer in silent update mode with UAC elevation
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = tempFile,
                            Arguments = "/update",
                            Verb = "runas",
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                        Application.Exit();
                    }
                    else
                    {
                        // Portable Mode: spawn self-updating script and exit
                        string currentExe = Application.ExecutablePath;
                        string batchPath = Path.Combine(Path.GetTempPath(), "update_tray_companion.bat");
                        string script = string.Format(
                            "@echo off\r\n" +
                            "timeout /t 1 /nobreak >nul\r\n" +
                            ":retry\r\n" +
                            "copy /y \"{0}\" \"{1}\" >nul\r\n" +
                            "if errorlevel 1 (\r\n" +
                            "    timeout /t 1 /nobreak >nul\r\n" +
                            "    goto retry\r\n" +
                            ")\r\n" +
                            "del \"{0}\"\r\n" +
                            "start \"\" \"{1}\"\r\n" +
                            "del \"%~f0\"\r\n",
                            tempFile,
                            currentExe
                        );

                        File.WriteAllText(batchPath, script, Encoding.Default);
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = batchPath,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                        Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Ошибка применения обновления:\n" + ex.Message,
                        "Ошибка обновления",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    _btnUpdate.Enabled = true;
                    _btnClose.Enabled = true;
                }
            };

            _lblProgress.Text = "Подключение к GitHub...";
            _webClient.DownloadFileAsync(new Uri(downloadUrl), tempFile);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_webClient != null && _webClient.IsBusy)
            {
                if (MessageBox.Show("Загрузка обновления ещё выполняется. Прервать?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                _webClient.CancelAsync();
            }
            base.OnFormClosing(e);
        }
    }
}
