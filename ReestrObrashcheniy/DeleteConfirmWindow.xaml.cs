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
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            DialogResult = true;  // ← главное изменение!
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