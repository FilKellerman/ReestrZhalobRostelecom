using System;

namespace ReestrObrashcheniy
{
    /// <summary>
    /// Текущий авторизованный пользователь (доступен из любого окна)
    /// </summary>
    public static class CurrentUser
    {
        public static string Login { get; set; } = string.Empty;
        public static string FIO { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;

        public static bool IsAdmin => Role == "Администратор";
        public static bool IsOperator => Role == "Оператор";

        public static void SetUser(string login, string fio, string role)
        {
            Login = login;
            FIO = fio;
            Role = role;

            Logger.Info("Security", $"Current user set: {login} ({role})");
        }

        public static void Clear()
        {
            Login = FIO = Role = string.Empty;
        }
    }
}