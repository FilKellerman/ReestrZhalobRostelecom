using System;
using System.Windows;
using System.Windows.Controls;

namespace ReestrObrashcheniy
{
    public partial class ChangeStatusWindow : Window
    {
        public int ОбращениеID { get; set; }
        public string НовыйСтатус { get; private set; }

        public ChangeStatusWindow(int id)
        {
            InitializeComponent();
            ОбращениеID = id;
            cmbСтатус.SelectedIndex = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            ConfigManager.ApplySettings(this);

            stopwatch.Stop();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbСтатус.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус!");
                return;
            }

            НовыйСтатус = (cmbСтатус.SelectedItem as ComboBoxItem).Content.ToString();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}