
using App.Models;
using Npgsql;

namespace App.Services
{
    public class DocumentServices
    {
        private readonly DatabaseServices db;

        public DocumentServices(DatabaseServices db)
        {
            this.db = db;
        }

        public async Task<Guid> Save(
            Guid blockId,
            string userName,
            SaveDocumentRequest request)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(@"
                INSERT INTO document (
                    id,
                    user_name,
                    title,
                    content
                )
                SELECT
                    b.id,
                    b.user_name,
                    @title,
                    @content
                FROM block b
                WHERE b.id = @blockId
                  AND b.user_name = @userName
                  AND b.repeat_x = 1
                  AND b.repeat_y = 1

                ON CONFLICT (id)
                DO UPDATE SET
                    title = EXCLUDED.title,
                    content = EXCLUDED.content,
                    updated_at = NOW()
                WHERE document.user_name = EXCLUDED.user_name

                RETURNING id;
            ", connection);

            command.Parameters.AddWithValue("blockId", blockId);
            command.Parameters.AddWithValue("userName", userName);
            command.Parameters.AddWithValue("title", request.title);
            command.Parameters.AddWithValue("content", request.content);

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException(
                    "Block not found, repeated, or not owned by user.");

            return (Guid)result;
        }

        public async Task<Document?> Get(Guid id, string userName)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(@"
                SELECT id, user_name, title, content,
                       created_at, updated_at
                FROM document
                WHERE id = @id
                  AND user_name = @userName
            ", connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userName", userName);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Document(reader.GetGuid(0))
            {
                user_name = reader.GetString(1),
                title = reader.GetString(2),
                content = reader.GetString(3),
                created_at = reader.GetDateTime(4),
                updated_at = reader.GetDateTime(5)
            };
        }

        public async Task<bool> Delete(Guid id, string userName)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(@"
                DELETE FROM document
                WHERE id = @id
                  AND user_name = @userName
            ", connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userName", userName);

            return await command.ExecuteNonQueryAsync() == 1;
        }
    }
}
