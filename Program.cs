using System.Text.Json;
using System.Data;
using System.Text;
using Npgsql;
using App;
using App.Services;
using App.Middleware;
using App.Controllers;

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

builder.Services.AddSingleton<DatabaseServices>(provider => new DatabaseServices(connectionString));
builder.Services.AddScoped<SessionServices>();
builder.Services.AddScoped<LevelServices>();
builder.Services.AddScoped<PlayerServices>();

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.UseMiddleware<AuthenticationMiddleware>();

HomeController.MapRoutes(app);
PlayerController.MapRoutes(app);
LevelController.MapRoutes(app);

if (editSchema)
{
    Console.WriteLine("connection string: " + connectionString);
	EditSchema es = new EditSchema(connectionString);
	es.Run();
}


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
		string levelId = context.Request.RouteValues["levelId"].ToString();
		var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
		Dictionary<string, JsonElement> drop_area = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
		using (var connection = new NpgsqlConnection(connectionString))
		{
			await connection.OpenAsync();

            var id = Guid.NewGuid();
            var recurrence_id = Guid.NewGuid();
            int x_dir = drop_area["xDir"].GetInt32();
            int y_dir = drop_area["yDir"].GetInt32();

			var command = new NpgsqlCommand(@$"
				INSERT INTO public.Block
				(
                    id,
                    user_name,
                    level_id,
                    level_grid,
                    image_id,
                    start_x,
                    start_y,
                    repeat_y,
                    repeat_x,
                    dir_y,
                    dir_x,
                    dimension,
                    recurrence_id
                )
				VALUES
				(
                    @id,
                    @user_name,
                    @level_id,
                    @level_grid,
                    @image_id,
                    @start_x,
                    @start_y,
                    @repeat_y,
                    @repeat_x,
                    @dir_y,
                    @dir_x,
                    @dimension,
                    @recurrence_id
                )
			", connection);

			command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("user_name", user_name);
			command.Parameters.AddWithValue("level_id", Guid.Parse(levelId));
            command.Parameters.AddWithValue("level_grid", drop_area["level_grid"].GetString());
			command.Parameters.AddWithValue("image_id", Guid.Parse(imageId));
			command.Parameters.AddWithValue("start_y", drop_area["start_y"].GetInt32());
			command.Parameters.AddWithValue("start_x", drop_area["start_x"].GetInt32());
			command.Parameters.AddWithValue("repeat_y", drop_area["yRepeat"].GetInt32());
			command.Parameters.AddWithValue("repeat_x", drop_area["xRepeat"].GetInt32());
            command.Parameters.AddWithValue("dir_y", x_dir / Math.Abs(x_dir));
            command.Parameters.AddWithValue("dir_x", y_dir / Math.Abs(y_dir));
            command.Parameters.AddWithValue("dimension", drop_area["dimensions"].GetInt32());
            command.Parameters.AddWithValue("recurrence_id", recurrence_id);

			await command.ExecuteNonQueryAsync();

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success", message = id.ToString() }));
		}
	}
	catch (Exception e)
	{
		context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
	}
});

app.MapDelete("/delete-block/{recurrence_id}", async context =>
{
    try
    {
        string recurrence_id = context.Request.RouteValues["recurrence_id"].ToString();
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@$"
                DELETE FROM public.Block
                WHERE recurrence_id = @recurrence_id
            ", connection);
            command.Parameters.AddWithValue("recurrence_id", Guid.Parse(recurrence_id));
            await command.ExecuteNonQueryAsync();
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "success" }));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Exception: " + ex.Message);
        Console.WriteLine("Stack Trace: " + ex.StackTrace);
        if (ex.InnerException != null)
        {
            Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
        }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = ex.Message }));
    }
});

app.MapPost("/update-block/{recurrence_id}", async context =>
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
                    start_y = @start_y,
                    start_x = @start_x,
                    repeat_y = @repeat_y,
                    repeat_x = @repeat_x,
                    dir_y = @dir_y,
                    dir_x = @dir_x,
                    translate_object_area = @translate_object_area,
                    random_rotation = @random_rotation,
                    ground = @ground,
                    parent_id = @parent_id,
                    level_id = @level_id,
                    level_grid = @level_grid,
                    css = @css
                WHERE recurrence_id = @recurrence_id
            ", connection);
            command.Parameters.AddWithValue("recurrence_id", Guid.Parse(recurrence_id));
            command.Parameters.AddWithValue("dimension", block["dimension"].GetInt32());
            command.Parameters.AddWithValue("start_y", block["start_y"].GetInt32());
            command.Parameters.AddWithValue("start_x", block["start_x"].GetInt32());
            command.Parameters.AddWithValue("repeat_y", block["repeat_y"].GetInt32());
            command.Parameters.AddWithValue("repeat_x", block["repeat_x"].GetInt32());
            command.Parameters.AddWithValue("dir_y", block["dir_y"].GetInt32());
            command.Parameters.AddWithValue("dir_x", block["dir_x"].GetInt32());
            command.Parameters.AddWithValue("translate_object_area", block["translate_object_area"].GetInt32());
            command.Parameters.AddWithValue("random_rotation", block["random_rotation"].GetInt32());
            command.Parameters.AddWithValue("level_id", Guid.Parse(block["level_id"].GetString()));
            command.Parameters.AddWithValue("level_grid", block["level_grid"].GetString());
            command.Parameters.AddWithValue("ground", block["ground"].GetInt32());
            string? parent_id = block["parent_id"].GetString();
            command.Parameters.AddWithValue(
                "parent_id",
                string.IsNullOrWhiteSpace(parent_id) ? (object)DBNull.Value : Guid.Parse(parent_id)
            );
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
        string levelId = context.Request.RouteValues["level_id"].ToString();
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
            command.Parameters.AddWithValue("level_id", Guid.Parse(levelId));

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

app.MapGet("/wall-bump-sound.mp3", async context => {
    try
    {
        context.Response.ContentType = "audio/mp3";
        using (FileStream stream = new FileStream("wwwroot/wall-bump-sound.mp3", FileMode.Open, FileAccess.Read))
        {
            await stream.CopyToAsync(context.Response.Body);
        }
    }
    catch (Exception e)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "error", message = e.Message }));
    }
});


app.Run();
