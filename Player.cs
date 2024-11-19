using Npgsql;
using BCrypt.Net;

namespace App
{
	public class Player
	{
		private string access_token { get; set; } = String.Empty;
		public async Task<string> Register(string connectionString, string name, string password)
		{
			try
			{
				using (var connection = new NpgsqlConnection(connectionString))
				{
					await connection.OpenAsync();

					// Hash the password using BCrypt
					var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

					var command = new NpgsqlCommand(@"
						INSERT INTO Player (name, level, position_x, position_y, password)
						VALUES (@name, 1, 0, 0, @password)
					", connection);

					// Add parameters to prevent SQL injection
					command.Parameters.AddWithValue("@name", name);
					command.Parameters.AddWithValue("@password", hashedPassword);

					await command.ExecuteNonQueryAsync();
				}
				return "success";
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during registration: {ex.Message}");
				return ex.Message;
			}
		}

		public async Task<string?> Login(string connectionString, string name, string password)
		{
			try
			{
				using (var connection = new NpgsqlConnection(connectionString))
				{
					await connection.OpenAsync();

					var command = new NpgsqlCommand(@"
						SELECT name, password FROM Player WHERE name = @name
					", connection);

					command.Parameters.AddWithValue("@name", name);

					var reader = await command.ExecuteReaderAsync();

					if (await reader.ReadAsync())
					{
						var storedPassword = reader.GetString(1);

						if (BCrypt.Net.BCrypt.Verify(password, storedPassword))
						{
							var token = Guid.NewGuid();
							this.access_token = token.ToString();
							return token.ToString();
						}
					}
				}
				return null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during login: {ex.Message}");
				return null;
			}
		}

	}
}
