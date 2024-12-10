using System.Text.Json;
using App.Models;
using App.Services;

namespace App.Controllers
{
	public static class ImageController
	{
		public static void MapRoutes(WebApplication app)
		{
			app.MapPost("/save-image", async (HttpContext context, ImageServices imageServices) =>
            {
                string? user_name = context.Session.GetString("name");

                if (user_name is null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "User not authenticated" }));
                    return;
                }

                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

                SaveImageRequest? saveImageRequest = JsonSerializer.Deserialize<SaveImageRequest>(body);

                if (saveImageRequest is null)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Malformed request body" }));
                    return;
                }

                ServiceResult saved = await imageServices.SaveImage(saveImageRequest.url, saveImageRequest.tag, user_name);

                if (saved.exception is not null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = saved.exception.Message }));
                    return;
                }

                context.Response.StatusCode = 201;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", id = saved.data }));
            });

            app.MapGet("/get-image-ids", async (HttpContext context, ImageServices imageServices) =>
            {
                string? user_name = context.Session.GetString("name");

                if (user_name is null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "User not authenticated" }));
                    return;
                }

                Console.WriteLine("Getting image ids for " + user_name);

                ServiceResult imageIdResult = await imageServices.GetImageIds(user_name);

                if (imageIdResult.exception is not null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = imageIdResult.exception.Message }));
                    return;
                }

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { 
                    status = "success", 
                    data = imageIdResult.data 
                }));
            });

            app.MapGet("/get-image/{imageId}", async (HttpContext context, ImageServices imageServices) =>
            {
                string? user_name = context.Session.GetString("name");

                if (user_name is null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "User not authenticated" }));
                    return;
                }

                string imageId = context.Request.RouteValues["imageId"]!.ToString() ?? String.Empty;

                ServiceResult imageResult = await imageServices.GetImage(user_name, imageId);

                if (imageResult.exception is not null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = imageResult.exception.Message }));
                    return;
                }
                
                if (imageResult.data is not byte[] imageData)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Image data not found" }));
                    return;
                }
                context.Response.ContentType = "image/png";
                await context.Response.Body.WriteAsync(imageData, 0, imageData.Length);
            });
		}
	}
}