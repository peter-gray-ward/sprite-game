using Npgsql;
using BCrypt.Net;

namespace App
{
	public class Player
	{
		public string access_token { get; set; } = String.Empty;
		public int level { get; set; } = 0;
		public double position_x { get; set; } = 0.0;
		public double position_y { get; set; } = 0.0;
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

		public async Task<bool> Login(string connectionString, string name, string password)
		{
			try
			{
				using (var connection = new NpgsqlConnection(connectionString))
				{
					await connection.OpenAsync();

					var command = new NpgsqlCommand(@"
						SELECT name, level, position_x, position_y, password FROM Player WHERE name = @name
					", connection);

					command.Parameters.AddWithValue("@name", name);

					var reader = await command.ExecuteReaderAsync();

					if (await reader.ReadAsync())
					{
						var storedPassword = reader.GetString(4);

						if (BCrypt.Net.BCrypt.Verify(password, storedPassword))
						{
							var token = Guid.NewGuid();

							this.access_token = token.ToString();

							this.level = reader.GetInt32(1);
							this.position_x = reader.GetDouble(3);
							this.position_y = reader.GetDouble(3);

							return true;
						}
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during login: {ex.Message}");
				return false;
			}
		}

	}
}
