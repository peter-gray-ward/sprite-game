namespace App.Models
{
    public class Level
    {
        public Guid id { get; set; }
        public string name { get; set; } = String.Empty;
        public string boundary_tile_ids { get; set; } = String.Empty;
        public string exit_tile_map { get; set; } = String.Empty;
    }
}