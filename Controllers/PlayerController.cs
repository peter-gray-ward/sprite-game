using Microsoft.AspNetCore.Mvc;
using App.Models;
using App.Services;
using System.Text.Json;

namespace App.Controllers
{
    [ApiController]
    [Route("player")]
    public class PlayerController : ControllerBase
    {
        private readonly PlayerServices _playerServices;
        private readonly SessionServices _sessionServices;

        public PlayerController(PlayerServices playerServices, SessionServices sessionServices)
        {
            _playerServices = playerServices;
            _sessionServices = sessionServices;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Player player)
        {
            try
            {
                if (player == null)
                    throw new Exception("Invalid credentials");

                ServiceResult registered = await _playerServices.Register(player.name, player.password);
                return Ok(new { status = registered.status });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { status = "error", message = "Error", details = e.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Player playerLoggingIn)
        {
            Console.WriteLine("Player logging in!");
            try
            {
                if (playerLoggingIn == null)
                    throw new Exception("Invalid login credentials");

                ServiceResult playerResult = await _playerServices.Login(playerLoggingIn.name, playerLoggingIn.password);
                Player player = playerResult.data as Player;

                if (player == null || string.IsNullOrEmpty(player.access_token))
                    throw new Exception("Invalid login credentials");

                _sessionServices.Login(HttpContext, player);

                return Ok(new
                {
                    status = "success",
                    level_id = player.level_id,
                    position_x = player.position_x,
                    position_y = player.position_y
                });
            }
            catch (Exception e)
            {
                return BadRequest(new { status = "error", message = e.Message });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            _sessionServices.Logout(HttpContext);
            return Ok(new { status = "success", message = "Logged out successfully" });
        }
    }
}
