namespace App
{
	public class AuthenticationMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly string _connectionString;

		public AuthenticationMiddleware(RequestDelegate next, string connectionString)
		{
			this._next = next;
			this._connectionString = connectionString;

			Console.WriteLine("AuthenticationMiddleware._connectionString: " + this._connectionString);
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				if (context.Request.Path.Equals("/register", StringComparison.OrdinalIgnoreCase) ||
		        	context.Request.Path.Equals("/login", StringComparison.OrdinalIgnoreCase) ||
		        	context.Request.Path.Equals("/logout", StringComparison.OrdinalIgnoreCase))
			    {
			        await _next(context); // Continue to the next middleware
			        return;
			    }

			    string token = context.Session.GetString("access_token");
			    string name = context.Session.GetString("name");

				if (token == null)
				{
					context.Response.ContentType = "text/html";
					await context.Response.SendFileAsync("wwwroot/auth.html");
	 				return;
				}

				await _next(context);
				return;
			} 
			catch (Exception e)
			{
				await _next(context);
			}
		}
	}

}