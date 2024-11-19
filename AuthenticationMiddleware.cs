using Npgsql;
using BCrypt.Net;

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
			if (context.Request.Path.Equals("/register", StringComparison.OrdinalIgnoreCase) ||
	        	context.Request.Path.Equals("/login", StringComparison.OrdinalIgnoreCase))
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

			try
			{
				using (var connection = new NpgsqlConnection(_connectionString))
				{
					await connection.OpenAsync();

					var command = new NpgsqlCommand(@"
						SELECT name FROM Player WHERE access_token = @token
					", connection);
					command.Parameters.AddWithValue("@token", Guid.Parse(token));

					var username = await command.ExecuteScalarAsync();

					if (username == null)
					{
						context.Response.ContentType = "text/html";
	    				await context.Response.SendFileAsync("wwwroot/index.html");
	    				return;
	    			}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"EXCEPTION {e.Message}");
				context.Response.ContentType = "text/html";
        		await context.Response.SendFileAsync("wwwroot/auth.html");
        		return;
			}

			await _next(context);
		}
	}

}