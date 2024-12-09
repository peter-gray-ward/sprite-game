using App.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace App.Services
{
	public class ImageServices
	{
		private DatabaseServices db;
		public ImageServices(DatabaseServices db)
		{
			this.db = db;
		}
		public async Task<ServiceResult> SaveImage(string url, string tag, string user_name)
		{
			try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var imageData = await response.Content.ReadAsByteArrayAsync();
                var imageId = Guid.NewGuid();

                var connection = db.GetConnection();
                await connection.OpenAsync();
                var command = new NpgsqlCommand("INSERT INTO public.images (id, tag, src, user_name) VALUES (@id, @tag, @src, @user_name)", connection);
                command.Parameters.AddWithValue("id", imageId);
                command.Parameters.AddWithValue("tag", tag);
                command.Parameters.AddWithValue("src", imageData);
                command.Parameters.AddWithValue("user_name", user_name);
                await command.ExecuteNonQueryAsync();

                return new ServiceResult("success", imageId.ToString());
            }
            catch (Exception e)
            {
                return new ServiceResult("error", e);
            }
		}
        public async Task<ServiceResult> GetImageIds(string user_name)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();
                var command = new NpgsqlCommand("SELECT id, tag FROM public.images WHERE user_name = @user_name GROUP BY tag, id", connection);
                command.Parameters.AddWithValue("user_name", user_name);
                var reader = await command.ExecuteReaderAsync();

                var images = new List<object>();
                while (await reader.ReadAsync())
                {
                    images.Add(new { id = reader["id"], tag = reader["tag"] });
                }
                
                ServiceResult result = new ServiceResult("success");
                result.data = images;

                return result;
            }
            catch (Exception e)
            {
                return new ServiceResult("error", e);
            }
        }
        public async Task<ServiceResult> GetImage(string user_name, string imageId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();
                var command = new NpgsqlCommand("SELECT src FROM public.images WHERE user_name = @user_name AND id = @imageId", connection);
                command.Parameters.AddWithValue("user_name", user_name);
                command.Parameters.AddWithValue("imageId", Guid.Parse(imageId));
                var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var imageData = (byte[])reader["src"];

                    ServiceResult result = new ServiceResult("success");
                    result.data = imageData;
                    return result;
                }
                else
                {
                    return new ServiceResult("error", new Exception("Not Found"));
                }
            }
            catch (Exception e)
            {
                return new ServiceResult("error", e);
            }
        }
	}
}