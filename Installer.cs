using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AntigravityTraySetup
{
    static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            try
            {
                SetProcessDpiAwarenessContext((IntPtr)(-4));
            }
            catch
            {
                try { SetProcessDPIAware(); } catch { }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }
    }

    public class SetupForm : Form
    {
        private TextBox txtInstallDir;
        private TextBox txtUnlockerPath;
        private CheckBox chkCloseToTray;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkAutostart;
        private CheckBox chkLaunchNow;
        private Button btnInstall;
        private Button btnCancel;

        public SetupForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Font = SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9F);
            this.Text = "Мастер установки - Antigravity Tray Companion v1.1.12";
            this.Size = new Size(570, 525);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Load Icon for Form
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White
            };
            this.Controls.Add(pnlHeader);

            Image headerBmp = null;
            try
            {
                string png1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                string png2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"assets\icon.png");
                if (File.Exists(png1)) headerBmp = Image.FromFile(png1);
                else if (File.Exists(png2)) headerBmp = Image.FromFile(png2);
            }
            catch { }

            Panel pnlHeaderIcon = new Panel
            {
                Location = new Point(16, 16),
                Size = new Size(48, 48),
                BackColor = Color.Transparent
            };
            pnlHeaderIcon.Paint += (s, pe) =>
            {
                pe.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                pe.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                if (headerBmp != null)
                {
                    pe.Graphics.DrawImage(headerBmp, new Rectangle(0, 0, 48, 48));
                }
                else if (this.Icon != null)
                {
                    pe.Graphics.DrawIcon(this.Icon, new Rectangle(0, 0, 48, 48));
                }
            };
            pnlHeader.Controls.Add(pnlHeaderIcon);

            Label lblTitle = new Label
            {
                Text = "Установка Antigravity Tray Companion",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                Location = new Point(75, 16),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Сворачивание Antigravity Unlocker в системный трей",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Location = new Point(76, 42),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSubtitle);

            Label lblLine = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.LightGray
            };
            pnlHeader.Controls.Add(lblLine);

            int curY = 95;

            // 1. Classic Program Files target directory
            Label lblInstallDir = new Label
            {
                Text = "1. Папка установки программы (Program Files):",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(20, curY),
                AutoSize = true
            };
            this.Controls.Add(lblInstallDir);
            curY += 24;

            string classicProgramFiles = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "AntigravityTray"
            );

            txtInstallDir = new TextBox
            {
                Text = classicProgramFiles,
                Location = new Point(20, curY),
                Width = 410,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(txtInstallDir);

            Button btnBrowseInstall = new Button
            {
                Text = "Обзор...",
                Location = new Point(440, curY - 1),
                Width = 95,
                Height = 26,
                Font = new Font("Segoe UI", 9F)
            };
            btnBrowseInstall.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = txtInstallDir.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtInstallDir.Text = fbd.SelectedPath;
                    }
                }
            };
            this.Controls.Add(btnBrowseInstall);
            curY += 42;

            // 2. Client-specified Unlocker exe path (empty by default)
            Label lblUnlocker = new Label
            {
                Text = "2. Укажите файл Antigravity Unlocker (exe от разработчика):",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(20, curY),
                AutoSize = true
            };
            this.Controls.Add(lblUnlocker);
            curY += 24;

            txtUnlockerPath = new TextBox
            {
                Text = "", // Empty by default
                Location = new Point(20, curY),
                Width = 410,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(txtUnlockerPath);

            Button btnBrowseUnlocker = new Button
            {
                Text = "Обзор...",
                Location = new Point(440, curY - 1),
                Width = 95,
                Height = 26,
                Font = new Font("Segoe UI", 9F)
            };
            btnBrowseUnlocker.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Выберите скачанный файл Antigravity Unlocker (например, AG_2.29.0.exe)";
                    ofd.Filter = "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*";
                    if (!string.IsNullOrEmpty(txtUnlockerPath.Text) && File.Exists(txtUnlockerPath.Text))
                    {
                        ofd.InitialDirectory = Path.GetDirectoryName(txtUnlockerPath.Text);
                        ofd.FileName = Path.GetFileName(txtUnlockerPath.Text);
                    }
                    else
                    {
                        // Open user's Documents folder with instant access to This PC and all drives
                        ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtUnlockerPath.Text = ofd.FileName;
                    }
                }
            };
            this.Controls.Add(btnBrowseUnlocker);
            curY += 40;

            // Options
            chkCloseToTray = new CheckBox
            {
                Text = "Сворачивать в трей при нажатии «Х» (крестик закрытия)",
                Location = new Point(25, curY),
                Checked = true,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(chkCloseToTray);
            curY += 26;

            chkDesktop = new CheckBox
            {
                Text = "Создать ярлык на Рабочем столе",
                Location = new Point(25, curY),
                Checked = true,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(chkDesktop);
            curY += 26;

            chkStartMenu = new CheckBox
            {
                Text = "Создать ярлык в меню «Пуск»",
                Location = new Point(25, curY),
                Checked = true,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(chkStartMenu);
            curY += 26;

            chkAutostart = new CheckBox
            {
                Text = "Запускать автоматически при входе в Windows",
                Location = new Point(25, curY),
                Checked = true,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(chkAutostart);
            curY += 26;

            chkLaunchNow = new CheckBox
            {
                Text = "Запустить Antigravity Tray сразу после установки",
                Location = new Point(25, curY),
                Checked = true,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(chkLaunchNow);
            curY += 46;

            // Bottom line & buttons
            Label lblBottomLine = new Label
            {
                Location = new Point(0, curY),
                Size = new Size(570, 1),
                BackColor = Color.LightGray
            };
            this.Controls.Add(lblBottomLine);
            curY += 12;

            btnInstall = new Button
            {
                Text = "Установить",
                Location = new Point(320, curY),
                Width = 110,
                Height = 34,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                DialogResult = DialogResult.None
            };
            btnInstall.Click += OnInstallClick;
            this.Controls.Add(btnInstall);

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(440, curY),
                Width = 95,
                Height = 34,
                Font = new Font("Segoe UI", 9F)
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnInstall;
            this.CancelButton = btnCancel;
        }

        private void OnInstallClick(object sender, EventArgs e)
        {
            string installDir = txtInstallDir.Text.Trim();
            string unlockerPath = txtUnlockerPath.Text.Trim();

            if (string.IsNullOrEmpty(installDir))
            {
                MessageBox.Show("Пожалуйста, укажите папку для установки программы.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(unlockerPath) || !File.Exists(unlockerPath))
            {
                DialogResult dr = MessageBox.Show(
                    "Вы не указали файл Antigravity Unlocker (exe-файл разработчика).\n\n" +
                    "Нажмите «Повторить», чтобы выбрать файл через кнопку «Обзор...», или «Пропустить», если укажете его позже через меню настроек в трее.",
                    "Файл не указан",
                    MessageBoxButtons.AbortRetryIgnore,
                    MessageBoxIcon.Warning
                );

                if (dr == DialogResult.Abort || dr == DialogResult.Retry)
                {
                    return;
                }
            }

            try
            {
                // Terminate any running instances so files are not locked
                Process[] runningTrays = Process.GetProcessesByName("AntigravityTray");
                foreach (Process pt in runningTrays)
                {
                    try
                    {
                        pt.Kill();
                        pt.WaitForExit(2000);
                    }
                    catch { }
                }

                Directory.CreateDirectory(installDir);

                string sourceDir = AppDomain.CurrentDomain.BaseDirectory;
                string srcExe = Path.Combine(sourceDir, "AntigravityTray.exe");
                string srcIcon = Path.Combine(sourceDir, "icon.ico");
                string srcReadme = Path.Combine(sourceDir, "README.md");

                string targetExe = Path.Combine(installDir, "AntigravityTray.exe");
                string targetIcon = Path.Combine(installDir, "icon.ico");
                string targetReadme = Path.Combine(installDir, "README.md");

                if (File.Exists(srcExe)) File.Copy(srcExe, targetExe, true);
                if (File.Exists(srcIcon)) File.Copy(srcIcon, targetIcon, true);
                if (File.Exists(srcReadme)) File.Copy(srcReadme, targetReadme, true);

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\AntigravityTray"))
                {
                    if (key != null)
                    {
                        if (!string.IsNullOrEmpty(unlockerPath) && File.Exists(unlockerPath))
                        {
                            key.SetValue("UnlockerPath", unlockerPath);
                        }
                        key.SetValue("CloseToTray", chkCloseToTray.Checked ? 1 : 0, RegistryValueKind.DWord);
                    }
                }

                // Autostart configuration (Task Scheduler for elevated run + registry fallback)
                if (chkAutostart.Checked)
                {
                    try
                    {
                        ProcessStartInfo psiTask = new ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = string.Format("/create /tn \"AntigravityTray\" /tr \"\\\"{0}\\\"\" /sc onlogon /rl highest /f", targetExe),
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (Process p = Process.Start(psiTask))
                        {
                            p.WaitForExit(3000);
                        }
                    }
                    catch { }

                    using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (runKey != null) runKey.SetValue("AntigravityTray", "\"" + targetExe + "\"");
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

                    using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (runKey != null) runKey.DeleteValue("AntigravityTray", false);
                    }
                }

                // Desktop shortcut
                if (chkDesktop.Checked)
                {
                    string desktopLnk = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                        "Antigravity Tray.lnk"
                    );
                    CreateShortcut(desktopLnk, targetExe, targetExe + ",0", "Antigravity Tray Companion");
                }

                // Start Menu shortcut
                if (chkStartMenu.Checked)
                {
                    string startMenuDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                        "Antigravity Tray Companion"
                    );
                    if (!Directory.Exists(startMenuDir))
                    {
                        startMenuDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Programs)
                        );
                    }
                    string startLnk = Path.Combine(startMenuDir, "Antigravity Tray Companion.lnk");
                    CreateShortcut(startLnk, targetExe, targetExe + ",0", "Antigravity Tray Companion");
                }

                // Add to Windows Programs and Features (Uninstall)
                using (RegistryKey uninst = Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\AntigravityTray"))
                {
                    RegistryKey targetKey = uninst ?? Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\AntigravityTray");
                    if (targetKey != null)
                    {
                        targetKey.SetValue("DisplayName", "Antigravity Tray Companion");
                        targetKey.SetValue("DisplayVersion", "1.1.12");
                        targetKey.SetValue("Publisher", "Community");
                        targetKey.SetValue("DisplayIcon", targetExe + ",0");
                        targetKey.SetValue("InstallLocation", installDir);
                        targetKey.SetValue("UninstallString", "\"" + targetExe + "\" --uninstall");
                        targetKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        targetKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                        targetKey.Close();
                    }
                }

                if (chkLaunchNow.Checked)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        WorkingDirectory = installDir,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                }

                MessageBox.Show(
                    "Установка Antigravity Tray Companion успешно завершена в Program Files!\n\n" +
                    "Программа готова к работе.\nПри нажатии крестика «Х» окно будет автоматически прятаться в трей.",
                    "Установка завершена",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при установке:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string iconLocation, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(iconLocation))
                {
                    shortcut.IconLocation = iconLocation;
                }
                shortcut.Description = description;
                shortcut.Save();
            }
            catch { }
        }
    }
}
