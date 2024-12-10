using System.Text.Json;
using App.Models;
using Npgsql;

namespace App.Services
{
    public class BlockServices
    {
        private DatabaseServices db;
        public BlockServices(DatabaseServices db)
        {
            this.db = db;
        }
        public async Task<ServiceResult> SaveBlock(string user_name, string imageId, string levelId, Block drop_area)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();
                var id = Guid.NewGuid();
                var recurrence_id = Guid.NewGuid();

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
                command.Parameters.AddWithValue("level_grid", drop_area.level_grid);
                command.Parameters.AddWithValue("image_id", Guid.Parse(imageId));
                command.Parameters.AddWithValue("start_y", drop_area.start_y);
                command.Parameters.AddWithValue("start_x", drop_area.start_x);
                command.Parameters.AddWithValue("repeat_y", drop_area.repeat_y);
                command.Parameters.AddWithValue("repeat_x", drop_area.repeat_x);
                command.Parameters.AddWithValue("dir_y", drop_area.dir_x / Math.Abs(drop_area.dir_x));
                command.Parameters.AddWithValue("dir_x", drop_area.dir_y / Math.Abs(drop_area.dir_y));
                command.Parameters.AddWithValue("dimension", drop_area.dimension);
                command.Parameters.AddWithValue("recurrence_id", recurrence_id);

                await command.ExecuteNonQueryAsync();

                return new ServiceResult("success", id.ToString());
                
            }
            catch (Exception e)
            {
                return new ServiceResult("error", e);
            }
        }
        public async Task<ServiceResult> DeleteBlock(string recurrence_id)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();
                var command = new NpgsqlCommand(@$"
                    DELETE FROM public.Block
                    WHERE recurrence_id = @recurrence_id
                ", connection);
                command.Parameters.AddWithValue("recurrence_id", Guid.Parse(recurrence_id));
                await command.ExecuteNonQueryAsync();

                return new ServiceResult("success");
            }
            catch (Exception e)
            {
                return new ServiceResult("error", e);
            }
        }
        public async Task<ServiceResult> UpdateBlock(string user_name, string recurrence_id, Block block)
        {
            try
            {
                using var connection = db.GetConnection();
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
                        {(block.parent_id is not null ? "parent_id = @parent_id," : "")}
                        level_id = @level_id,
                        level_grid = @level_grid,
                        css = @css
                    WHERE recurrence_id = @recurrence_id
                ", connection);
                command.Parameters.AddWithValue("recurrence_id", Guid.Parse(recurrence_id));
                command.Parameters.AddWithValue("dimension", block.dimension);
                command.Parameters.AddWithValue("start_y", block.start_y);
                command.Parameters.AddWithValue("start_x", block.start_x);
                command.Parameters.AddWithValue("repeat_y", block.repeat_y > 0 ? block.repeat_y : 1);
                command.Parameters.AddWithValue("repeat_x", block.repeat_x > 0 ? block.repeat_x : 1);
                command.Parameters.AddWithValue("dir_y", block.dir_y);
                command.Parameters.AddWithValue("dir_x", block.dir_x);
                command.Parameters.AddWithValue("translate_object_area", block.translate_object_area);
                command.Parameters.AddWithValue("random_rotation", block.random_rotation);
                command.Parameters.AddWithValue("level_id", block.level_id);
                command.Parameters.AddWithValue("level_grid", block.level_grid);
                command.Parameters.AddWithValue("ground", block.ground);
                if (block.parent_id is not null)
                {
                    command.Parameters.AddWithValue("parent_id", block.parent_id);
                }
                command.Parameters.AddWithValue("css", block.css);
                await command.ExecuteNonQueryAsync();


                return new ServiceResult("success");
            }
            catch (Exception e)
            {
                return new ServiceResult("error", e);
            }
        }
        public async Task<ServiceResult> UpdateObjectArea(string recurrence_id, Block block)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();
                var command = new NpgsqlCommand(@$"
                    UPDATE public.Block
                    SET
                        object_area = @object_area
                    WHERE recurrence_id = @recurrence_id
                ", connection);
                command.Parameters.AddWithValue("recurrence_id", Guid.Parse(recurrence_id));
                command.Parameters.AddWithValue("object_area", block.object_area);
                await command.ExecuteNonQueryAsync();
                
                return new ServiceResult("success");
            }
            catch (Exception e)
            {
                return new ServiceResult("error", e);
            }
        }
        public async Task<ServiceResult> GetBlocks(string username, string levelId)
        {
            try
            {
                using var connection = db.GetConnection();
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

                return new ServiceResult("success", blocks);
            }
            catch (Exception e)
            {
                return new ServiceResult("error", e);
            }
        }
    }
}