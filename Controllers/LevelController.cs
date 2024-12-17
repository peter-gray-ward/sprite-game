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

        public LevelController(LevelServices levelServices)
        {
            _levelServices = levelServices;
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
                ServiceResult level = await _levelServices.GetLevel(userName, levelId);

                if (level.exception != null)
                {
                    return StatusCode(500, new { status = level.status, message = level.exception.Message });
                }

                return Ok(new { status = "success", data = level.data });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = e.Message });
            }
        }

        [HttpPost("{levelId}")]
        public async Task<IActionResult> EditLevel(string levelId, [FromBody] Dictionary<string, JsonElement> levelData)
        {
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
