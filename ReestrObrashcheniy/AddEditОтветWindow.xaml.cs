using System;
using System.Data.SqlClient;
using System.Windows;

namespace ReestrObrashcheniy
{
    public partial class AddEditОтветWindow : Window
    {
        private int? editID = null; // ID ответа для редактирования
        private int НазначениеID { get; set; }

        public AddEditОтветWindow(int назначениеID, int? ответID = null)
        {
            InitializeComponent();
            НазначениеID = назначениеID;
            editID = ответID;
            txtОбращениеID.Text = "Обращение связано с назначением #" + назначениеID;

            if (ответID.HasValue)
            {
                LoadДляРедактирования(ответID.Value);
            }
        }

        private void LoadДляРедактирования(int id)
        {
            // Загрузка существующего ответа (можно сделать через DbHelper позже)
            // Пока заглушка — в реальности добавь SELECT
            txtТекст.Text = "Текст для редактирования...";
            chkВнутренний.IsChecked = false;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtТекст.Text))
            {
                MessageBox.Show("Введите текст ответа!");
                return;
            }

            try
            {
                using (SqlConnection conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = editID.HasValue
                        ? "UPDATE Ответы SET Текст = @Текст, ЭтоВнутренний = @ЭтоВнутренний WHERE ID = @ID"
                        : "INSERT INTO Ответы (Назначение_ID, Текст, ДатаОтвета, ЭтоВнутренний) VALUES (@Назначение_ID, @Текст, GETDATE(), @ЭтоВнутренний)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Текст", txtТекст.Text);
                        cmd.Parameters.AddWithValue("@ЭтоВнутренний", chkВнутренний.IsChecked ?? false);

                        if (editID.HasValue)
                            cmd.Parameters.AddWithValue("@ID", editID.Value);
                        else
                            cmd.Parameters.AddWithValue("@Назначение_ID", НазначениеID);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Ответ сохранён!");
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