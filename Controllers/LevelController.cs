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
                string? user_name = context.Session.GetString("name");

                if (user_name == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                        status = "error", 
                        message = "User name is required." 
                    }));
                    return;
                }

                try
                {
                    string? level_id = context.Request.RouteValues["levelId"]?.ToString();
                    if (level_id == null)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                            status = "error", 
                            message = "Level ID is required." 
                        }));
                        return;
                    }
                    ServiceResult level = await levelServices.GetLevel(user_name, level_id);
                        
                    if (level.exception is not null)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = level.status, message = level.exception.Message }));
                        return;
                    }

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                        status = "success", 
                        data = level.data
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
                string? user_name = context.Session.GetString("name");

                if (user_name == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                        status = "error", 
                        message = "User name is required." 
                    }));
                    return;
                }

                try
                {
                    string? level_id = context.Request.RouteValues["levelId"]?.ToString();
                    if (level_id == null)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                            status = "error", 
                            message = "Level ID is required." 
                        }));
                        return;
                    }
                    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    if (string.IsNullOrEmpty(body))
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                            status = "error", 
                            message = "Request body cannot be empty." 
                        }));
                        return;
                    }
                    Dictionary<string, JsonElement>? level = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                    
                    if (level == null)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                            status = "error", 
                            message = "Invalid request body." 
                        }));
                        return;
                    }
                    ServiceResult edited = await levelServices.EditLevel(user_name, level);
                        
                    if (edited.exception is not null) 
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                            status = "error",
                            message = edited.exception.Message
                        }));

                        return;
                    }

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                        status = edited.status, 
                        data = edited.data 
                    }));
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