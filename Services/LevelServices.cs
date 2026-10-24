using System.Text.Json;
using App.Models;
using Npgsql;

namespace App.Services
{
    public class LevelServices
    {
        private readonly DatabaseServices db;
        public LevelServices(DatabaseServices db) {
            this.db = db;
        }
    
        public async Task<ServiceResult> GetLevel(string userName, string levelId)
        {
            try {
                using var connection = db.GetConnection();
                await connection.OpenAsync();
                var command = new NpgsqlCommand(@$"
                    SELECT l.id, l.boundary_tile_ids, l.name
                    FROM level l
                    INNER JOIN player p on (l.player_name = p.name)
                    WHERE p.name = @user_name
                    AND l.id = @level_id
                ", connection);
                command.Parameters.AddWithValue("user_name", userName);
                command.Parameters.AddWithValue("level_id", Guid.Parse(levelId));

                var reader = await command.ExecuteReaderAsync();

                Level level = new();

                if (await reader.ReadAsync())
                {
                    level.name = reader["name"]?.ToString() ?? string.Empty;
                    level.id = Guid.Parse(reader["id"]?.ToString() ?? Guid.Empty.ToString());
                    level.boundary_tile_ids = reader["boundary_tile_ids"]?.ToString() ?? string.Empty;
                }

                return new ServiceResult("success", level);
            } catch (Exception ex) {
                return new ServiceResult("ex " + ex.Message);
            }
        }
        public async Task<ServiceResult> EditLevel(string userName, Dictionary<string, JsonElement> level)
        {
            try
            {
                Console.WriteLine("EditLevel(" + userName + ")");
                using var connection = db.GetConnection();
                await connection.OpenAsync();
                var command = new NpgsqlCommand(@$"
                    UPDATE level
                        set name = @name,
                            boundary_tile_ids = @boundary_tile_ids,
                            exit_tile_map = @exit_tile_map
                    WHERE player_name = @player_name
                    AND id = @id
                ", connection);
                // Check if the property exists and get its value safely
                if (level.TryGetValue("boundary_tile_ids", out JsonElement boundaryTileIdsElement))
                {
                    command.Parameters.AddWithValue("boundary_tile_ids", boundaryTileIdsElement.GetString() ?? string.Empty);
                }
                else
                {
                    command.Parameters.AddWithValue("boundary_tile_ids", string.Empty);
                }

                if (level.TryGetValue("exit_tile_map", out JsonElement exitTileMapElement))
                {
                    command.Parameters.AddWithValue("exit_tile_map", exitTileMapElement.GetString() ?? string.Empty);
                }

                if (level.TryGetValue("name", out JsonElement nameElement))
                {
                    command.Parameters.AddWithValue("name", nameElement.GetString() ?? string.Empty);
                }

                if (level.TryGetValue("player_name", out JsonElement player_nameElement))
                {
                    command.Parameters.AddWithValue("player_name", player_nameElement.GetString() ?? string.Empty);
                }

                string id = string.Empty;
                if (level.TryGetValue("id", out JsonElement idElement))
                {
                    id = idElement.GetString();
                    command.Parameters.AddWithValue("id", id != "" ? Guid.Parse(id) : Guid.Empty.ToString());
                }
                else
                {
                    command.Parameters.AddWithValue("id", Guid.Empty);
                }

                var reader = await command.ExecuteNonQueryAsync();
                

                if (id != string.Empty) 
                {
                   return await GetLevel(userName, id);
                }

                return new ServiceResult("error");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new ServiceResult("error: " + e.Message, e);
            }
        }
    }
}
