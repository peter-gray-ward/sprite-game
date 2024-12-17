using Microsoft.AspNetCore.Mvc;
using App.Models;
using App.Services;
using System.Text.Json;

namespace App.Controllers
{
    [ApiController]
    [Route("image")]
    public class ImageController : ControllerBase
    {
        private readonly ImageServices _imageServices;

        public ImageController(ImageServices imageServices)
        {
            _imageServices = imageServices;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveImage([FromBody] SaveImageRequest saveImageRequest)
        {
            string? userName = HttpContext.Session.GetString("name");

            if (userName == null)
            {
                return Unauthorized(new { status = "error", message = "User not authenticated" });
            }

            if (saveImageRequest == null)
            {
                return BadRequest(new { status = "error", message = "Malformed request body" });
            }

            try
            {
                ServiceResult saved = await _imageServices.SaveImage(saveImageRequest.url, saveImageRequest.tag, userName);

                if (saved.exception != null)
                {
                    return StatusCode(500, new { status = "error", message = saved.exception.Message });
                }

                return Created(string.Empty, new { status = "success", id = saved.data });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = e.Message });
            }
        }

        [HttpGet("ids")]
        public async Task<IActionResult> GetImageIds()
        {
            string? userName = HttpContext.Session.GetString("name");

            if (userName == null)
            {
                return Unauthorized(new { status = "error", message = "User not authenticated" });
            }

            try
            {
                ServiceResult imageIdResult = await _imageServices.GetImageIds(userName);

                if (imageIdResult.exception != null)
                {
                    return StatusCode(500, new { status = "error", message = imageIdResult.exception.Message });
                }

                return Ok(new { status = "success", data = imageIdResult.data });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = e.Message });
            }
        }

        [HttpGet("{imageId}")]
        public async Task<IActionResult> GetImage(string imageId)
        {
            string? userName = HttpContext.Session.GetString("name");

            if (userName == null)
            {
                return Unauthorized(new { status = "error", message = "User not authenticated" });
            }

            try
            {
                ServiceResult imageResult = await _imageServices.GetImage(userName, imageId);

                if (imageResult.exception != null)
                {
                    return NotFound(new { status = "error", message = imageResult.exception.Message });
                }

                if (imageResult.data is not byte[] imageData)
                {
                    return NotFound(new { status = "error", message = "Image data not found" });
                }

                return File(imageData, "image/png");
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = e.Message });
            }
        }
    }
}
