using Microsoft.AspNetCore.Mvc;
using App.Models;
using App.Services;
using System.Text.Json;

namespace App.Controllers
{
    [ApiController]
    [Route("level")]
    public class LevelController : ControllerBase
    {
        private readonly LevelServices _levelServices;
        private readonly SessionServices _sessionServices;

        public LevelController(LevelServices levelServices, SessionServices sessionServices)
        {
            _levelServices = levelServices;
            _sessionServices = sessionServices;
        }

        [HttpGet("{levelId}")]
        public async Task<IActionResult> GetLevel(string levelId)
        {
            string? userName = HttpContext.Session.GetString("name");

            if (userName == null)
            {
                return BadRequest(new { status = "error", message = "User name is required." });
            }

            try
            {
                ServiceResult res = await _levelServices.GetLevel(userName, levelId);

                Level level = (Level) res.data;

                _sessionServices.SetLevel(HttpContext, level.id.ToString());

                if (res.exception != null)
                {
                    return StatusCode(500, new { status = res.status, message = res.exception.Message });
                }

                return Ok(new { status = "success", data = level });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = e.Message });
            }
        }

        [HttpPost("{levelId}")]
        public async Task<IActionResult> EditLevel(string levelId, [FromBody] Dictionary<string, JsonElement> levelData)
        {
            Console.WriteLine("Editing level " + levelId);
            string? userName = HttpContext.Session.GetString("name");

            if (userName == null)
            {
                return BadRequest(new { status = "error", message = "User name is required." });
            }

            if (levelData == null || !levelData.Any())
            {
                return BadRequest(new { status = "error", message = "Request body cannot be empty." });
            }

            try
            {
                ServiceResult edited = await _levelServices.EditLevel(userName, levelData);

                if (edited.exception != null)
                {
                    return StatusCode(500, new { status = "error", message = edited.exception.Message });
                }

                return Ok(new { status = edited.status, data = edited.data });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = e.Message });
            }
        }
    }
}
