using Npgsql; // Ensure you have this package installed

namespace App 
{
	public class EditSchema
	{
		private string connectionString { get; set; } = String.Empty;
		public EditSchema(string connectionString)
		{
			this.connectionString = connectionString;
		}
		public async void Run()
		{
			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();
				var command = new NpgsqlCommand(@$"
					DROP TABLE IF EXISTS Block;
					CREATE TABLE Block (
						id UUID PRIMARY KEY,
						recurrence_id UUID,
						user_name TEXT NOT NULL,
						start_y INT NOT NULL,
						start_x INT NOT NULL,
						end_y INT NOT NULL,
						end_x INT NOT NULL,
						level_id INT NOT NULL,
						image_id UUID,
						css TEXT,
						dimension INT,
						object_area TEXT
					);

					DROP TABLE IF EXISTS Player;
					CREATE TABLE Player (
						name TEXT PRIMARY KEY NOT NULL,
						password TEXT NOT NULL,
						level INT NOT NULL,
						position_x FLOAT,
						position_y FLOAT
					);

					DROP TABLE IF EXISTS Item;
					CREATE TABLE Item (
						id UUID PRIMARY KEY,
						name TEXT,
						image_id UUID,
						width FLOAT,
						height FLOAT
					);

					DROP TABLE IF EXISTS PlayerInventory;
					CREATE TABLE PlayerInventory (
						id UUID PRIMARY KEY,
						user_name TEXT NOT NULL,
						item_id UUID NOT NULL
					);
				", connection);
				await command.ExecuteNonQueryAsync();

				Console.WriteLine("schema edited!");
			}
		}
	}
}