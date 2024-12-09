using System.Text.Json;
using App.Models;
using App.Services;

namespace App.Controllers
{
	public static class BlockController
	{
		public static void MapRoutes(WebApplication app)
		{
			
			app.MapPost("/save-blocks/{levelId}/{imageId}", async (HttpContext context, BlockServices blockServices) =>
			{
				string user_name = context.Session.GetString("name") ?? String.Empty;
				string imageId = context.Request.RouteValues["imageId"]!.ToString() ?? String.Empty;
				string levelId = context.Request.RouteValues["levelId"]!.ToString() ?? String.Empty;

				var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
				Block? drop_area = JsonSerializer.Deserialize<Block>(body);

				if (drop_area is null)
				{
					context.Response.StatusCode = 400;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Malformed drop area" }));
					return;
				}
				
				
				ServiceResult saved = await blockServices.SaveBlock(user_name, imageId, levelId, drop_area);

				if (saved.exception is not null)
				{
					context.Response.StatusCode = 500;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = saved.status, message = saved.exception }));
					return;
				}
				
				context.Response.StatusCode = 200;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = saved.status, message = saved.data }));
			});

			app.MapDelete("/delete-block/{recurrence_id}", async (HttpContext context, BlockServices blockServices) =>
			{
				string recurrence_id = context.Request.RouteValues["recurrence_id"]!.ToString() ?? String.Empty;

				ServiceResult deletion = await blockServices.DeleteBlock(recurrence_id);
				if (deletion.exception is not null)
				{
					context.Response.StatusCode = 500;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = deletion.status, message = deletion.exception.Message }));
					return;
				}

				context.Response.StatusCode = 200;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = deletion.status }));
			});

			app.MapPost("/update-block/{recurrence_id}", async (HttpContext context, BlockServices blockServices) =>
			{
				try
				{
					string username = context.Session.GetString("name") ?? String.Empty;
					string recurrence_id = context.Request.RouteValues["recurrence_id"]?.ToString() ?? String.Empty;
					
					Console.WriteLine(2);
					var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
					
					Block? block = JsonSerializer.Deserialize<Block>(body);
					
					if (block is null)
					{
						context.Response.StatusCode = 400;
						await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Malformed block" }));
						return;
					}

					ServiceResult update = await blockServices.UpdateBlock(username, recurrence_id, block);

					if (update.exception is not null)
					{
						context.Response.StatusCode = 500;
						await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = update.status, message = update.exception.Message }));
						return;
					}

					context.Response.StatusCode = 200;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = update.status }));
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = ex.Message}));
					return;
				}
			});

			app.MapPost("/update-block-object-area/{recurrence_id}", async (HttpContext context, BlockServices blockServices) => {
				try
				{
					string username = context.Session.GetString("name") ?? String.Empty;
					string recurrence_id = context.Request.RouteValues["recurrence_id"]!.ToString() ?? String.Empty;
					var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
					Block? block = JsonSerializer.Deserialize<Block>(body);
					
					if (block is null)
					{
						context.Response.StatusCode = 400;
						await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Malformed drop area" }));
						return;
					}

					ServiceResult update = await blockServices.UpdateObjectArea(recurrence_id, block);
					if (update.exception is not null)
					{
						context.Response.StatusCode = 500;
						await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = update.status, message = update.exception.Message }));
						return;
					}

					context.Response.StatusCode = 200;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = update.status, data = block.object_area}));
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = ex.Message}));
					return;
				}
			});

			app.MapGet("/get-blocks/{level_id}", async (HttpContext context, BlockServices blockServices) =>
			{
				try
				{
					string username = context.Session.GetString("name") ?? String.Empty;
					string levelId = context.Request.RouteValues["level_id"]!.ToString() ?? String.Empty;
					
					ServiceResult blocks = await blockServices.GetBlocks(username, levelId);

					if (blocks.exception is not null)
					{
						context.Response.StatusCode = 500;
						await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = blocks.exception.Message }));
						return;
					}

					context.Response.StatusCode = 200;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = blocks.status, data = blocks.data }));
				}
				catch (Exception ex)
				{
					context.Response.StatusCode = 500;
					await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = ex.Message}));
					return;
				}
			});

		}
	}
}