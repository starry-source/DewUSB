using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace DewUSB
{
    /// <summary>
    /// 主窗口逻辑
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private NotifyIcon tray;
        private readonly Dictionary<string, UIElement> deviceGrids = new Dictionary<string, UIElement>();
        private DispatcherTimer messageTimer;
        private ScrollViewer scrollViewer;
        private System.Windows.Controls.StackPanel devicesPanel;
        private Wpf.Ui.Controls.TextBlock messageText;
        private Settings settings;

        // Win32 API 常量
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_MOVING = 0x0216;
        private const int HTCLIENT = 1;

        public MainWindow()
        {
            InitializeComponent();
            // 获取控件引用
            devicesPanel = (System.Windows.Controls.StackPanel)FindName("DevicesPanel");
            messageText = (Wpf.Ui.Controls.TextBlock)FindName("MessageText");
            scrollViewer = (ScrollViewer)FindName("MainScrollViewer");
            settings = Settings.Load();
            InitializeTrayIcon();
            SetupWindowTheme();
            InitializeMessageTimer();
            //SetupEventHandlers();
            if (settings.StartWithWindows)
            {
                SetStartup(true);
            }
            // 确保窗口完全加载后再设置位置
            Loaded += (s, e) =>
            {
                UpdateWindowPosition();
                RegisterForDeviceNotifications();
                UpdateDevices();
            };

            StateChanged += OnStateChanged;

            //UpdateDevices();
            // 初始启动时，如果没有U盘则隐藏窗口
            //Loaded += (s, e) =>
            //{
            //    UpdateDevices();
            //};
        }


        /// <summary>
        /// 初始化托盘图标及菜单
        /// </summary>
        private void InitializeTrayIcon()
        {
            tray = new NotifyIcon
            {
                Icon = new Icon("/ud.ico"),
                Visible = true,
                Text = "DewUSB"
            };
            
            var contextMenu = new ContextMenuStrip();
            var settingsItem = new ToolStripMenuItem("设置");
            settingsItem.Click += (s, e) => ShowSettingsWindow();
            var aboutItem = new ToolStripMenuItem("关于");
            aboutItem.Click += (s, e) => ShowAboutWindow();
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) =>
            {
                tray.Dispose();
                Application.Current.Shutdown();
            };
            
            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                settingsItem,
                aboutItem,
                new ToolStripSeparator(),
                exitItem
            });
            
            tray.ContextMenuStrip = contextMenu;
            tray.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (deviceGrids.Count > 0)
                    {
                        Show();
                        WindowState = WindowState.Normal;
                        //UpdateDevices();
                        UpdateWindowPosition();
                        Activate();
                        ShowWithFadeIn();
                    }
                }
            };
        }

        /// <summary>
        /// 状态变化时处理最小化到托盘
        /// </summary>
        private void OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                //if (settings.MinimizeToTray)
                //{

                    Hide();
                //}
            }
        }

        /// <summary>
        /// 设置窗口主题
        /// </summary>
        private void SetupWindowTheme()
        {

            if (settings.Theme == 2)
            {
                // 跟随系统主题
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this as System.Windows.Window);
            }
            else if (settings.Theme == 0)
            {
                // 深色主题
                (new ThemeService()).SetTheme(Wpf.Ui.Appearance.ApplicationTheme.Dark);
            }
            else if (settings.Theme == 1)
            {
                // 浅色主题
                (new ThemeService()).SetTheme(Wpf.Ui.Appearance.ApplicationTheme.Light);
            }
        }

            /// <summary>
            /// 初始化消息定时器
            /// </summary>
        private void InitializeMessageTimer()
        {
            messageTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            messageTimer.Tick += (s, e) =>
            {
                UpdateDevices();
                messageTimer.Stop();
                messageText.Visibility = Visibility.Collapsed;
                MainScrollViewer.Effect = null;
                MainScrollViewer.IsEnabled = true;
            };
        }

        /// <summary>
        /// 显示消息文本
        /// </summary>
        private void ShowMessage(string message)
        {
            messageText.Text = message;
            messageText.Visibility = Visibility.Visible;
            MainScrollViewer.Effect = new System.Windows.Media.Effects.BlurEffect
            {
                Radius = 25,
                KernelType = System.Windows.Media.Effects.KernelType.Gaussian
            };
            MainScrollViewer.IsEnabled = false;
            messageTimer.Start();
            System.Windows.Controls.Panel.SetZIndex(messageText, 1000);
        }

        /// <summary>
        /// 创建设备卡片（带动画）
        /// </summary>
        private UIElement CreateDeviceGrid(DriveInfo drive)
        {
            var containerGrid = new Grid();
            var border = new Border
            {
                Style = FindResource("DeviceCardStyle") as Style
            };
            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            border.MouseLeftButtonDown += (s, e) => System.Diagnostics.Process.Start("explorer.exe", drive.Name);
            string uri;
            if (settings.IconType == 0)
            {
                uri = "pack://application:,,,/ud.png";
            }
            else
            {
                if(Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark)
                    uri = "pack://application:,,,/uddark.png";
                else
                    uri = "pack://application:,,,/udlight.png";
            }
            var image = new Wpf.Ui.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(uri)),
                Width = 60,
                Height = 70,
                Margin = new Thickness(10,10,5,10),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(image, 0);
            var infoPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 20, 20, 20) };
            Grid.SetColumn(infoPanel, 1);
            var headerPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 2)
            };
            var driveName = new Wpf.Ui.Controls.TextBlock
            {
                Text = string.IsNullOrEmpty(drive.VolumeLabel) ? "U 盘" : drive.VolumeLabel,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
            var driveLabel = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 128, 128, 128)),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(6, 4, 6, 0)
            };
            driveLabel.Child = new Wpf.Ui.Controls.TextBlock
            {
                Text = drive.Name.TrimEnd('\\'),
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(225, 140, 140, 140))
            };
            headerPanel.Children.Add(driveName);
            headerPanel.Children.Add(driveLabel);
            var progressContainer = new Grid();
            progressContainer.ColumnDefinitions.Add(new ColumnDefinition());
            progressContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var progressBar = new System.Windows.Controls.ProgressBar
            {
                Height = 6,
                Minimum = 0,
                Maximum = 100,
                Value = ((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize) * 100,
                Margin = new Thickness(0, 0, 5, 0),
            };
            Grid.SetColumn(progressBar, 0);
            var spaceInfo = new Wpf.Ui.Controls.TextBlock
            {
                Text = $"{FormatBytes(drive.AvailableFreeSpace)}/{FormatBytes(drive.TotalSize)}",
                FontSize = 12,
                Margin = new Thickness(8, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(spaceInfo, 1);
            progressContainer.Children.Add(progressBar);
            progressContainer.Children.Add(spaceInfo);
            var buttonsPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };
            var openButton = new Wpf.Ui.Controls.Button
            {
                Content = "打开 U 盘",
                Margin = new Thickness(0, 0, 10, 0),
                Appearance=Wpf.Ui.Controls.ControlAppearance.Primary
            };
            openButton.Click += (s, e) => System.Diagnostics.Process.Start("explorer.exe", drive.Name);
            var ejectButton = new Wpf.Ui.Controls.Button
            {
                Content = "安全弹出",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary
            };
            // 弹出U盘逻辑，带反馈
            ejectButton.Click += (s, e) =>
            {
                bool result = EjectDrive(drive.Name);
                if (result)
                {
                    ShowMessage("已安全弹出");
                }
                else
                {
                    ShowMessage("弹出失败");
                }
            };
            buttonsPanel.Children.Add(openButton);
            buttonsPanel.Children.Add(ejectButton);
            infoPanel.Children.Add(headerPanel);
            infoPanel.Children.Add(progressContainer);
            infoPanel.Children.Add(buttonsPanel);
            contentGrid.Children.Add(image);
            contentGrid.Children.Add(infoPanel);
            border.Child = contentGrid;
            containerGrid.Children.Add(border);
            // 淡入动画
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            border.BeginAnimation(Border.OpacityProperty, fade);
            return containerGrid;
        }

        /// <summary>
        /// 显示窗口并淡入
        /// </summary>
        private void ShowWithFadeIn()
        {
            if (!IsVisible)
                Show();
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            BeginAnimation(OpacityProperty, fade);
        }

        /// <summary>
        /// 设置开机自启
        /// </summary>
        private void SetStartup(bool enable)
        {
            const string AppName = "DewUSB";
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (enable)
            {
                key.SetValue(AppName, System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }

        /// <summary>
        /// 字节格式化
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }

        /// <summary>
        /// 弹出U盘，返回是否成功
        /// </summary>
        private bool EjectDrive(string driveLetter)
        {
            try
            {
                // 等待一小段时间确保所有文件操作完成
                System.Threading.Thread.Sleep(500);

                // 使用设备管理API弹出设备
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = $"-Command \"$driveEject = New-Object -comObject Shell.Application; $driveEject.Namespace(17).ParseName('{driveLetter}').InvokeVerb('Eject'); Start-Sleep -Seconds 1; $removable = Get-WmiObject Win32_LogicalDisk | Where-Object {{$_.DeviceID -eq '{driveLetter.TrimEnd('\\')}' -and $_.DriveType -eq 2}}; return ($removable -eq $null)\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    // 等待弹出操作完成
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(3000);

                    // 验证设备是否真的已经弹出
                    bool ejected = !Directory.Exists(driveLetter) || output.Contains("True");
                    
                    if (!ejected)
                    {
                        ShowMessage("设备正在使用中，请关闭所有程序后重试");
                        return false;
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"弹出设备时发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 更新设备列表和分割线
        /// </summary>
        private void UpdateDevices()
        {
            var currentDrives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                .ToDictionary(d => d.Name);

            // 移除断开设备
            var disconnectedDevices = deviceGrids.Keys
                .Where(k => !currentDrives.ContainsKey(k))
                .ToList();

            foreach (var device in disconnectedDevices)
            {
                devicesPanel.Children.Remove(deviceGrids[device]);
                deviceGrids.Remove(device);
            }

            // 清理所有分割线
            var separators = devicesPanel.Children.OfType<Separator>().ToList();
            foreach (var separator in separators)
            {
                devicesPanel.Children.Remove(separator);
            }

            // 重新添加设备和分割线
            devicesPanel.Children.Clear();
            deviceGrids.Clear();
            int idx = 0;

            foreach (var drive in currentDrives.Values)
            {
                if (idx > 0)
                {
                    devicesPanel.Children.Add(new Separator { Margin = new Thickness(20, 5, 20, 5) });
                }
                var deviceGrid = CreateDeviceGrid(drive);
                devicesPanel.Children.Add(deviceGrid);
                deviceGrids[drive.Name] = deviceGrid;
                idx++;
            }

            // 更新窗口位置和显示状态
            UpdateWindowPosition();

            // 处理设备变化
            if (disconnectedDevices.Any())
            {
                ShowMessage("设备已拔出");
            }
            else if (currentDrives.Count > 0 && !IsVisible)
            {
                // 有设备且窗口未显示时，显示窗口
                ShowWithFadeIn();
                WindowState = WindowState.Normal;
                Activate();
            }
        }

        /// <summary>
        /// 根据内容和屏幕动态调整窗口位置和高度
        /// </summary>
        private void UpdateWindowPosition()
        {
            // 获取当前窗口所在的屏幕
            var windowHandle = new WindowInteropHelper(this).Handle;
            var screen = Screen.FromHandle(windowHandle);
            var workingArea = screen.WorkingArea;

            // 固定宽度
            Width = settings.WindowWidth;

            // 处理 DPI 缩放问题
            double dpiFactor = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                // 该矩阵可将设备像素转换为DIP
                dpiFactor = source.CompositionTarget.TransformFromDevice.M11;
            }

            // 计算内容高度（标题栏 + 设备卡片 * 数量 + 边距）
            double contentHeight = 38 + (deviceGrids.Count * 150) + 10;
            double maxHeight = Math.Min(workingArea.Height * dpiFactor * 0.5, contentHeight);  // 最大高度为屏幕高度的一半或内容高度
            Height = Math.Max(100, maxHeight);  // 最小高度100


            if(settings.Position == 0) // 左上
            {
                Left = 20;
                Top = 20;
            }
            else if(settings.Position == 1) // 左下
            {
                Left = 20;
                Top = (workingArea.Bottom * dpiFactor) - Height - 20;
            }
            else if(settings.Position==3) // 居中
            {
                Left = (workingArea.Width * dpiFactor - Width) / 2 + workingArea.Left;
                Top = (workingArea.Height * dpiFactor - Height) / 2 + workingArea.Top;
            }
            else
            {
                // 右下角位置
                Left = (workingArea.Right * dpiFactor) - Width - 20;
                Top = (workingArea.Bottom * dpiFactor) - Height - 20;
            }

            if (settings.TopMost)
            {
                Topmost = true;
            }
            else
            {
                Topmost = false;
            }

            // 没有U盘时，隐藏窗口
            if (deviceGrids.Count == 0)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }
        }

        /// <summary>
        /// 显示设置窗口
        /// </summary>
        private void ShowSettingsWindow()
        {
            var settingsWindow = new SettingsWindow(settings);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                settings = Settings.Load(); // 重新加载设置
            }
        }

        /// <summary>
        /// 显示关于窗口
        /// </summary>
        private void ShowAboutWindow()
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        // 在 MainWindow 类中添加以下方法

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                int eventType = wParam.ToInt32();
                if (eventType == DBT_DEVICEARRIVAL || eventType == DBT_DEVICEREMOVECOMPLETE)
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateDevices();
                    });
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        // 修改 RegisterForDeviceNotifications 方法，添加钩子监听
        private void RegisterForDeviceNotifications()
        {
            //UpdateDevices();
            var windowHandle = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(windowHandle);
            if (source != null)
            {
                source.AddHook(WndProc);
            }
        }

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        /// <summary>
        /// 关闭时释放托盘资源
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 阻止程序关闭
            base.OnClosing(e);
        }

        private void Minimize_Button_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Refresh_Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateDevices();
        }
    }
}
