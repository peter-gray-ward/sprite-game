using System.Text.Json;
using App.Services;

namespace App.Controllers
{
	public static class PlayerController
	{
		public static void MapRoutes(WebApplication app)
		{
			app.MapPost("/register", async (HttpContext context, PlayerServices playerServices) =>
            {
                Console.WriteLine("    /register");
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                try
                {
                    Dictionary<string, string> credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
                    Console.WriteLine("registering", credentials);
                    string registered = await playerServices.Register(credentials["name"], credentials["password"]);
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = registered }));
                }
                catch (Exception e) 
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Error", details = e.Message }));
                }
            });

            app.MapPost("/login", async (HttpContext context, PlayerServices playerServices, SessionServices sessionServices) =>
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                try
                {
                    Dictionary<string, string> credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

                    Player player = await playerServices.Login(credentials["name"], credentials["password"]);

                    if (player.access_token != String.Empty)
                    {
                        sessionServices.Login(context, player);

                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", 
                            level_id = player.level_id,
                            position_x = player.position_x,
                            position_y = player.position_y
                        }));
                    }
                    else
                    {
                        context.Response.StatusCode = 401; // Unauthorized
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Invalid credentials" }));
                    }
                }
                catch (Exception e) 
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Error", details = e.Message }));
                    return;
                }
            });

            app.MapPost("/logout", async (HttpContext context, SessionServices sessionServices) =>
            {

                sessionServices.Logout(context);
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", message = "Logged out successfully" }));
            });
		}
	}
}