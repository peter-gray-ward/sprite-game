namespace App.Controllers
{
	public static class HomeController
	{
		public static void MapRoutes(WebApplication app)
		{
			app.MapGet("/", async context =>
			{
			    context.Response.ContentType = "text/html";
			    await context.Response.SendFileAsync("wwwroot/index.html");
			});
		}
	}
}