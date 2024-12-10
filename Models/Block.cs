namespace App.Models
{
    public class Block
    {
        public Guid id { get; set; }
        public string user_name { get; set; } = String.Empty;
        public Guid level_id { get; set; }
        public string level_grid { get; set; } = String.Empty;
        public Guid image_id { get; set; }
        public int start_x { get; set; } = 0;
        public int start_y { get; set; } = 0;
        public int repeat_y { get; set; } = 1;
        public int repeat_x { get; set; } = 1;
        public int dir_y { get; set; } = 1;
        public int dir_x { get; set; } = 1;
        public int dimension { get; set; } = 1;
        public Guid recurrence_id { get; set; }
        public string object_area { get; set; } = String.Empty;
        public int translate_object_area { get; set; } = 0;
        public int random_rotation { get; set; } = 0;
        public int ground { get; set; } = 0;
        public Guid? parent_id { get; set; } = null;
        public string css { get; set; } = String.Empty;

    }
}