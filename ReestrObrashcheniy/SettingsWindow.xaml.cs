using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReestrObrashcheniy
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            ConfigManager.ApplySettings(this);
            LoadCurrentSettings();                    // загружаем данные
            this.Loaded += (s, e) => ConfigManager.ApplySettings(this);
            stopwatch.Stop();
        }

        private void LoadCurrentSettings()
        {
            var s = ConfigManager.Settings;

            // Тема
            cmbTheme.SelectedIndex = s.Interface.Theme == "light" ? 0 : 1;

            // Масштаб
            cmbScale.SelectedItem = cmbScale.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString() == s.Interface.Scale + "%")
                ?? cmbScale.Items[1];

            // Шрифт
            cmbFont.SelectedItem = cmbFont.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString() == s.Interface.FontFamily)
                ?? cmbFont.Items[0];

            // Автобэкап
            chkAutoBackup.IsChecked = s.Backup.AutoBackup;
            txtBackupInterval.Text = s.Backup.BackupIntervalDays.ToString();

            // Дополнительные функции
            chkTips.IsChecked = s.Features.TipsEnabled;
            chkSound.IsChecked = s.Features.SoundEnabled;
            chkHotkeys.IsChecked = s.Features.HotkeysEnabled;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var s = ConfigManager.Settings;

                s.Interface.Theme = (cmbTheme.SelectedIndex == 0) ? "light" : "dark";
                s.Interface.Scale = int.Parse(((ComboBoxItem)cmbScale.SelectedItem).Content.ToString().TrimEnd('%'));
                s.Interface.FontFamily = ((ComboBoxItem)cmbFont.SelectedItem).Content.ToString();

                s.Backup.AutoBackup = chkAutoBackup.IsChecked ?? false;
                s.Backup.BackupIntervalDays = int.TryParse(txtBackupInterval.Text, out int days) ? days : 7;

                s.Features.TipsEnabled = chkTips.IsChecked ?? true;
                s.Features.SoundEnabled = chkSound.IsChecked ?? false;
                s.Features.HotkeysEnabled = chkHotkeys.IsChecked ?? true;

                Logger.Info("Settings", $"User {CurrentUser.Login} changed settings: " +
                                    $"Theme={ConfigManager.Settings.Interface.Theme}, " +
                                    $"Scale={ConfigManager.Settings.Interface.Scale}%");

                ConfigManager.SaveConfig();
                ConfigManager.ApplySettings();

                MessageBox.Show("Настройки успешно сохранены и применены!\n\n" +
                                "Тёмная тема использует белый шрифт для лучшей читаемости.",
                                "Сохранено",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Settings", "Error saving configuration", ex);
                MessageBox.Show("Ошибка при сохранении настроек:\n" + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Запрет ввода нецифровых символов в интервал бэкапа
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text[0]);
        }
    }
}