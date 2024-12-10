using Microsoft.AspNetCore.Http.HttpResults;

namespace App.Models
{
    public class SaveImageRequest
    {
        public string url { get; set; } = String.Empty;
        public string tag { get; set; } = String.Empty;
    }
}