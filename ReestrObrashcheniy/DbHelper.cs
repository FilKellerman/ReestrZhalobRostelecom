using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace ReestrObrashcheniy
{
    public static class DbHelper
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["ReestrObrashcheniy.Properties.Settings.РеестрОбращенийConnectionString"].ConnectionString;

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        // Получить список пользователей для авторизации
        public static DataTable GetПользователи()
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = GetConnection())
            {
                string sql = "SELECT Логин, ПарольХэш, Роль, ФИО FROM Пользователи WHERE Активен = 1";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        }

        public static (bool Success, string Role, string ФИО) Авторизоваться(string login, string password)
        {
            string hash = GetMd5Hash(password);

            using (SqlConnection conn = GetConnection())
            {
                string sql = "SELECT Роль, ФИО FROM Пользователи WHERE Логин = @login AND ПарольХэш = @hash AND Активен = 1";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@hash", hash);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (true, reader["Роль"].ToString(), reader["ФИО"].ToString());
                        }
                    }
                }
            }

            return (false, "", "");
        }

        // Тестовый MD5-хэш (только для примера!)
        private static string GetMd5Hash(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                var sb = new System.Text.StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // Удаляем строку с CurrentUserFIO
        // public static DataTable GetОбращения(string currentUserLogin = null)
        public static DataTable GetОбращения(string currentUserLogin = null)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = GetConnection())
            {
                string sql = @"
            SELECT 
                o.ID,
                c.ФИО AS Клиент_ФИО,
                o.Описание,
                o.ДатаПоступления,
                o.Статус,
                s.ФИО AS Сотрудник_ФИО,
                s.ID AS Сотрудник_ID
            FROM Обращения o
            LEFT JOIN Клиенты c ON o.Клиент_ID = c.ID
            LEFT JOIN Назначения n ON o.ID = n.Обращение_ID
            LEFT JOIN Сотрудники s ON n.Сотрудник_ID = s.ID";

                if (!string.IsNullOrEmpty(currentUserLogin))
                {
                    sql += @"
                WHERE n.Сотрудник_ID IN (
                    SELECT ID FROM Сотрудники 
                    WHERE ФИО = (SELECT ФИО FROM Пользователи WHERE Логин = @Login)
                )";
                }

                sql += " ORDER BY o.ДатаПоступления DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(currentUserLogin))
                    {
                        cmd.Parameters.AddWithValue("@Login", currentUserLogin);
                    }

                    conn.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        // Получить всех Клиентов
        public static DataTable GetКлиенты()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = GetConnection())
            {
                string sql = "SELECT ID, ФИО, Адрес, Телефон FROM Клиенты ORDER BY ФИО";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        // Получить всех Сотрудников
        public static DataTable GetСотрудники()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = GetConnection())
            {
                string sql = "SELECT ID, ФИО, Должность, Email, Телефон FROM Сотрудники ORDER BY ФИО";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        // Получить все Ответы с обращением и сотрудником
        public static DataTable GetОтветы()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = GetConnection())
            {
                string sql = @"
            SELECT 
                r.ID,
                r.Назначение_ID,               -- ← ДОБАВЛЯЕМ ЭТОТ СТОЛБЕЦ
                n.Обращение_ID,
                s.ФИО AS Сотрудник_ФИО,
                r.Текст,
                r.ДатаОтвета,
                r.ЭтоВнутренний
            FROM Ответы r
            INNER JOIN Назначения n ON r.Назначение_ID = n.ID
            INNER JOIN Сотрудники s ON n.Сотрудник_ID = s.ID
            ORDER BY r.ДатаОтвета DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        // Получить непрочитанные уведомления для оператора
        public static string ПолучитьНепрочитанныеУведомления(string логин)
        {
            string текст = "";
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = @"
            SELECT Текст, Дата
            FROM Уведомления
            WHERE ЛогинОператора = @Логин AND Прочитано = 0
            ORDER BY Дата DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Логин", логин);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            текст += $"[{reader["Дата"]:dd.MM.yyyy HH:mm}] {reader["Текст"]}\n\n";
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(текст))
                return "Уведомление от администратора:\n\n" + текст;

            return "";
        }

        // Пометить все уведомления как прочитанные (после показа)
        public static void ПометитьУведомленияПрочитанными(string логин)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "UPDATE Уведомления SET Прочитано = 1 WHERE ЛогинОператора = @Логин AND Прочитано = 0";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Логин", логин);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}