using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace ReestrObrashcheniy
{
    public partial class AddEditКлиентWindow : Window
    {
        private int? editID = null;

        public AddEditКлиентWindow(int? id = null)
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
            DataTable dt = DbHelper.GetКлиенты();
            DataRow[] rows = dt.Select($"ID = {id}");
            if (rows.Length > 0)
            {
                txtФИО.Text = rows[0]["ФИО"].ToString();
                txtАдрес.Text = rows[0]["Адрес"]?.ToString();
                txtТелефон.Text = rows[0]["Телефон"]?.ToString();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtФИО.Text))
            {
                Logger.Warning("UI", "Attempt to save client without FIO");
                MessageBox.Show("Заполните ФИО!");
                return;
            }

            try
            {
                using (SqlConnection conn = DbHelper.GetConnection())
                {

                    conn.Open();
                    string sql = editID.HasValue
                        ? "UPDATE Клиенты SET ФИО = @ФИО, Адрес = @Адрес, Телефон = @Телефон WHERE ID = @ID"
                        : "INSERT INTO Клиенты (ФИО, Адрес, Телефон) VALUES (@ФИО, @Адрес, @Телефон)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ФИО", txtФИО.Text);
                        cmd.Parameters.AddWithValue("@Адрес", (object)txtАдрес.Text ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Телефон", string.IsNullOrWhiteSpace(txtТелефон.Text) ? (object)DBNull.Value : txtТелефон.Text);

                        if (editID.HasValue)
                            cmd.Parameters.AddWithValue("@ID", editID.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
                string action = editID.HasValue ? "Updated record" : "Added record";
                Logger.Audit(CurrentUser.Login, action, $"Table: Клиенты | ФИО: {txtФИО.Text}");

                MessageBox.Show("Клиент сохранён!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("UI", "Error saving client", ex);
                MessageBox.Show("Ошибка сохранения:\n" + ex.Message);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("UI", "Client edit window cancelled");
            DialogResult = false;
            Close();
        }
    }
}