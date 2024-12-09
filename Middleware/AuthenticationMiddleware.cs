namespace App.Middleware
{
	public class AuthenticationMiddleware
	{
		private readonly RequestDelegate _next;

		public AuthenticationMiddleware(RequestDelegate next)
		{
			this._next = next;
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