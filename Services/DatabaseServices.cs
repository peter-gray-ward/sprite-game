using Npgsql;

namespace App.Services
{
    public class DatabaseServices
    {
        private string _connectionString;

        public DatabaseServices(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string GetConnectionString() => _connectionString;

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}