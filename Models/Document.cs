namespace App.Models
{
    public class Document
    {
        public Guid id { get; set; }
        public string user_name { get; set; } = "";
        public string title { get; set; } = "Untitled";
        public string content { get; set; } = "";
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class SaveDocumentRequest
    {
        public Guid? block_id { get; set; }
        public string title { get; set; } = "Untitled";
        public string content { get; set; } = "";
    }

    public class UpdateDocumentRequest
    {
        public string title { get; set; } = "Untitled";
        public string content { get; set; } = "";
    }
}
