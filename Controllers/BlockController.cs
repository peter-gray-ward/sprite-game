using Microsoft.AspNetCore.Mvc;
using App.Models;
using App.Services;
using System.Text.Json;

namespace App.Controllers
{
    [ApiController]
    [Route("blocks")]
    public class BlockController : ControllerBase
    {
        private readonly BlockServices _blockServices;

        public BlockController(BlockServices blockServices)
        {
            _blockServices = blockServices;
        }

        [HttpPost("save/{levelId}/{imageId}")]
        public async Task<IActionResult> SaveBlock(string levelId, string imageId, [FromBody] Block dropArea)
        {
            string userName = HttpContext.Session.GetString("name") ?? string.Empty;

            if (dropArea == null)
            {
                return BadRequest(new { status = "error", message = "Malformed drop area" });
            }

            var saved = await _blockServices.SaveBlock(userName, imageId, levelId, dropArea);

            if (saved.exception != null)
            {
                return StatusCode(500, new { status = saved.status, message = saved.exception });
            }

            return Ok(new { status = saved.status, message = saved.data });
        }

        [HttpDelete("delete/{recurrenceId}")]
        public async Task<IActionResult> DeleteBlock(string recurrenceId)
        {
            var deletion = await _blockServices.DeleteBlock(recurrenceId);

            if (deletion.exception != null)
            {
                return StatusCode(500, new { status = deletion.status, message = deletion.exception.Message });
            }

            return Ok(new { status = deletion.status });
        }

        [HttpPost("update/{recurrenceId}")]
        public async Task<IActionResult> UpdateBlock(string recurrenceId, [FromBody] Block block)
        {
            string userName = HttpContext.Session.GetString("name") ?? string.Empty;

            if (block == null)
            {
                return BadRequest(new { status = "error", message = "Malformed block" });
            }

            var update = await _blockServices.UpdateBlock(userName, recurrenceId, block);

            if (update.exception != null)
            {
                return StatusCode(500, new { status = update.status, message = update.exception.Message });
            }

            return Ok(new { status = update.status });
        }

        [HttpPost("update-object-area/{recurrenceId}")]
        public async Task<IActionResult> UpdateObjectArea(string recurrenceId, [FromBody] Block block)
        {
            string userName = HttpContext.Session.GetString("name") ?? string.Empty;

            if (block == null)
            {
                return BadRequest(new { status = "error", message = "Malformed drop area" });
            }

            var update = await _blockServices.UpdateObjectArea(recurrenceId, block);

            if (update.exception != null)
            {
                return StatusCode(500, new { status = update.status, message = update.exception.Message });
            }

            return Ok(new { status = update.status, data = block.object_area });
        }

        [HttpGet("get/{levelId}")]
        public async Task<IActionResult> GetBlocks(string levelId)
        {
            string userName = HttpContext.Session.GetString("name") ?? string.Empty;

            var blocks = await _blockServices.GetBlocks(userName, levelId);

            if (blocks.exception != null)
            {
                return StatusCode(500, new { status = "error", message = blocks.exception.Message });
            }

            return Ok(new { status = blocks.status, data = blocks.data });
        }
    }
}
