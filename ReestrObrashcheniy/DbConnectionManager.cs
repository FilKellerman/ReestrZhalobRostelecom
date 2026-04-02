using System;
using System.Data.SqlClient;

namespace ReestrObrashcheniy
{
    public static class DbConnectionManager
    {
        private static SqlConnection _connection;
        private static readonly object _lock = new object();

        public static SqlConnection GetConnection()
        {
            lock (_lock)
            {
                if (_connection == null || _connection.State == System.Data.ConnectionState.Closed)
                {
                    string connString = System.Configuration.ConfigurationManager
                        .ConnectionStrings["ReestrObrashcheniy.Properties.Settings.РеестрОбращенийConnectionString"]
                        .ConnectionString;

                    _connection = new SqlConnection(connString);
                    _connection.Open();

                    Logger.Info("DB", "Database connection opened (single connection mode)");
                }
                return _connection;
            }
        }

        public static void CloseConnection()
        {
            lock (_lock)
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Closed)
                {
                    _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                    Logger.Info("DB", "Database connection closed");
                }
            }
        }
    }
}