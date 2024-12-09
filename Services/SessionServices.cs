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
	}
}