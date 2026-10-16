
namespace App.Models
{
    public class Image
    {
        public Guid id { get; set; }
        public string? tag { get; set; }
        public byte[] src { get; set; } = Array.Empty<byte>();
        public string? user_name { get; set; }
    }
}
