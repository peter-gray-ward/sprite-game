using System.Reflection;
using App.Models;

namespace App.Services
{
	public class SessionServices
	{
		private DatabaseServices db;
		public SessionServices(DatabaseServices db)
		{
			this.db = db;
		}
		public void Login(HttpContext context, Player player)
		{
			context.Session.SetString("access_token", player.access_token);
            context.Session.SetString("name", player.name);
		    context.Session.SetString("level_id", player.level_id.ToString());
		    context.Session.SetInt32("z_index", player.z_index);
		    context.Session.SetString("direction", player.direction.ToString());
		    context.Session.SetString("position_x", player.position_x.ToString());
		    context.Session.SetString("position_y", player.position_y.ToString());
		    context.Session.SetString("level_grid", player.level_grid.ToString());

		    Console.WriteLine(player.level_grid.ToString());

            foreach (PropertyInfo prop in player.GetType().GetProperties())
            {
                context.Response.Cookies.Append(prop.Name, prop.GetValue(player)?.ToString() ?? string.Empty, new CookieOptions
                {
                    HttpOnly = false, // Accessible via JavaScript if needed
                    Secure = false,
                    SameSite = SameSiteMode.Lax
                });
            }
		}
		public void Logout(HttpContext context)
		{
            context.Session.Clear();
            
            context.Response.Cookies.Delete("name");
            context.Response.Cookies.Delete(".sprite-game.Session");
		}

		public void UpdateSession(HttpContext context, Player player) 
		{
			context.Session.SetString("access_token", player.access_token);
			context.Session.SetString("name", player.name);
			context.Session.SetString("level_id", player.level_id.ToString());
			context.Session.SetInt32("z_index", player.z_index);
			context.Session.SetString("direction", player.direction.ToString());
			context.Session.SetString("position_x", player.position_x.ToString());
			context.Session.SetString("position_y", player.position_y.ToString());
			context.Session.SetString("level_grid", player.level_grid.ToString());
		}

		public void SetLevel(HttpContext context, string levelId)
		{
			context.Session.SetString("level_id", levelId);
		}
	}
}