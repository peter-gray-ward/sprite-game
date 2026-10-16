using App.Models;
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
		public async Task<ServiceResult> Register(string name, string password)
		{
			try
			{
				using var connection = db.GetConnection();
				await connection.OpenAsync();

				// Hash the password using BCrypt
				var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

				var command = new NpgsqlCommand(@"
					INSERT INTO Player (name, position_x, position_y, password, level_id, direction, z_index, top_left_tile, level_grid)
					VALUES (@name, 0, 0, @password, 'd2358090-8f14-4c5a-8c9e-fe59d58a4e75', '', 0, '', '')
				", connection);

				// Add parameters to prevent SQL injection
				command.Parameters.AddWithValue("@name", name);
				command.Parameters.AddWithValue("@password", hashedPassword);

				await command.ExecuteNonQueryAsync();
				
				return new ServiceResult("success");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error registering player " + ex.Message);
				return new ServiceResult("error", ex);
			}
		}

		public async Task<ServiceResult> Login(string name, string password)
		{
			Player player = new Player();
			try
			{
				Console.WriteLine("Getting connection...");
		        using var connection = db.GetConnection();

		        Console.WriteLine("Opening connection...");
		        await connection.OpenAsync();

		        Console.WriteLine("Connection opened!");


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

					Console.WriteLine("storedPassword " + storedPassword);

					Console.WriteLine("Verification " + BCrypt.Net.BCrypt.Verify(password, storedPassword));


					if (BCrypt.Net.BCrypt.Verify(password, storedPassword))
					{
						Console.WriteLine("Passwords match!");
						var token = Guid.NewGuid();

						player.access_token = token.ToString();
						player.name = reader.GetString(0);
						player.level_id = reader.GetGuid(1);
						player.position_x = reader.GetDouble(2);
						player.position_y = reader.GetDouble(3);
						player.direction = reader.GetString(5);
						player.z_index = reader.GetInt32(6);
						player.top_left_tile = reader.GetString(7);
						player.level_grid = reader.GetString(8);

						return new ServiceResult("success", player);
					}
				}

				Console.WriteLine("Login failed");

				return new ServiceResult("failure", player);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return new ServiceResult("error", ex);
			}
		}
	}
}