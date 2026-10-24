
using Microsoft.AspNetCore.Mvc;
using App.Models;
using App.Services;

namespace App.Controllers
{
    [ApiController]
    [Route("document")]
    public class DocumentController : ControllerBase
    {
        private readonly DocumentServices _documentServices;

        public DocumentController(DocumentServices documentServices)
        {
            _documentServices = documentServices;
        }

        [HttpPost("{blockId:guid}")]
        public async Task<IActionResult> Save(
            Guid blockId,
            [FromBody] SaveDocumentRequest request)
        {
            var userName = HttpContext.Session.GetString("name");

            if (string.IsNullOrEmpty(userName))
                return Unauthorized();

            try
            {
                var id = await _documentServices.Save(
                    blockId,
                    userName,
                    request);

                return Ok(new
                {
                    status = "success",
                    id
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var userName = HttpContext.Session.GetString("name");

            if (userName == null)
                return Unauthorized(new { status = "error", message = "Unauthorized user"  });

            var document = await _documentServices.Get(id, userName);

            if (document == null)
                return Ok(new { status = "success", data = new Document(id) });

            return Ok(new { status = "success", data = document });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userName = HttpContext.Session.GetString("name");

            if (userName == null)
                return Unauthorized(new { status = "error" });

            var deleted = await _documentServices.Delete(id, userName);

            if (!deleted)
                return NotFound(new { status = "error" });

            return Ok(new { status = "success" });
        }
    }
}
