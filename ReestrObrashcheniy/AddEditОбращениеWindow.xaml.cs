using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ReestrObrashcheniy
{
    public partial class AddEditОбращениеWindow : Window
    {
        private int? editID = null;
        private bool isOperatorMode = false;
        private string currentUserLogin = null;

        public AddEditОбращениеWindow(int? id = null, bool operatorMode = false, string userLogin = null)
        {
            InitializeComponent();
            editID = id;
            isOperatorMode = operatorMode;
            currentUserLogin = userLogin;

            LoadКлиенты();

            if (id.HasValue)
            {
                LoadДляРедактирования(id.Value);
            }
        }

        private void LoadКлиенты()
        {
            DataTable dt = DbHelper.GetКлиенты();
            cmbКлиент.ItemsSource = dt.DefaultView;
            cmbКлиент.SelectedValuePath = "ID";
            cmbКлиент.DisplayMemberPath = "ФИО";
        }

        private void LoadДляРедактирования(int id)
        {
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT o.Клиент_ID, o.Описание, o.Статус
                    FROM Обращения o
                    WHERE o.ID = @ID";

                if (isOperatorMode && !string.IsNullOrEmpty(currentUserLogin))
                {
                    sql += @"
                        AND EXISTS (
                            SELECT 1 FROM Назначения n
                            INNER JOIN Сотрудники s ON n.Сотрудник_ID = s.ID
                            INNER JOIN Пользователи p ON s.ФИО = p.ФИО
                            WHERE n.Обращение_ID = o.ID AND p.Логин = @Login
                        )";
                }

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    if (isOperatorMode && !string.IsNullOrEmpty(currentUserLogin))
                        cmd.Parameters.AddWithValue("@Login", currentUserLogin);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            cmbКлиент.SelectedValue = reader["Клиент_ID"];
                            txtОписание.Text = reader["Описание"].ToString();
                            cmbСтатус.SelectedItem = cmbСтатус.Items.Cast<ComboBoxItem>()
                                .FirstOrDefault(item => item.Content.ToString() == reader["Статус"].ToString());
                        }
                        else
                        {
                            MessageBox.Show("Это обращение не принадлежит вам или не существует!");
                            Close();
                        }
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbКлиент.SelectedValue == null || string.IsNullOrWhiteSpace(txtОписание.Text) || cmbСтатус.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента, заполните описание и статус!");
                return;
            }

            try
            {
                using (SqlConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // Проверка права редактирования для оператора
                    if (isOperatorMode && editID.HasValue)
                    {
                        string checkSql = @"
                            SELECT COUNT(*) FROM Обращения o
                            INNER JOIN Назначения n ON o.ID = n.Обращение_ID
                            INNER JOIN Сотрудники s ON n.Сотрудник_ID = s.ID
                            INNER JOIN Пользователи p ON s.ФИО = p.ФИО
                            WHERE o.ID = @ID AND p.Логин = @Login";
                        using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@ID", editID.Value);
                            checkCmd.Parameters.AddWithValue("@Login", currentUserLogin);
                            int count = (int)checkCmd.ExecuteScalar();
                            if (count == 0)
                            {
                                MessageBox.Show("Вы не можете редактировать это обращение!");
                                return;
                            }
                        }
                    }

                    string sql = editID.HasValue
                        ? "UPDATE Обращения SET Клиент_ID = @Клиент_ID, Описание = @Описание, Статус = @Статус WHERE ID = @ID"
                        : "INSERT INTO Обращения (Клиент_ID, Описание, Статус) OUTPUT INSERTED.ID VALUES (@Клиент_ID, @Описание, @Статус)";

                    int обращениеID;
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Клиент_ID", cmbКлиент.SelectedValue);
                        cmd.Parameters.AddWithValue("@Описание", txtОписание.Text);
                        cmd.Parameters.AddWithValue("@Статус", (cmbСтатус.SelectedItem as ComboBoxItem).Content.ToString());

                        if (editID.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@ID", editID.Value);
                            cmd.ExecuteNonQuery();
                            обращениеID = editID.Value;
                        }
                        else
                        {
                            обращениеID = (int)cmd.ExecuteScalar();
                        }
                    }

                    // Автоматическое назначение оператора на новое обращение
                    if (!editID.HasValue && isOperatorMode && !string.IsNullOrEmpty(currentUserLogin))
                    {
                        string sqlНазначение = @"
                            INSERT INTO Назначения (Обращение_ID, Сотрудник_ID, ДатаНазначения)
                            SELECT @Обращение_ID, s.ID, GETDATE()
                            FROM Сотрудники s
                            INNER JOIN Пользователи p ON s.ФИО = p.ФИО
                            WHERE p.Логин = @Login";

                        using (SqlCommand cmd = new SqlCommand(sqlНазначение, conn))
                        {
                            cmd.Parameters.AddWithValue("@Обращение_ID", обращениеID);
                            cmd.Parameters.AddWithValue("@Login", currentUserLogin);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Обращение сохранено!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения:\n" + ex.Message);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}