using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BetKookBridge
{
    internal sealed class AppSettings
    {
        public string? GameLogPath { get; set; }
        public string? KookToken { get; set; }
        public string? KookChannelId { get; set; }
        public bool UploadEnabled { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;

        public static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BetKookBridge");
        public static string PathFile => System.IO.Path.Combine(Dir, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                if (File.Exists(PathFile))
                {
                    var json = File.ReadAllText(PathFile, Encoding.UTF8);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    return s ?? new AppSettings();
                }
            }
            catch {}
            return new AppSettings();
        }

        public void Save()
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions{WriteIndented = true});
            File.WriteAllText(PathFile, json, Encoding.UTF8);
        }
    }
}
