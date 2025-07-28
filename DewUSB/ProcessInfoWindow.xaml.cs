using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace DewUSB
{
    public partial class ProcessInfoWindow : FluentWindow
    {
        private readonly string drivePath;

        public ProcessInfoWindow(string driveLetter)
        {
            InitializeComponent();
            drivePath = driveLetter;
            Title = $"进程管理 - {driveLetter}";

            Loaded += (s, e) => RefreshProcessList();
        }

        private void RefreshProcessList()
        {
            ProcessPanel.Children.Clear();
            var processes = GetProcessesUsingDrive();

            if (processes.Count == 0)
            {
                NoProcessText.Visibility = Visibility.Visible;
                return;
            }

            NoProcessText.Visibility = Visibility.Collapsed;
            foreach (var process in processes)
            {
                ProcessPanel.Children.Add(CreateProcessCard(process));
            }
        }

        private UIElement CreateProcessCard(Process process)
        {
            var card = new CardExpander
            {
                Header = process.ProcessName,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(16)
            };

            var stackPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical
            };

            var pidText = new TextBlock
            {
                Text = $"进程ID: {process.Id}",
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanel.Children.Add(pidText);

            try
            {
                var mainWindowText = new TextBlock
                {
                    Text = $"窗口标题: {(string.IsNullOrEmpty(process.MainWindowTitle) ? "(无窗口)" : process.MainWindowTitle)}",
                    Margin = new Thickness(0, 0, 0, 8)
                };
                stackPanel.Children.Add(mainWindowText);
            }
            catch { }

            var buttonsPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var closeButton = new Button
            {
                Content = "关闭进程",
                Appearance = ControlAppearance.Secondary,
                Margin = new Thickness(0, 0, 8, 0)
            };
            closeButton.Click += (s, e) => CloseProcess(process);

            var killButton = new Button
            {
                Content = "强制结束",
                Appearance = ControlAppearance.Danger
            };
            var warningIcon = new SymbolIcon { Symbol = SymbolRegular.Warning24 };
            killButton.Icon = warningIcon;
            killButton.Click += (s, e) => KillProcess(process);

            buttonsPanel.Children.Add(closeButton);
            buttonsPanel.Children.Add(killButton);
            
            stackPanel.Children.Add(buttonsPanel);
            card.Content = stackPanel;

            return card;
        }
        
        private List<Process> GetProcessesUsingDrive()
        {
            var processList = new List<Process>();
            var pids = new HashSet<int>();
            string resourceName = "DewUSB.handle.exe";
            string tempPath = Path.Combine(Path.GetTempPath(), "handle.exe");

            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (FileStream file = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                resource.CopyTo(file);
            }
            var fileInfo = new FileInfo(tempPath);
            if (fileInfo.Length == 0)
                throw new Exception("释放的EXE文件大小为0，请检查资源名和嵌入方式。");

            // 构造命令参数
            string arguments = $"F: /accepteula";
            var startInfo = new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 示例行: notepad.exe       pid: 1234    ...
                var matches = Regex.Matches(output, @"(?<name>[^\s]+)\s+pid: (?<pid>\d+)", RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    int pid = int.Parse(match.Groups["pid"].Value);
                    if (!pids.Contains(pid))
                    {
                        try
                        {
                            var proc = Process.GetProcessById(pid);
                            processList.Add(proc);
                            pids.Add(pid);
                        }
                        catch
                        {
                            // 进程可能已退出，忽略
                        }
                    }
                }
            }

            return processList;
        }

        //private List<Process> GetProcessesUsingDrive()
        //{
        //    var processes = new HashSet<Process>();

        //    try
        //    {
        //        // 使用WMI查询打开的文件
        //        var scope = new ManagementScope("\\\\.\\root\\cimv2");
        //        var query = new SelectQuery("SELECT * FROM Win32_Process");
                
        //        using (var searcher = new ManagementObjectSearcher(scope, query))
        //        {
        //            foreach (ManagementObject process in searcher.Get())
        //            {
        //                try
        //                {
        //                    var pid = Convert.ToInt32(process["ProcessId"]);
        //                    var proc = Process.GetProcessById(pid);

        //                    // 检查进程的所有模块是否使用了该驱动器
        //                    foreach (ProcessModule module in proc.Modules)
        //                    {
        //                        if (module.FileName?.StartsWith(drivePath, StringComparison.OrdinalIgnoreCase) == true)
        //                        {
        //                            processes.Add(proc);
        //                            break;
        //                        }
        //                    }

        //                    // 检查工作目录
        //                    var workingSet = process["WorkingSetSize"];
        //                    if (workingSet != null && proc.MainModule?.FileName?.StartsWith(drivePath, StringComparison.OrdinalIgnoreCase) == true)
        //                    {
        //                        processes.Add(proc);
        //                    }
        //                }
        //                catch { }
        //            }
        //        }

        //        // 检查当前目录在U盘上的进程
        //        var currentDriveProcesses = Process.GetProcesses().Where(p =>
        //        {
        //            try
        //            {
        //                return p.MainModule?.FileName?.StartsWith(drivePath, StringComparison.OrdinalIgnoreCase) == true ||
        //                       (p.MainWindowTitle?.Length > 0 && 
        //                        p.Modules.Cast<ProcessModule>().Any(m => 
        //                            m.FileName?.StartsWith(drivePath, StringComparison.OrdinalIgnoreCase) == true));
        //            }
        //            catch { return false; }
        //        });

        //        foreach (var proc in currentDriveProcesses)
        //        {
        //            processes.Add(proc);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var dialog = new Wpf.Ui.Controls.MessageBox
        //        {
        //            Title = "错误",
        //            Content = $"无法获取进程信息: {ex.Message}",
        //            PrimaryButtonText = "确定"
        //        };
        //        dialog.ShowDialogAsync();
        //    }

        //    return processes.ToList();
        //}

        private async void CloseProcess(Process process)
        {
            try 
            {
                if (!process.HasExited && process.CloseMainWindow())
                {
                    await process.WaitForExitAsync();
                }
                else if (!process.HasExited)
                {
                    process.Kill(true);
                }
                RefreshProcessList();
            }
            catch (Exception ex)
            {
                var dialog = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = $"无法关闭进程: {ex.Message}",
                    PrimaryButtonText = "确定"
                };
                await dialog.ShowDialogAsync();
            }
        }

        private async void KillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
                RefreshProcessList();
            }
            catch (Exception ex)
            {
                var dialog = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = $"无法强制结束进程: {ex.Message}",
                    PrimaryButtonText = "确定"
                };
                await dialog.ShowDialogAsync();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}