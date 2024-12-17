using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace App.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public HomeController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var filePath = Path.Combine(_env.WebRootPath, "index.html");
            return PhysicalFile(filePath, "text/html");
        }
    }
}
