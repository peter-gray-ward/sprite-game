using System.Text.Json;
using App.Models;
using App.Services;

namespace App.Controllers
{
	public static class PlayerController
	{
		public static void MapRoutes(WebApplication app)
		{
			app.MapPost("/register", async (HttpContext context, PlayerServices playerServices) =>
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                try
                {
                    Player? player = JsonSerializer.Deserialize<Player>(body);
                    if (player is null)
                    {
                        throw new Exception("Invalid credentials");
                    }
                    ServiceResult registered = await playerServices.Register(player.name, player.password);
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = registered.status }));
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
                    Player? playerLoggingIn = JsonSerializer.Deserialize<Player>(body);

                    if (playerLoggingIn is null)
                    {
                        throw new Exception("Invalid login credentials");
                    }

                    ServiceResult playerResult = await playerServices.Login(playerLoggingIn.name, playerLoggingIn.password);
                    Player? player = playerResult.data as Player;

                    if (player is null || player.access_token == String.Empty)
                    {
                        throw new Exception("Invalid login credentials");
                    }

                    sessionServices.Login(context, player);

                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", 
                        level_id = player.level_id,
                        position_x = player.position_x,
                        position_y = player.position_y
                    }));
                }
                catch (Exception e) 
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
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