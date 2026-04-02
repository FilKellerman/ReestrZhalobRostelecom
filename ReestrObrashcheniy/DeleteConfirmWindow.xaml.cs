using System.Diagnostics;
using System.Windows;

namespace ReestrObrashcheniy
{
    public partial class DeleteConfirmWindow : Window
    {
        public bool Confirmed { get; private set; } = false;

        public DeleteConfirmWindow(string infoText)
        {
            InitializeComponent();
            txtКлиентИнфо.Text = infoText;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            ConfigManager.ApplySettings(this);  // ✅
            stopwatch.Stop();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            DialogResult = true;
            Logger.Warning("Security", $"Delete blocked: {txtКлиентИнфо} has related records");
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;  // ← тоже желательно
            Close();
        }
    }
}