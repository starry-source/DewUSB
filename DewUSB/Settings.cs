using System;
using System.IO;
using System.Text.Json;

namespace DewUSB
{
    public class Settings
    {
        public bool StartWithWindows { get; set; }= false; // 默认不开机自启
        public bool MinimizeToTray { get; set; }= true; // 默认最小化到托盘
        public int Theme { get; set; } = 2;

        public bool TopMost { get; set; } = false; // 默认不设置为置顶

        public int Position { get; set; } = 2; // 默认右下
        public int IconType { get; set; } = 0; // 默认彩色

        public int WindowWidth { get; set; } = 400; // 默认窗口宽度

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DewUSB",
            "settings.json"
        );

        public static Settings Load()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
            }
            catch { }

            return new Settings();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}