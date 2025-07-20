using System.Windows;
using Wpf.Ui.Controls;

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
            settings = currentSettings;
            
            // 根据当前设置更新UI控件状态
            StartWithWindows.IsChecked = settings.StartWithWindows;
            MinimizeToTray.IsChecked = settings.MinimizeToTray;

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
            settings.StartWithWindows = StartWithWindows.IsChecked ?? false;
            settings.MinimizeToTray = MinimizeToTray.IsChecked ?? true;
            settings.Save();
            
            // 关闭窗口并返回成功结果
            DialogResult = true;
            Close();
        }
    }
}