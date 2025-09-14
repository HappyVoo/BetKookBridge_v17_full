using System;
using System.IO;
using System.Text;

namespace BetKookBridge
{
    internal static class Logger
    {
        private static readonly object _lock = new();
        public static string Dir => System.IO.Path.Combine(AppSettings.Dir, "logs");
        public static string FilePath => System.IO.Path.Combine(Dir, "app.log");

        public static void Log(string msg)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}{Environment.NewLine}";
                lock(_lock) { File.AppendAllText(FilePath, line, new UTF8Encoding(false)); }
            } catch {}
        }
    }
}
