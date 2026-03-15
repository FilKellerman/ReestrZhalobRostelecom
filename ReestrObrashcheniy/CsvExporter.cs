using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;

namespace ReestrObrashcheniy
{
    public static class CsvExporter
    {
        public static bool ЭкспортВCSV(DataTable данные, string предложенноеИмяФайла)
        {
            try
            {
                if (данные == null || данные.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = предложенноеИмяФайла,
                    DefaultExt = ".csv",
                    Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*"
                };

                if (dlg.ShowDialog() != true)
                {
                    MessageBox.Show("Экспорт отменён.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                using (StreamWriter sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                {
                    // Заголовки
                    for (int i = 0; i < данные.Columns.Count; i++)
                    {
                        sw.Write($"\"{данные.Columns[i].ColumnName}\"");
                        if (i < данные.Columns.Count - 1) sw.Write(",");
                    }
                    sw.WriteLine();

                    // Данные
                    foreach (DataRow row in данные.Rows)
                    {
                        for (int i = 0; i < данные.Columns.Count; i++)
                        {
                            string value = row[i]?.ToString()?.Replace("\"", "\"\"") ?? "";
                            sw.Write($"\"{value}\"");
                            if (i < данные.Columns.Count - 1) sw.Write(",");
                        }
                        sw.WriteLine();
                    }
                }

                MessageBox.Show($"Файл сохранён:\n{dlg.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}