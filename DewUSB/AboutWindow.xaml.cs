using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Controls;

namespace DewUSB
{
    /// <summary>
    /// 关于窗口的交互逻辑，显示应用程序信息和项目链接
    /// </summary>
    public partial class AboutWindow : FluentWindow
    {
        /// <summary>
        /// 初始化关于窗口
        /// </summary>
        public AboutWindow()
        {
            InitializeComponent();

            // 注册访问项目按钮的点击事件
            // 点击时在默认浏览器中打开项目地址
            VisitProjectButton.Click += (s, e) =>
            {
                Process.Start("https://github.com/starry-source/DewUSB");
            };
        }
    }
}