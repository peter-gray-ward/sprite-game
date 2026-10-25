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
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                bool isGuid = Guid.TryParse(levelId, out Guid parsedId);

                var command = new NpgsqlCommand(@"
                    SELECT id, boundary_tile_ids, name
                    FROM level
                    WHERE player_name = @user_name
                      AND (
                          (@is_guid AND id = @level_id)
                          OR
                          (NOT @is_guid AND name = @level_name)
                      )
                    LIMIT 1
                ", connection);

                command.Parameters.AddWithValue("user_name", userName);
                command.Parameters.AddWithValue("is_guid", isGuid);
                command.Parameters.AddWithValue("level_id", isGuid ? parsedId : Guid.Empty);
                command.Parameters.AddWithValue("level_name", levelId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var level = new Level
                        {
                            id = reader.GetGuid(reader.GetOrdinal("id")),
                            name = reader["name"]?.ToString() ?? "",
                            boundary_tile_ids = reader["boundary_tile_ids"]?.ToString() ?? ""
                        };

                        return new ServiceResult("success", level);
                    }
                }

                // A GUID must already exist.
                if (isGuid)
                    return new ServiceResult("error", "Level not found");

                // A name can create a new level.
                var newId = Guid.NewGuid();

                using var insert = new NpgsqlCommand(@"
                    INSERT INTO level (
                        id,
                        player_name,
                        name,
                        boundary_tile_ids
                    )
                    VALUES (
                        @id,
                        @user_name,
                        @name,
                        @boundary_tile_ids
                    )
                ", connection);

                insert.Parameters.AddWithValue("id", newId);
                insert.Parameters.AddWithValue("user_name", userName);
                insert.Parameters.AddWithValue("name", levelId);
                insert.Parameters.AddWithValue("boundary_tile_ids", "[]");

                await insert.ExecuteNonQueryAsync();

                return new ServiceResult("success", new Level
                {
                    id = newId,
                    name = levelId,
                    boundary_tile_ids = "[]"
                });
            }
            catch (Exception ex)
            {
                return new ServiceResult("error", ex);
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
