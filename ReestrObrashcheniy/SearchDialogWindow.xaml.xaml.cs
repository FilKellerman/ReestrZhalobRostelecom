using System.Windows;
using System.Windows.Controls;

namespace ReestrObrashcheniy
{
    public partial class SearchDialogWindow : Window
    {
        public string КритерийОписания { get; private set; }
        public string КритерийФИО { get; private set; }
        public string КритерийСтатус { get; private set; }

        public SearchDialogWindow()
        {
            InitializeComponent();
            cmbСтатус.SelectedIndex = 0; // "Все" по умолчанию
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            КритерийОписания = txtОписание.Text.Trim();
            КритерийФИО = txtФИОКлиента.Text.Trim();
            КритерийСтатус = (cmbСтатус.SelectedItem as ComboBoxItem)?.Content.ToString();

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