using Microsoft.AspNetCore.Http.HttpResults;

namespace App.Models
{
    public class ServiceResult
    {
        public string status { get; set; }
        public Exception? exception { get; set; }
        public object? data { get; set; }
        public ServiceResult(string status)
        {
            this.status = status;
        }
        public ServiceResult(string status, string data)
        {
            this.status = status;
            this.data = data;
        }
        public ServiceResult(string status, object data)
        {
            this.status = status;
            this.data = data;
        }
        public ServiceResult(string status, Exception exception)
        {
            this.status = status;
            this.exception = exception;
        }
    }
}