using Npgsql; // Ensure you have this package installed

namespace App 
{
	public class EditSchema
	{
		private string connectionString;
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
	            	CREATE TABLE IF NOT EXISTS Block (
					    id UUID PRIMARY KEY,
					    start_y INT NOT NULL,
					    start_x INT NOT NULL,
					    end_y INT NOT NULL,
					    end_x INT NOT NULL,
					    image_id UUID,
					    background_size TEXT,
					    background_repeat TEXT,
					    translateX INT,
					    translateY INT
					);
	            ", connection);
	            await command.ExecuteNonQueryAsync();
	        }
		}
	}
}