using Npgsql;

namespace App.Services
{
	public class PlayerServices
	{
		private DatabaseServices db;
		public PlayerServices(DatabaseServices db)
		{
			this.db = db;
		}
		public async Task<string> Register(string name, string password)
		{
			try
			{
				var connection = db.GetConnection();
				await connection.OpenAsync();

				// Hash the password using BCrypt
				var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

				var command = new NpgsqlCommand(@"
					INSERT INTO Player (name, position_x, position_y, password)
					VALUES (@name, 0, 0, @password)
				", connection);

				// Add parameters to prevent SQL injection
				command.Parameters.AddWithValue("@name", name);
				command.Parameters.AddWithValue("@password", hashedPassword);

				await command.ExecuteNonQueryAsync();
				
				return "success";
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during registration: {ex.Message}");
				return "error";
			}
		}

		public async Task<Player> Login(string name, string password)
		{
			Player player = new Player();
			try
			{
				var connection = db.GetConnection();
				await connection.OpenAsync();

				var command = new NpgsqlCommand(@"
					SELECT 
						name, 
						level_id, 
						position_x, 
						position_y, 
						password, 
						direction, 
						z_index,
						top_left_tile,
						level_grid
					FROM Player WHERE name = @name
				", connection);

				command.Parameters.AddWithValue("@name", name);

				var reader = await command.ExecuteReaderAsync();

				if (await reader.ReadAsync())
				{
					var storedPassword = reader.GetString(4);

					if (BCrypt.Net.BCrypt.Verify(password, storedPassword))
					{
						Console.WriteLine("Passwords match!");
						var token = Guid.NewGuid();

						player.access_token = token.ToString();
						player.name = reader.GetString(0);
						player.level_id = reader.GetGuid(1).ToString();
						player.position_x = reader.GetDouble(2);
						player.position_y = reader.GetDouble(3);
						player.direction = reader.GetString(5);
						player.z_index = reader.GetInt32(6);
						player.top_left_tile = reader.GetString(7);
						player.level_grid = reader.GetString(8);

						return player;
					}
					else
					{
						Console.WriteLine("Passwords do not match!");
					}
				}

				return player;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during login: {ex.Message}");
				return player;
			}
		}
	}
}