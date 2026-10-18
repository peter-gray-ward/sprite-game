
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

        public async Task<Guid> Create(
            string userName,
            SaveDocumentRequest request)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();

            await using var transaction =
                await connection.BeginTransactionAsync();

            if (request.block_id.HasValue)
            {
                await using var check = new NpgsqlCommand(@"
                    SELECT COUNT(*)
                    FROM block
                    WHERE id = @blockId
                      AND user_name = @userName
                      AND repeat_x = 1
                      AND repeat_y = 1
                      AND document_id IS NULL
                ", connection, transaction);

                check.Parameters.AddWithValue(
                    "blockId", request.block_id.Value);
                check.Parameters.AddWithValue(
                    "userName", userName);

                var count = Convert.ToInt32(
                    await check.ExecuteScalarAsync());

                if (count != 1)
                    throw new InvalidOperationException(
                        "Block not found, already linked, or repeated.");
            }

            Guid documentId;

            await using (var insert = new NpgsqlCommand(@"
                INSERT INTO document (user_name, title, content)
                VALUES (@userName, @title, @content)
                RETURNING id
            ", connection, transaction))
            {
                insert.Parameters.AddWithValue("userName", userName);
                insert.Parameters.AddWithValue("title", request.title);
                insert.Parameters.AddWithValue("content", request.content);

                documentId = (Guid)
                    (await insert.ExecuteScalarAsync())!;
            }

            if (request.block_id.HasValue)
            {
                await using var attach = new NpgsqlCommand(@"
                    UPDATE block
                    SET document_id = @documentId
                    WHERE id = @blockId
                      AND user_name = @userName
                      AND document_id IS NULL
                      AND repeat_x = 1
                      AND repeat_y = 1
                ", connection, transaction);

                attach.Parameters.AddWithValue(
                    "documentId", documentId);
                attach.Parameters.AddWithValue(
                    "blockId", request.block_id.Value);
                attach.Parameters.AddWithValue(
                    "userName", userName);

                if (await attach.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException(
                        "Unable to attach document to block.");
            }

            await transaction.CommitAsync();

            return documentId;
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

            return new Document
            {
                id = reader.GetGuid(0),
                user_name = reader.GetString(1),
                title = reader.GetString(2),
                content = reader.GetString(3),
                created_at = reader.GetDateTime(4),
                updated_at = reader.GetDateTime(5)
            };
        }

        public async Task<bool> Update(
            Guid id,
            string userName,
            UpdateDocumentRequest request)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(@"
                UPDATE document
                SET title = @title,
                    content = @content,
                    updated_at = NOW()
                WHERE id = @id
                  AND user_name = @userName
            ", connection);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userName", userName);
            command.Parameters.AddWithValue("title", request.title);
            command.Parameters.AddWithValue("content", request.content);

            return await command.ExecuteNonQueryAsync() == 1;
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
