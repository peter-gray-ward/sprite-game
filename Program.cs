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
using Npgsql; // Ensure you have this package installed

var userId = "6f7b8307-5130-4dbf-ba8e-e54a855a5357";
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles();

// Serve index.html at the root URL
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

// Database connection string
string connectionString = "Host=localhost;Username=peter;Password=enter123";

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
    //var userId = context.Session.GetString("user_id");
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

        Console.WriteLine(imageId.ToString(), Guid.Parse(userId).ToString());

        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand("INSERT INTO public.images (id, tag, src, user_id) VALUES (@id, @tag, @src, @userId)", connection);
            command.Parameters.AddWithValue("id", imageId);
            command.Parameters.AddWithValue("tag", tag);
            command.Parameters.AddWithValue("src", imageData);
            command.Parameters.AddWithValue("userId", Guid.Parse(userId));
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
    //var userId = context.Session.GetString("user_id");
    try
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand("SELECT id, tag FROM public.images WHERE user_id = @userId GROUP BY tag, id", connection);
            command.Parameters.AddWithValue("userId", Guid.Parse(userId));
            var reader = await command.ExecuteReaderAsync();

            var images = new List<object>();
            while (await reader.ReadAsync())
            {
                images.Add(new { id = reader["id"], tag = reader["tag"] });
            }

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
    //var userId = context.Session.GetString("user_id");
    var imageId = context.Request.RouteValues["imageId"].ToString();
    if (imageId is null)
    {
    	await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = "invalid image id" }));
    }
    try
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand("SELECT src FROM public.images WHERE user_id = @userId AND id = @imageId", connection);
            command.Parameters.AddWithValue("userId", Guid.Parse(userId));
            command.Parameters.AddWithValue("imageId", Guid.Parse(imageId));
            var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var imageData = (byte[])reader["src"];
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

app.Run();
