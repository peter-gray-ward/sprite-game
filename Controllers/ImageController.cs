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

                // Deserialize the request body into a Dictionary
                Dictionary<string, object>? requestData;
                try
                {
                    requestData = JsonSerializer.Deserialize<Dictionary<string, object>?>(body);
                }
                catch (JsonException ex)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Invalid JSON format", details = ex.Message }));
                    return;
                }

                if (requestData is null)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Invalid JSON format"  }));
                    return;
                }

                // Check if keys exist and retrieve values safely
                if (!requestData.TryGetValue("url", out var urlObj) || urlObj is null)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "URL cannot be null" }));
                    return;
                }
                var url = urlObj.ToString();

                if (!requestData.TryGetValue("tag", out var tagObj) || tagObj is null)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Tag cannot be null" }));
                    return;
                }
                var tag = tagObj.ToString();

                // Check for null values before calling SaveImage
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(tag))
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "URL and tag cannot be null or empty" }));
                    return;
                }

                ServiceResult saved = await imageServices.SaveImage(url, tag, user_name);

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

                var imageIdObj = context.Request.RouteValues["imageId"];
                if (imageIdObj is null || imageIdObj.ToString() is null)
                {
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "invalid image id" }));
                    return;
                }
                var imageId = imageIdObj?.ToString();

                // Check if imageId is null before calling GetImage
                if (string.IsNullOrEmpty(imageId))
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Invalid image id" }));
                    return;
                }

                ServiceResult imageResult = await imageServices.GetImage(user_name, imageId);

                if (imageResult.exception is not null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = imageResult.exception.Message }));
                    return;
                }

                // Check if imageResult.data is null before casting
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