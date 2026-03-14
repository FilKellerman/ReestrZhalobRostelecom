using System;
using System.Windows;

namespace ReestrObrashcheniy
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtLogin.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                txtError.Text = "Введите логин и пароль!";
                return;
            }

            try
            {
                var (success, role, fio) = DbHelper.Авторизоваться(login, password);

                if (success)
                {
                    MainWindow main = new MainWindow();
                    main.CurrentRole = role;
                    main.CurrentUserFIO = fio;
                    main.CurrentUserLogin = login;

                    // Проверяем и показываем уведомления (только для оператора)
                    if (role == "Оператор")
                    {
                        string уведомление = DbHelper.ПолучитьНепрочитанныеУведомления(login);  // ← добавили DbHelper.
                        if (!string.IsNullOrEmpty(уведомление))
                        {
                            MessageBox.Show(уведомление, "Уведомление от администратора", MessageBoxButton.OK, MessageBoxImage.Information);
                            DbHelper.ПометитьУведомленияПрочитанными(login);  // ← добавили DbHelper.
                        }
                    }

                    main.Show();
                    this.Close();
                }
                else
                {
                    txtError.Text = "Неверный логин или пароль!";
                }
            }
            catch (Exception ex)
            {
                txtError.Text = "Ошибка: " + ex.Message;
            }
        }
    }
}