using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;

namespace ReestrObrashcheniy
{
    public partial class AddEditСотрудникWindow : Window
    {
        private int? editID = null;

        public AddEditСотрудникWindow(int? id = null)
        {
            InitializeComponent();
            editID = id;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            ConfigManager.ApplySettings(this);  // ✅
            if (id.HasValue) LoadДляРедактирования(id.Value);

            if (id.HasValue)
            {
                LoadДляРедактирования(id.Value);
            }

            stopwatch.Stop();
        }

        private void LoadДляРедактирования(int id)
        {
            DataTable dt = DbHelper.GetСотрудники();
            DataRow[] rows = dt.Select($"ID = {id}");
            if (rows.Length > 0)
            {
                txtФИО.Text = rows[0]["ФИО"].ToString();
                txtДолжность.Text = rows[0]["Должность"].ToString();
                txtEmail.Text = rows[0]["Email"]?.ToString();
                txtТелефон.Text = rows[0]["Телефон"]?.ToString();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtФИО.Text) || string.IsNullOrWhiteSpace(txtДолжность.Text))
            {
                MessageBox.Show("Заполните ФИО и Должность!");
                return;
            }

            try
            {
                using (SqlConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = editID.HasValue
                        ? "UPDATE Сотрудники SET ФИО = @ФИО, Должность = @Должность, Email = @Email, Телефон = @Телефон WHERE ID = @ID"
                        : "INSERT INTO Сотрудники (ФИО, Должность, Email, Телефон) VALUES (@ФИО, @Должность, @Email, @Телефон)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ФИО", txtФИО.Text);
                        cmd.Parameters.AddWithValue("@Должность", txtДолжность.Text);
                        cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text);
                        cmd.Parameters.AddWithValue("@Телефон", string.IsNullOrWhiteSpace(txtТелефон.Text) ? (object)DBNull.Value : txtТелефон.Text);

                        if (editID.HasValue)
                            cmd.Parameters.AddWithValue("@ID", editID.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                string action = editID.HasValue ? "Updated record" : "Added record";
                Logger.Audit(CurrentUser.Login, action, $"Table: Сотрудники | ФИО: {txtФИО.Text}");

                MessageBox.Show("Сотрудник сохранён!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("UI", "Error saving Сотрудник", ex);
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