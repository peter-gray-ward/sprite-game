
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

        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] SaveDocumentRequest request)
        {
            var userName = HttpContext.Session.GetString("name");

            if (userName == null)
                return Unauthorized(new { status = "error" });

            try
            {
                var id = await _documentServices.Create(userName, request);

                return Ok(new { status = "success", id });
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new {
                    status = "error",
                    message = e.Message
                });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var userName = HttpContext.Session.GetString("name");

            if (userName == null)
                return Unauthorized(new { status = "error" });

            var document = await _documentServices.Get(id, userName);

            if (document == null)
                return NotFound(new { status = "error" });

            return Ok(new { status = "success", data = document });
        }

        [HttpPost("update/{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateDocumentRequest request)
        {
            var userName = HttpContext.Session.GetString("name");

            if (userName == null)
                return Unauthorized(new { status = "error" });

            var updated = await _documentServices.Update(
                id, userName, request);

            if (!updated)
                return NotFound(new { status = "error" });

            return Ok(new { status = "success" });
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
