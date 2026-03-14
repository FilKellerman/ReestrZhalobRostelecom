using System;
using System.Windows;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Controls;
using OfficeOpenXml;
using System.IO;
using System.ComponentModel;
using Microsoft.Win32;

namespace ReestrObrashcheniy
{
    public partial class MainWindow : Window
    {
        public string CurrentRole { get; set; }
        public string CurrentUserFIO { get; set; }
        public string CurrentUserLogin { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtПриветствие.Text = $"Добро пожаловать, {CurrentUserFIO} ({CurrentRole})";

            if (CurrentRole == "Оператор")
            {
                tabКлиенты.Visibility = Visibility.Collapsed;
                tabСотрудники.Visibility = Visibility.Collapsed;
                tabОтветы.Visibility = Visibility.Collapsed;
                BtnChangeStatus.Visibility = Visibility.Collapsed;
                BtnAddОтвет.Visibility = Visibility.Collapsed;

            }
            else // Администратор
            {
                // Всё видно
                tabОбращения.Visibility = Visibility.Visible;
                tabКлиенты.Visibility = Visibility.Visible;
                tabСотрудники.Visibility = Visibility.Visible;
                tabОтветы.Visibility = Visibility.Visible;
            }

            if (new Random().Next(0, 3) == 0) // 1 шанс из 3
            {
                BtnПодсказка_Click(null, null); // покажет случайную подсказку
            }

            // Загружаем данные
            LoadОбращения();
            LoadКлиенты();
            LoadСотрудники();
            LoadОтветы();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadОбращения()
        {
            try
            {
                DataTable dt;
                if (CurrentRole == "Оператор")
                {
                    dt = DbHelper.GetОбращения(CurrentUserLogin); // передаём логин оператора
                }
                else
                {
                    dt = DbHelper.GetОбращения(); // все обращения
                }
                dgОбращения.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки обращений:\n" + ex.Message);
            }
        }

        // Загрузка клиентов (с учётом поиска)
        private void LoadКлиенты(string searchText = "")
        {
            try
            {
                DataTable dt = DbHelper.GetКлиенты();

                if (!string.IsNullOrEmpty(searchText))
                {
                    DataView dv = dt.DefaultView;
                    dv.RowFilter = $"ФИО LIKE '%{searchText}%'";
                    dgКлиенты.ItemsSource = dv;
                }
                else
                {
                    dgКлиенты.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки клиентов:\n" + ex.Message);
            }
        }

        // Поиск по тексту
        private void TxtПоискКлиент_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtПоискКлиент.Text.Trim();
            LoadКлиенты(search);
        }

        // Добавить клиента
        private void BtnAddКлиент_Click(object sender, RoutedEventArgs e)
        {
            AddEditКлиентWindow window = new AddEditКлиентWindow();
            if (window.ShowDialog() == true)
            {
                LoadКлиенты(txtПоискКлиент.Text); // обновляем с учётом текущего поиска
            }
        }

        // Редактировать клиента
        private void BtnEditКлиент_Click(object sender, RoutedEventArgs e)
        {
            if (dgКлиенты.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента для редактирования!");
                return;
            }

            DataRowView row = (DataRowView)dgКлиенты.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);

            AddEditКлиентWindow window = new AddEditКлиентWindow(id);
            if (window.ShowDialog() == true)
            {
                LoadКлиенты(txtПоискКлиент.Text); // обновляем с учётом поиска
                MessageBox.Show("Клиент обновлён!");
            }
        }

        // Удалить клиента
        private void BtnDeleteКлиент_Click(object sender, RoutedEventArgs e)
        {
            if (dgКлиенты.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента для удаления!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView row = (DataRowView)dgКлиенты.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);
            string фио = row["ФИО"].ToString();

            DeleteConfirmWindow confirm = new DeleteConfirmWindow($"Клиент: {фио} (ID: {id})");

            if (confirm.ShowDialog() == true)  // ← теперь работает правильно
            {
                try
                {
                    using (SqlConnection conn = DbHelper.GetConnection())
                    {
                        conn.Open();

                        // Проверка на связанные обращения
                        string checkSql = "SELECT COUNT(*) FROM Обращения WHERE Клиент_ID = @ID";
                        using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@ID", id);
                            int count = (int)checkCmd.ExecuteScalar();

                            if (count > 0)
                            {
                                MessageBox.Show(
                                    $"Невозможно удалить клиента!\n\n" +
                                    $"У клиента **{фио}** есть **{count}** обращений.\n" +
                                    "Сначала удалите или переназначьте связанные обращения.",
                                    "Запрещено удаление",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning
                                );
                                return;
                            }
                        }

                        // Если нет обращений — удаляем
                        string sql = "DELETE FROM Клиенты WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                LoadКлиенты(txtПоискКлиент.Text);
                                MessageBox.Show("Клиент успешно удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Клиент не найден или уже удалён.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Удаление отменено.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Загрузка Сотрудников
        private void LoadСотрудники(string searchText = "")
        {
            try
            {
                DataTable dt = DbHelper.GetСотрудники();

                if (!string.IsNullOrEmpty(searchText))
                {
                    DataView dv = dt.DefaultView;
                    dv.RowFilter = $"ФИО LIKE '%{searchText}%'";
                    dgСотрудники.ItemsSource = dv;
                }
                else
                {
                    dgСотрудники.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сотрудников:\n" + ex.Message);
            }
        }
        private void TxtПоискСотрудник_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtПоискСотрудник.Text.Trim();
            LoadСотрудники(search);
        }

        // Добавить сотрудника
        private void BtnAddСотрудник_Click(object sender, RoutedEventArgs e)
        {
            AddEditСотрудникWindow window = new AddEditСотрудникWindow();
            if (window.ShowDialog() == true)
            {
                LoadСотрудники(txtПоискСотрудник.Text);
            }
        }

        // Редактировать сотрудника
        private void BtnEditСотрудник_Click(object sender, RoutedEventArgs e)
        {
            if (dgСотрудники.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника!");
                return;
            }

            DataRowView row = (DataRowView)dgСотрудники.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);

            AddEditСотрудникWindow window = new AddEditСотрудникWindow(id);
            if (window.ShowDialog() == true)
            {
                LoadСотрудники(txtПоискСотрудник.Text);
            }
        }

        // Удалить сотрудника
        private void BtnDeleteСотрудник_Click(object sender, RoutedEventArgs e)
        {
            if (dgСотрудники.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника!");
                return;
            }

            DataRowView row = (DataRowView)dgСотрудники.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);
            string фио = row["ФИО"].ToString();

            DeleteConfirmWindow confirm = new DeleteConfirmWindow($"Сотрудник: {фио} (ID: {id})");
            if (confirm.ShowDialog() == true && confirm.Confirmed)
            {
                try
                {
                    using (SqlConnection conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = "DELETE FROM Сотрудники WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    LoadСотрудники(txtПоискСотрудник.Text);
                    MessageBox.Show("Сотрудник удалён!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления:\n" + ex.Message);
                }
            }
        }

        // Загрузка Ответов
        private void LoadОтветы()
        {
            try
            {
                DataTable dt = DbHelper.GetОтветы();
                dgОтветы.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки ответов:\n" + ex.Message);
            }
        }

        private void BtnAddОбращение_Click(object sender, RoutedEventArgs e)
        {
            AddEditОбращениеWindow window;
            if (CurrentRole == "Оператор")
            {
                window = new AddEditОбращениеWindow(null, true, CurrentUserLogin);
            }
            else
            {
                window = new AddEditОбращениеWindow();
            }

            if (window.ShowDialog() == true)
            {
                LoadОбращения();
            }
        }
        private void BtnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (dgОбращения.SelectedItem == null)
            {
                MessageBox.Show("Выберите обращение!");
                return;
            }

            // Получаем ID выбранного обращения
            DataRowView row = (DataRowView)dgОбращения.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);

            // Открываем окно изменения статуса
            ChangeStatusWindow window = new ChangeStatusWindow(id);
            if (window.ShowDialog() == true)
            {
                try
                {
                    using (SqlConnection conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = "UPDATE Обращения SET Статус = @Статус WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Статус", window.НовыйСтатус);
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Статус изменён!");
                    LoadОбращения(); // обновляем таблицу
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка изменения статуса:\n" + ex.Message);
                }
            }
        }

        // Редактировать обращение
        private void BtnEditОбращение_Click(object sender, RoutedEventArgs e)
        {
            if (dgОбращения.SelectedItem == null)
            {
                MessageBox.Show("Выберите обращение!");
                return;
            }

            DataRowView row = (DataRowView)dgОбращения.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);

            AddEditОбращениеWindow window;
            if (CurrentRole == "Оператор")
            {
                window = new AddEditОбращениеWindow(id, true, CurrentUserLogin);
            }
            else
            {
                window = new AddEditОбращениеWindow(id);
            }

            if (window.ShowDialog() == true)
            {
                LoadОбращения();
            }
        }

        // Удалить обращение
        private void BtnDeleteОбращение_Click(object sender, RoutedEventArgs e)
        {
            if (dgОбращения.SelectedItem == null)
            {
                MessageBox.Show("Выберите обращение для удаления!");
                return;
            }

            DataRowView row = (DataRowView)dgОбращения.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);
            string описание = row["Описание"].ToString().Substring(0, Math.Min(50, row["Описание"].ToString().Length)) + "...";

            DeleteConfirmWindow confirm = new DeleteConfirmWindow($"Обращение: {описание} (ID: {id})");
            if (confirm.ShowDialog() == true && confirm.Confirmed)
            {
                try
                {
                    using (SqlConnection conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = "DELETE FROM Обращения WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    LoadОбращения();
                    MessageBox.Show("Обращение удалено!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления:\n" + ex.Message);
                }
            }
        }

        private void TxtПоискОбращение_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtПоискОбращение.Text.Trim().ToLower();

            DataTable dt = DbHelper.GetОбращения(CurrentRole == "Оператор" ? CurrentUserLogin : null);

            if (!string.IsNullOrEmpty(search))
            {
                DataView dv = dt.DefaultView;
                dv.RowFilter = $"Описание LIKE '%{search}%' OR Клиент_ФИО LIKE '%{search}%' OR Статус LIKE '%{search}%'";
                dgОбращения.ItemsSource = dv;
            }
            else
            {
                dgОбращения.ItemsSource = dt.DefaultView;
            }
        }

        // Поиск по ответам
        private void TxtПоискОтвет_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtПоискОтвет.Text.Trim().ToLower();

            try
            {
                DataTable dt = DbHelper.GetОтветы();

                if (!string.IsNullOrEmpty(search))
                {
                    DataView dv = dt.DefaultView;
                    dv.RowFilter = $"Текст LIKE '%{search}%' OR Сотрудник_ФИО LIKE '%{search}%' OR Convert(Обращение_ID, 'System.String') LIKE '%{search}%'";
                    dgОтветы.ItemsSource = dv;
                }
                else
                {
                    dgОтветы.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка поиска:\n" + ex.Message);
            }
        }

        // Добавить ответ
        private void BtnAddОтвет_Click(object sender, RoutedEventArgs e)
        {
            if (dgОбращения.SelectedItem == null)
            {
                MessageBox.Show("Сначала выберите обращение во вкладке 'Обращения'!");
                return;
            }

            DataRowView row = (DataRowView)dgОбращения.SelectedItem;
            int обращениеID = Convert.ToInt32(row["ID"]);

            // Находим Назначение_ID
            int? назначениеID = null;
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                conn.Open();
                string sql = "SELECT ID FROM Назначения WHERE Обращение_ID = @Обращение_ID";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Обращение_ID", обращениеID);
                    var result = cmd.ExecuteScalar();
                    if (result != null) назначениеID = Convert.ToInt32(result);
                }
            }

            if (!назначениеID.HasValue)
            {
                MessageBox.Show("У этого обращения нет назначенного сотрудника!");
                return;
            }

            AddEditОтветWindow window = new AddEditОтветWindow(назначениеID.Value);
            if (window.ShowDialog() == true)
            {
                LoadОтветы();
                LoadОбращения(); // на случай изменения статуса
            }
        }

        // Редактировать ответ
        private void BtnEditОтвет_Click(object sender, RoutedEventArgs e)
        {
            if (dgОтветы.SelectedItem == null)
            {
                MessageBox.Show("Выберите ответ для редактирования!");
                return;
            }

            DataRowView row = (DataRowView)dgОтветы.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);
            int назначениеID = Convert.ToInt32(row["Назначение_ID"]); // можно добавить в SELECT, если нужно

            AddEditОтветWindow window = new AddEditОтветWindow(назначениеID, id);
            if (window.ShowDialog() == true)
            {
                LoadОтветы();
            }
        }

        // Удалить ответ
        private void BtnDeleteОтвет_Click(object sender, RoutedEventArgs e)
        {
            if (dgОтветы.SelectedItem == null)
            {
                MessageBox.Show("Выберите ответ!");
                return;
            }

            DataRowView row = (DataRowView)dgОтветы.SelectedItem;
            int id = Convert.ToInt32(row["ID"]);
            string текст = row["Текст"].ToString().Substring(0, Math.Min(30, row["Текст"].ToString().Length)) + "...";

            DeleteConfirmWindow confirm = new DeleteConfirmWindow($"Ответ: {текст} (ID: {id})");
            if (confirm.ShowDialog() == true && confirm.Confirmed)
            {
                try
                {
                    using (SqlConnection conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = "DELETE FROM Ответы WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    LoadОтветы();
                    MessageBox.Show("Ответ удалён!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления:\n" + ex.Message);
                }
            }
        }

        private void BtnУведомлениеОператору_Click(object sender, RoutedEventArgs e)
        {
            var window = new УведомлениеОператоруWindow();
            if (window.ShowDialog() == true)
            {
                string логинОператора = window.Логин;
                string текст = window.Текст;

                try
                {
                    using (SqlConnection conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = @"
                    INSERT INTO Уведомления (ЛогинОператора, Текст, Дата, Прочитано)
                    VALUES (@Логин, @Текст, GETDATE(), 0)";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Логин", логинОператора);
                            cmd.Parameters.AddWithValue("@Текст", текст);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"Уведомление отправлено оператору '{логинОператора}'!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка отправки уведомления:\n" + ex.Message);
                }
            }
        }

        private void BtnExportCSVWithSaveDialog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable dt = DbHelper.GetОбращения(CurrentRole == "Оператор" ? CurrentUserLogin : null);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта!");
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
                saveFileDialog.Title = "Сохранить экспорт обращений";
                saveFileDialog.FileName = $"Обращения_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                bool? result = saveFileDialog.ShowDialog();

                if (result == true)
                {
                    // Пользователь выбрал файл и нажал "Сохранить"
                    string filePath = saveFileDialog.FileName;

                    using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                    {
                        // Заголовки
                        for (int col = 0; col < dt.Columns.Count; col++)
                        {
                            sw.Write($"\"{dt.Columns[col].ColumnName}\"");
                            if (col < dt.Columns.Count - 1) sw.Write(",");
                        }
                        sw.WriteLine();

                        // Данные
                        foreach (DataRow row in dt.Rows)
                        {
                            for (int col = 0; col < dt.Columns.Count; col++)
                            {
                                string value = row[col]?.ToString() ?? "";
                                value = value.Replace("\"", "\"\"");
                                sw.Write($"\"{value}\"");
                                if (col < dt.Columns.Count - 1) sw.Write(",");
                            }
                            sw.WriteLine();
                        }
                    }

                    MessageBox.Show($"Файл успешно сохранён:\n{filePath}\n\nОткройте в Excel!");
                }
                else
                {
                    // Пользователь нажал "Отмена" или закрыл диалог
                    MessageBox.Show("Действие отменено.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта:\n" + ex.Message);
            }
        }

        private void BtnПодсказка_Click(object sender, RoutedEventArgs e)
        {
            string[] подсказки = new string[]
            {
        "Не забудьте указать точное описание проблемы — это ускорит обработку!",
        "Если клиент звонит повторно — проверьте статус в системе перед разговором.",
        "Статус 'На уточнении' используйте, когда нужны дополнительные данные от клиента.",
        "Прикрепляйте фото/скриншоты в описание — это помогает техникам.",
        "Не удаляйте старые обращения — они нужны для статистики.",
        "Используйте внутренние комментарии для заметок коллегам.",
        "Проверяйте назначенного сотрудника — он отвечает за обращение.",
        "Экспортируйте данные в Excel, если нужно отчитаться руководству."
            };

            Random rand = new Random();
            string случайная = подсказки[rand.Next(подсказки.Length)];

            MessageBox.Show(случайная, "Полезная подсказка", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}