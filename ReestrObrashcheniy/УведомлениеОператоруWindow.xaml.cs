using System.Diagnostics;
using System.Windows;

namespace ReestrObrashcheniy
{
    public partial class УведомлениеОператоруWindow : Window
    {
        public string Логин { get; private set; }
        public string Текст { get; private set; }

        public УведомлениеОператоруWindow()
        {
            InitializeComponent();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            ConfigManager.ApplySettings(this);
            stopwatch.Stop();
        }

        private void BtnОтправить_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtЛогин.Text) || string.IsNullOrWhiteSpace(txtТекст.Text))
            {
                MessageBox.Show("Заполните логин и текст!");
                return;
            }

            Логин = txtЛогин.Text.Trim();
            Текст = txtТекст.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void BtnОтмена_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}