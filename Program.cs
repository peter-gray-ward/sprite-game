using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Data;
using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using App;

bool editSchema = false;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache(); // Use in-memory cache for session storage
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true; // Secure against XSS
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Secure in production
    options.Cookie.SameSite = SameSiteMode.Lax; // Lax or Strict depending on your needs
    options.Cookie.Name = ".sprite-game.Session"; // Custom cookie name
    options.IdleTimeout = TimeSpan.FromMinutes(20); // Session timeout
});

// Database connection string
string connectionString = builder.Configuration.GetConnectionString("CloudConnection");

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.UseMiddleware<AuthenticationMiddleware>(connectionString);

// Serve index.html at the root URL
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});


if (editSchema)
{
    Console.WriteLine("connection string: " + connectionString);
	EditSchema es = new EditSchema(connectionString);
	es.Run();
}

app.MapPost("/register", async context =>
{
    Console.WriteLine("    /register");
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    try
    {
        Dictionary<string, string> credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        Console.WriteLine("registering", credentials);
        Player player = new Player();
        string registered = await player.Register(connectionString, credentials["name"], credentials["password"]);
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = registered }));
    }
    catch (Exception e) 
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Error", details = e.Message }));
        return;
    }
});

app.MapPost("/login", async context =>
{
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    try
    {
        Dictionary<string, string> credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        Player player = new Player();
        await player.Login(connectionString, credentials["name"], credentials["password"]);

        if (player.access_token != String.Empty)
        {
            
            context.Session.SetString("access_token", player.access_token);
            context.Session.SetString("name", credentials["name"]);

            context.Response.Cookies.Append("name", credentials["name"], new CookieOptions
            {
                HttpOnly = false, // Accessible via JavaScript if needed
                Secure = false,
                SameSite = SameSiteMode.Lax
            });

            context.Response.Cookies.Append("level", player.level.ToString(), new CookieOptions
            {
                HttpOnly = false, // Accessible via JavaScript if needed
                Secure = false,
                SameSite = SameSiteMode.Lax
            });

            context.Response.Cookies.Append("position_x", player.position_x.ToString(), new CookieOptions
            {
                HttpOnly = false, // Accessible via JavaScript if needed
                Secure = false,
                SameSite = SameSiteMode.Lax
            });

            context.Response.Cookies.Append("position_y", player.position_y.ToString(), new CookieOptions
            {
                HttpOnly = false, // Accessible via JavaScript if needed
                Secure = false,
                SameSite = SameSiteMode.Lax
            });

            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", 
                level = player.level,
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

app.MapPost("/logout", async context =>
{

    Console.WriteLine("logging-out");
    // Clear session data
    context.Session.Clear();

    // Remove cookies (if applicable)
    context.Response.Cookies.Delete("name");
    context.Response.Cookies.Delete(".sprite-game.Session"); // Replace with your actual session cookie name if different


    Console.WriteLine("logged-out");
    await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", message = "Logged out successfully" }));
});


// API endpoint for index
app.MapGet("/api/", async context =>
{
    var data = new Dictionary<string, object>();
    if (context.User.Identity.IsAuthenticated)
    {
        var user = context.User; // Get user info
        data["user"] = user.Claims.ToDictionary(c => c.Type, c => c.Value); // Add user info to data
    }
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(new { data }));
});


// Save image endpoint
app.MapPost("/save-image", async context =>
{
    string user_name = context.Session.GetString("name");

    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

    // Deserialize the request body into a Dictionary
    Dictionary<string, object> requestData;
    try
    {
        requestData = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
    }
    catch (JsonException ex)
    {
        context.Response.StatusCode = 400; // Bad Request
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Invalid JSON format", details = ex.Message }));
        return;
    }

    // Extract URL and Tag from the dictionary
    if (!requestData.TryGetValue("url", out var urlObj) || !requestData.TryGetValue("tag", out var tagObj))
    {
        context.Response.StatusCode = 400; // Bad Request
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Missing 'url' or 'tag' in the request" }));
        return;
    }

    var url = urlObj.ToString();
    var tag = tagObj.ToString();

    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var imageData = await response.Content.ReadAsByteArrayAsync();
        var imageId = Guid.NewGuid();

        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand("INSERT INTO public.images (id, tag, src, user_name) VALUES (@id, @tag, @src, @user_name)", connection);
            command.Parameters.AddWithValue("id", imageId);
            command.Parameters.AddWithValue("tag", tag);
            command.Parameters.AddWithValue("src", imageData);
            command.Parameters.AddWithValue("user_name", user_name);
            await command.ExecuteNonQueryAsync();
        }

        context.Response.StatusCode = 201;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", id = imageId }));
    }
    catch (Exception e)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
    }
});

// Get image IDs endpoint
app.MapGet("/get-image-ids", async context =>
{
    string user_name = context.Session.GetString("name");

    Console.WriteLine("Getting image ids for " + user_name);

    try
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand("SELECT id, tag FROM public.images WHERE user_name = @user_name GROUP BY tag, id", connection);
            command.Parameters.AddWithValue("user_name", user_name);
            var reader = await command.ExecuteReaderAsync();

            var images = new List<object>();
            while (await reader.ReadAsync())
            {
                images.Add(new { id = reader["id"], tag = reader["tag"] });
            }
            await reader.CloseAsync();
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", data = images }));
        }
    }
    catch (Exception e)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
    }
});

// Get image by ID endpoint
app.MapGet("/get-image/{imageId}", async context =>
{
    string user_name = context.Session.GetString("name");

    var imageId = context.Request.RouteValues["imageId"].ToString();
    if (imageId is null)
    {
    	await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "invalid image id" }));
        return;
    }
    try
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand("SELECT src FROM public.images WHERE user_name = @user_name AND id = @imageId", connection);
            command.Parameters.AddWithValue("user_name", user_name);
            command.Parameters.AddWithValue("imageId", Guid.Parse(imageId));
            var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var imageData = (byte[])reader["src"];

                await reader.CloseAsync();

                context.Response.ContentType = "image/png";
                await context.Response.Body.WriteAsync(imageData, 0, imageData.Length);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "Image not found" }));
            }
        }
    }
    catch (Exception e)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
    }
});

app.MapPost("/save-blocks/{levelId}/{imageId}", async context =>
{
    string user_name = context.Session.GetString("name");

	try
	{
		string imageId = context.Request.RouteValues["imageId"].ToString();
		int levelId = Convert.ToInt32(context.Request.RouteValues["levelId"]);
		var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
		List<Dictionary<string, List<int>>> drop_block_ids = JsonSerializer.Deserialize<List<Dictionary<string, List<int>>>>(body);
		using (var connection = new NpgsqlConnection(connectionString))
		{
			await connection.OpenAsync();
            string message = "";
            var recurrence_id = Guid.NewGuid();
			foreach (Dictionary<string, List<int>> block in drop_block_ids)
			{
				var command = new NpgsqlCommand(@$"
					INSERT INTO public.Block
					(id, user_name, level_id, image_id, start_x, start_y, end_x, end_y, dimension, recurrence_id)
					VALUES
					(@id, @user_name, @level_id, @image_id, @start_x, @start_y, @end_x, @end_y, @dimension, @recurrence_id)
				", connection);
				command.Parameters.AddWithValue("id", Guid.NewGuid());
                command.Parameters.AddWithValue("user_name", user_name);
				command.Parameters.AddWithValue("level_id", levelId);
				command.Parameters.AddWithValue("image_id", Guid.Parse(imageId));
				command.Parameters.AddWithValue("start_y", block["start"][0]);
				command.Parameters.AddWithValue("start_x", block["start"][1]);
				command.Parameters.AddWithValue("end_y", block["end"][0]);
				command.Parameters.AddWithValue("end_x", block["end"][1]);
                command.Parameters.AddWithValue("dimension", block["dimension"][0]);
                command.Parameters.AddWithValue("recurrence_id", recurrence_id);
				await command.ExecuteNonQueryAsync();
                message += $"Saved block {block["start"][0]}, {block["start"][1]}\n";
			}
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", message = message }));
		}
	}
	catch (Exception e)
	{
		context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
	}
});

app.MapPost("/update-block-style/{recurrence_id}", async context =>
{
    string username = context.Session.GetString("name");
    try
    {
        string recurrence_id = context.Request.RouteValues["recurrence_id"].ToString();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Dictionary<string, JsonElement> block = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@$"
                UPDATE public.Block
                SET
                    dimension = @dimension,
                    css = @css
                WHERE recurrence_id = @recurrence_id
            ", connection);
            command.Parameters.AddWithValue("recurrence_id", Guid.Parse(recurrence_id));
            command.Parameters.AddWithValue("dimension", block["dimension"].GetInt32());
            command.Parameters.AddWithValue("css", block["css"].GetString());
            await command.ExecuteNonQueryAsync();
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success" }));
        }
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = ex.Message }));
    }
});

app.MapPost("/update-block-object-area/{recurrence_id}", async context => {
    string username = context.Session.GetString("name");
    try
    {
        string recurrence_id = context.Request.RouteValues["recurrence_id"].ToString();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Dictionary<string, JsonElement> block = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@$"
                UPDATE public.Block
                SET
                    object_area = @object_area
                WHERE recurrence_id = @recurrence_id
            ", connection);
            command.Parameters.AddWithValue("recurrence_id", Guid.Parse(recurrence_id));
            command.Parameters.AddWithValue("object_area", block["object_area"].GetString());
            await command.ExecuteNonQueryAsync();
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", data = block["object_area"].GetString()}));
        }
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = ex.Message }));
    }
});

app.MapGet("/get-blocks/{level_id}", async context =>
{
    string username = context.Session.GetString("name");

    try 
    {
        int levelId = Convert.ToInt32(context.Request.RouteValues["level_id"]);
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@$"
                SELECT *
                FROM public.Block
                WHERE user_name = @user_name
                AND level_id = @level_id
            ", connection);

            // Add parameters
            command.Parameters.AddWithValue("user_name", username);
            command.Parameters.AddWithValue("level_id", levelId);

            var reader = await command.ExecuteReaderAsync();

            List<Dictionary<string, object>> blocks = new();

            while (await reader.ReadAsync())
            {
                Dictionary<string, object> block = new Dictionary<string, object>();
                
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string fieldName = reader.GetName(i);
                    object fieldValue = reader[fieldName];
                    block[fieldName] = fieldValue;
                }

                blocks.Add(block);
            }

            await reader.CloseAsync();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", data = blocks }));

        }
    }
    catch (Exception e)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
    }
});


app.Run();
