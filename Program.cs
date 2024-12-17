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
string? connectionString = builder.Configuration.GetConnectionString("CloudConnection");

if (connectionString is null || connectionString == String.Empty)
{
    throw new Exception("Missing Connection String!");
}

builder.Services.AddSingleton<DatabaseServices>(provider => new DatabaseServices(connectionString));
builder.Services.AddScoped<SessionServices>();
builder.Services.AddScoped<LevelServices>();
builder.Services.AddScoped<PlayerServices>();
builder.Services.AddScoped<ImageServices>();
builder.Services.AddScoped<BlockServices>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseMiddleware<AuthenticationMiddleware>();
app.MapControllers();


if (editSchema)
{
    Console.WriteLine("connection string: " + connectionString);
	EditSchema es = new EditSchema(connectionString);
	es.Run();
}


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
