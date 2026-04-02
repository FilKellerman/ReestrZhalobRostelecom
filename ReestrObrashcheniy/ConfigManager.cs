using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Newtonsoft.Json;

namespace ReestrObrashcheniy
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = "config.json";

        public static ConfigSettings Settings { get; private set; } = new ConfigSettings();

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    Settings = JsonConvert.DeserializeObject<ConfigSettings>(json) ?? new ConfigSettings();
                }
                else
                {
                    Settings = new ConfigSettings();
                    SaveConfig();
                }

                ValidateConfig();
                ApplySettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки конфигурации.\nИспользуются настройки по умолчанию.\n" + ex.Message);
                Settings = new ConfigSettings();
            }
        }

        public static void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения конфигурации:\n" + ex.Message);
            }
        }

        private static void ValidateConfig()
        {
            if (Settings.Interface.Scale < 75 || Settings.Interface.Scale > 150)
                Settings.Interface.Scale = 100;

            if (string.IsNullOrWhiteSpace(Settings.Interface.FontFamily))
                Settings.Interface.FontFamily = "Arial";
        }

        public static void ApplySettings(Window targetWindow = null)
        {
            try
            {
                double scale = Settings.Interface.Scale / 100.0;
                FontFamily font = new FontFamily(Settings.Interface.FontFamily);

                bool isDark = Settings.Interface.Theme == "dark";

                SolidColorBrush bgBrush = isDark
                    ? new SolidColorBrush(Color.FromRgb(35, 47, 62))
                    : new SolidColorBrush(Colors.White);

                SolidColorBrush textBrush = isDark
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Colors.Black);

                if (targetWindow != null)
                {
                    targetWindow.LayoutTransform = new ScaleTransform(scale, scale);
                    targetWindow.FontFamily = font;
                    targetWindow.Background = bgBrush;
                    ApplyThemeToWindow(targetWindow, textBrush);
                    return;
                }

                foreach (Window w in Application.Current.Windows)
                {
                    if (w == null) continue;
                    w.LayoutTransform = new ScaleTransform(scale, scale);
                    w.FontFamily = font;
                    w.Background = bgBrush;
                    ApplyThemeToWindow(w, textBrush);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ApplySettings error: " + ex.Message);
            }
        }

        private static void ApplyThemeToWindow(Window window, SolidColorBrush textBrush)
        {
            SolidColorBrush inputBackgroundBrush = textBrush.Color == Colors.White
                ? new SolidColorBrush(Color.FromRgb(45, 45, 45))
                : new SolidColorBrush(Colors.White);

            SolidColorBrush dataGridRowBrush = textBrush.Color == Colors.White
                ? new SolidColorBrush(Color.FromRgb(40, 40, 40))
                : new SolidColorBrush(Colors.White);
            SolidColorBrush dataGridAltRowBrush = textBrush.Color == Colors.White
                ? new SolidColorBrush(Color.FromRgb(50, 50, 50))
                : new SolidColorBrush(Color.FromRgb(232, 240, 254));

            foreach (var child in GetAllVisualChildren(window))
            {
                if (child is TextBlock tb)
                {
                    tb.Foreground = textBrush;
                }
                else if (child is Control ctrl)
                {
                    // Не трогаем кнопки, комбобоксы и заголовки DataGrid
                    if (ctrl is Button || ctrl is ComboBox || ctrl is DataGridColumnHeader)
                        continue;

                    ctrl.Foreground = textBrush;

                    if (ctrl is TextBox textBox)
                    {
                        textBox.Background = inputBackgroundBrush;
                        textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                    }
                    else if (ctrl is PasswordBox passwordBox)
                    {
                        passwordBox.Background = inputBackgroundBrush;
                        passwordBox.BorderBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                        passwordBox.Foreground = textBrush;
                    }
                    else if (ctrl is GroupBox groupBox)
                    {
                        var header = groupBox.Header as TextBlock;
                        if (header != null)
                            header.Foreground = textBrush;
                    }
                    else if (ctrl is DataGrid dataGrid)
                    {
                        dataGrid.Foreground = textBrush;
                        dataGrid.Background = dataGridRowBrush;
                        dataGrid.RowBackground = dataGridRowBrush;
                        dataGrid.AlternatingRowBackground = dataGridAltRowBrush;
                        dataGrid.Items.Refresh();
                    }
                }
            }
        }

        private static System.Collections.Generic.IEnumerable<DependencyObject> GetAllVisualChildren(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (var descendant in GetAllVisualChildren(child))
                    yield return descendant;
            }
        }


    }
}