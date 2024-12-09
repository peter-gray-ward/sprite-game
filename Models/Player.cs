using Npgsql;
using BCrypt.Net;

namespace App.Models
{
	public class Player
	{
		public string access_token { get; set; } = String.Empty;
		public string password { get; set; } = String.Empty;
		public string name { get; set; } = "";
		public Guid level_id { get; set; }
		public double position_x { get; set; } = 0.0;
		public double position_y { get; set; } = 0.0;
		public string direction { get; set; } = "";
		public int z_index { get; set; } = 0;
		public string top_left_tile { get; set; } = "";
		public string level_grid { get; set; } = "";
	}
}
