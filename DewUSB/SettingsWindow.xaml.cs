using DewUSB.Properties;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;
using Wpf.Ui;
using Wpf.Ui.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DewUSB
{
    /// <summary>
    /// 设置窗口的交互逻辑，处理应用程序设置的显示和保存
    /// </summary>
    public partial class SettingsWindow : FluentWindow
    {
        /// <summary>
        /// 当前应用程序设置的引用
        /// </summary>
        private readonly Settings settings;

        /// <summary>
        /// 初始化设置窗口
        /// </summary>
        /// <param name="currentSettings">当前应用程序的设置实例</param>
        public SettingsWindow(Settings currentSettings)
        {
            InitializeComponent();
            Topmost = false;
            settings = currentSettings;

            // 根据当前设置更新UI控件状态
            StartWithWindows.IsChecked = settings.StartWithWindows;
            MinimizeToTray.IsChecked = settings.MinimizeToTray;

            ThemeSelector.ItemsSource = new List<string>{
                "深色",
                "浅色",
                "跟随系统"
            };

            ThemeSelector.SelectedIndex = settings.Theme;
            TopMost.IsChecked = settings.TopMost;

            Position.ItemsSource = new List<string>{
                "左上角",
                "左下角",
                "右下角",
                "居中"
            };
            Position.SelectedIndex = settings.Position;

            IconType0.IsChecked = settings.IconType == 0; // 彩色图标
            IconType1.IsChecked = settings.IconType == 1; // 灰色图标

            WindowWidth.Value = settings.WindowWidth;

            // 注册保存按钮的点击事件
            SaveButton.Click += SaveButton_Click;
        }

        /// <summary>
        /// 处理设置保存操作
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 从UI更新设置值

            bool restart = ThemeSelector.SelectedIndex != settings.Theme;

            settings.StartWithWindows = (bool)StartWithWindows.IsChecked;
            settings.MinimizeToTray = (bool)MinimizeToTray.IsChecked;
            settings.Theme = ThemeSelector.SelectedIndex;
            settings.TopMost = (bool)TopMost.IsChecked;
            settings.Position = Position.SelectedIndex;
            settings.IconType = IconType0.IsChecked == true ? 0 : 1;
            settings.WindowWidth = (int)WindowWidth.Value;

            settings.Save();

            if (restart)
            {
                // 如果主题改变，提示用户重启应用
                fly.Visibility = Visibility.Visible;
                mainContent.Effect = new System.Windows.Media.Effects.BlurEffect
                {
                    Radius = 25
                };
                mainContent.IsEnabled = false;
                SaveButton.IsEnabled = false;
            }
            else
            {
                // 直接关闭设置窗口
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 取消设置，直接关闭窗口
            Close();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            // 重启应用程序
            System.Diagnostics.Process.Start(AppDomain.CurrentDomain.FriendlyName);
            System.Windows.Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 引导用户关闭自动播放通知
            Hide();
            System.Diagnostics.Process.Start("ms-settings:notifications");
            // wpfui message box
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = this,
                Title = "提示",
                Content = new TextBlock
                {
                    Text = "请找到 “自动播放” 的通知开关，然后关闭它。\n这可以避免被弹出的通知遮挡。",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                },
                ShowInTaskbar = false,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
            };
            messageBox.ShowDialogAsync();
            Show();
        }

        private void IconType0_Checked(object sender, RoutedEventArgs e)
        {
            IconImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/ud.png"));
        }

        private void IconType1_Checked(object sender, RoutedEventArgs e)
        {
            // 根据当前应用的颜色主题
            if (Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark)
                IconImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/uddark.png"));
            else
                IconImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/udlight.png"));
        }

        private void Restore_WindowWidth_Click(object sender, RoutedEventArgs e)
        {
            // 恢复窗口宽度
            WindowWidth.Value = 400;
        }
    }
}