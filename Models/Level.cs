namespace App.Models
{
    public class Level
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string boundary_tile_ids { get; set; }
    }
}