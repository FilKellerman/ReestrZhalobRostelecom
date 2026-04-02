using System;
using System.IO;
using System.Linq;

namespace ReestrObrashcheniy
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
        private static readonly object _lock = new object();
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 МБ
        private const int MaxArchiveFiles = 5;

        private static void CheckAndRotateLog()
        {
            try
            {
                if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxFileSize)
                {
                    string archiveName = $"app_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                    string archivePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, archiveName);
                    File.Move(LogFilePath, archivePath);

                    // Удаляем старые архивы, если их больше 5
                    var archives = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "app_*.log")
                                            .OrderByDescending(f => f)
                                            .Skip(MaxArchiveFiles)
                                            .ToList();

                    foreach (var old in archives)
                    {
                        try { File.Delete(old); } catch { }
                    }
                }
            }
            catch { /* Игнорируем ошибки ротации, чтобы не останавливать программу */ }
        }

        private static void WriteLog(string level, string category, string message, Exception ex = null)
        {
            lock (_lock)
            {
                CheckAndRotateLog();

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logLine = $"[{timestamp}] [{level}] [{category}] {message}";

                if (ex != null)
                    logLine += $" | Exception: {ex.Message} | StackTrace: {ex.StackTrace}";

                try
                {
                    File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
                }
                catch { }
            }
        }

        public static void Info(string category, string message)
            => WriteLog("INFO", category, message);

        public static void Warning(string category, string message)
            => WriteLog("WARNING", category, message);

        public static void Error(string category, string message, Exception ex = null)
            => WriteLog("ERROR", category, message, ex);

        public static void Audit(string user, string action, string details)
            => WriteLog("AUDIT", $"User:{user}", $"{action} | {details}");
    }
}