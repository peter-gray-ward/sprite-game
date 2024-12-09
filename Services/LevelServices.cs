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
    
        public async Task<Level> GetLevel(string userName, string levelId)
        {
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
                level.name = reader["name"].ToString();
                level.id = Guid.Parse(reader["id"].ToString());
                level.boundary_tile_ids = reader["boundary_tile_ids"].ToString();
            }

            return level;
        }
        public async Task<bool> EditLevel(string userName, Dictionary<string, JsonElement> level)
    {
        try
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();
            var command = new NpgsqlCommand(@$"
                UPDATE level
                    set boundary_tile_ids = @boundary_tile_ids
                WHERE player_name = @player_name
                AND id = @id
            ", connection);
            command.Parameters.AddWithValue("boundary_tile_ids", level["boundary_tile_ids"].GetString());
            command.Parameters.AddWithValue("player_name", userName);
            command.Parameters.AddWithValue("id", Guid.Parse(level["id"].GetString()));

            var reader = await command.ExecuteNonQueryAsync();

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    }
}
