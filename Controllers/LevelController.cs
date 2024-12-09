using System.Text.Json;
using App.Models;
using App.Services;

namespace App.Controllers
{
	public static class LevelController
	{
		public static void MapRoutes(WebApplication app)
		{
			app.MapGet("/level/{levelId}", async (HttpContext context, LevelServices levelServices) =>
            {
                string user_name = context.Session.GetString("name");

                try
                {
                    string level_id = context.Request.RouteValues["levelId"].ToString();
                    Level level = await levelServices.GetLevel(user_name, level_id);
                        
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                        status = "success", 
                        data = level 
                    }));
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
                }
            });

            app.MapPost("/level/{levelId}", async (HttpContext context, LevelServices levelServices) =>
            {
                string user_name = context.Session.GetString("name");

                try
                {
                    string level_id = context.Request.RouteValues["levelId"].ToString();
                    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    Dictionary<string, JsonElement> level = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                    
                    bool edited = await levelServices.EditLevel(user_name, level);
                        
                    if (edited == true) 
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                            status = "success", 
                            data = level 
                        }));
                    }
                    else
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                            status = "error"
                        }));
                    }
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
                }
            });
		}
	}
}