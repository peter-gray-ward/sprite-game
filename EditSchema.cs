using Npgsql; // Ensure you have this package installed

namespace App 
{
	public class EditSchema
	{
		private string connectionString { get; set; } = String.Empty;
		public EditSchema(string connectionString)
		{
			this.connectionString = connectionString;
		}
		public async void Run()
		{
			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();
				var command = new NpgsqlCommand(@$"
					DROP TABLE IF EXISTS level;
					CREATE TABLE level (
						id UUID PRIMARY KEY,
						name TEXT NOT NULL,
						player_name TEXT NOT NULL,
						boundary_tile_ids TEXT,
						exit_tile_map TEXT
					);

					-- default level!
					INSERT INTO level
					(id, name, player_name, boundary_tile_ids, exit_tile_map)
					VALUES
					('d2358090-8f14-4c5a-8c9e-fe59d58a4e75', 'Level One', 'peter', '', '');


					CREATE TABLE document (
					    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
					    user_name TEXT NOT NULL,
					    title TEXT NOT NULL DEFAULT 'Untitled',
					    content TEXT NOT NULL DEFAULT '',
					    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
					    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
					);


					DROP TABLE IF EXISTS Block;
					CREATE TABLE IF NOT EXISTS Block (
						id UUID PRIMARY KEY,
						recurrence_id UUID,
						tag TEXT,
						user_name TEXT NOT NULL,
						start_y INT NOT NULL,
						start_x INT NOT NULL,
						repeat_y INT NOT NULL,
						repeat_x INT NOT NULL,
						dir_y INT NOT NULL,
						dir_x INT NOT NULL,
						level_id UUID NOT NULL,
						level_grid TEXT NOT NULL,
						image_id UUID,
						document_id UUID NULL,
						css TEXT,
						dimension_x DOUBLE PRECISION NOT NULL,
						dimension_y DOUBLE PRECISION NOT NULL,
						object_area TEXT,
						translate_object_area INT NOT NULL,
						object_area_inclusions TEXT NOT NULL,
						random_rotation INT NOT NULL,
						ground INT NOT NULL,
						parent_id UUID
					);

					CREATE INDEX idx_block_document_id
					ON block(document_id);

					ALTER TABLE block
					ADD CONSTRAINT document_no_repetition
					CHECK (
					    document_id IS NULL
					    OR (repeat_x = 1 AND repeat_y = 1)
					);

					DROP TABLE IF EXISTS Player;
					CREATE TABLE IF NOT EXISTS Player (
						name TEXT PRIMARY KEY NOT NULL,
						password TEXT NOT NULL,
						level_id UUID NOT NULL,
						position_x FLOAT,
						position_y FLOAT,
						direction TEXT NOT NULL,
						z_index INT NOT NULL,
						level_grid TEXT NOT NULL,
						top_left_tile TEXT NOT NULL
					);

					DROP TABLE IF EXISTS Item;
					CREATE TABLE Item (
						id UUID PRIMARY KEY,
						name TEXT,
						image_id UUID,
						width FLOAT,
						height FLOAT
					);

					DROP TABLE IF EXISTS PlayerInventory;
					CREATE TABLE PlayerInventory (
						id UUID PRIMARY KEY,
						user_name TEXT NOT NULL,
						item_id UUID NOT NULL
					);

					DROP TABLE IF EXISTS public.images;
					CREATE TABLE public.images (
					    id UUID PRIMARY KEY,
					    tag TEXT,
					    src BYTEA NOT NULL,
					    user_name TEXT
					);
				", connection);
				await command.ExecuteNonQueryAsync();

				Console.WriteLine("schema edited!");
			}
		}
	}
}