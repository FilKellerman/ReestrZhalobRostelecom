using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;

namespace ReestrObrashcheniy
{
    public static class BackupManager
    {
        // Самое доступное место для SQL Server — корень диска C:
        public static string BackupDir { get; } = @"C:\ReestrObrashcheniy_Backups";

        private static readonly string LogFile = Path.Combine(BackupDir, "backup_log.txt");

        /// <summary>
        /// Создание резервной копии
        /// </summary>
        public static string CreateBackup()
        {
            try
            {
                Directory.CreateDirectory(BackupDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"backup_{timestamp}.bak";
                string backupFullPath = Path.Combine(BackupDir, backupFileName);

                string connString = System.Configuration.ConfigurationManager
                    .ConnectionStrings["ReestrObrashcheniy.Properties.Settings.РеестрОбращенийConnectionString"]
                    .ConnectionString;

                string dbName = new SqlConnectionStringBuilder(connString).InitialCatalog;

                if (string.IsNullOrEmpty(dbName))
                    throw new Exception("Не удалось определить имя базы данных.");

                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string sql = $@"
                        BACKUP DATABASE [{dbName}] 
                        TO DISK = '{backupFullPath}' 
                        WITH FORMAT, INIT, NAME = 'Full Backup', SKIP, STATS = 10";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 180;
                        cmd.ExecuteNonQuery();
                    }
                }

                string checksum = ComputeMD5(backupFullPath);
                long size = new FileInfo(backupFullPath).Length;

                LogBackupEvent($"CREATE | {backupFileName} | {checksum} | {size} bytes");

                MessageBox.Show($"Резервная копия успешно создана!\n\n" +
                                $"Файл: {backupFileName}\n" +
                                $"Папка: {BackupDir}",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                Logger.Info("Backup", $"Backup created successfully: {backupFullPath}");
                return backupFullPath;
            }
            catch (Exception ex)
            {
                LogBackupEvent($"ERROR | CREATE | {ex.Message}");
                MessageBox.Show($"Не удалось создать резервную копию.\n\n" +
                                $"Ошибка: {ex.Message}\n\n" +
                                $"Папка: {BackupDir}\n\n" +
                                $"Рекомендация:\n" +
                                "1. Закройте SSMS полностью\n" +
                                "2. Попробуйте создать бэкап ещё раз",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Восстановление базы данных из резервной копии
        /// </summary>
        public static bool RestoreBackup(string backupPath)
        {
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Файл резервной копии не найден.", backupPath);

            string connString = System.Configuration.ConfigurationManager
                .ConnectionStrings["ReestrObrashcheniy.Properties.Settings.РеестрОбращенийConnectionString"]
                .ConnectionString;

            string dbName = new SqlConnectionStringBuilder(connString).InitialCatalog;

            try
            {
                // 1. Создаём страховочную копию текущей базы
                CreateBackup();

                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // 2. Переключаемся на базу master (обязательно!)
                    conn.ChangeDatabase("master");

                    // 3. Отключаем всех пользователей от целевой базы
                    string sqlSingle = $@"
                ALTER DATABASE [{dbName}] 
                SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                    new SqlCommand(sqlSingle, conn).ExecuteNonQuery();

                    // 4. Выполняем восстановление
                    string sqlRestore = $@"
                RESTORE DATABASE [{dbName}] 
                FROM DISK = '{backupPath}' 
                WITH REPLACE, RECOVERY";

                    new SqlCommand(sqlRestore, conn).ExecuteNonQuery();

                    // 5. Возвращаем нормальный режим
                    string sqlMulti = $@"
                ALTER DATABASE [{dbName}] 
                SET MULTI_USER";
                    new SqlCommand(sqlMulti, conn).ExecuteNonQuery();
                }

                LogBackupEvent($"RESTORE | {Path.GetFileName(backupPath)} | OK");

                MessageBox.Show("База данных успешно восстановлена из резервной копии!\n\n" +
                                "Приложение будет закрыто для применения изменений.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                Application.Current.Shutdown();
                Logger.Info("Backup", $"Backup restored: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                LogBackupEvent($"ERROR | RESTORE | {ex.Message}");
                MessageBox.Show($"Ошибка восстановления базы данных:\n{ex.Message}\n\n" +
                                "Рекомендация: Закройте все другие программы, работающие с базой (SSMS и т.д.).",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static string ComputeMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private static void LogBackupEvent(string message)
        {
            try
            {
                Directory.CreateDirectory(BackupDir);
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
                File.AppendAllText(LogFile, logEntry + Environment.NewLine);
            }
            catch { }
        }

        public static List<BackupInfo> ListBackups()
        {
            if (!Directory.Exists(BackupDir))
                return new List<BackupInfo>();

            return Directory.GetFiles(BackupDir, "backup_*.bak")
                .Select(file => new BackupInfo
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    Created = File.GetCreationTime(file),
                    Size = new FileInfo(file).Length,
                    Checksum = ComputeMD5(file)
                })
                .OrderByDescending(b => b.Created)
                .ToList();
        }

        public static int CleanupOldBackups(int days) => 0;
    }

    public class BackupInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime Created { get; set; }
        public long Size { get; set; }
        public string Checksum { get; set; }
    }
}