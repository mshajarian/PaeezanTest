using Microsoft.AspNetCore.Mvc;
using Paeezan.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Paeezan.Server.Services.RoomService;

namespace Paeezan.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly RoomService _rooms;
        public AdminController(RoomService rooms) { _rooms = rooms; }

        [HttpGet("rooms")]
        [Authorize]
        public IActionResult Rooms() => Ok(_rooms.ListRooms().Select(r => new { r.Code, r.PlayerAUserId, r.PlayerBUserId, r.Started, r.CreatedAt }));

        [HttpGet("status/{code}")]
        public IActionResult Status(string code)
        {
            if (_rooms.TryGetByCode(code, out var r)) return Ok(new { started = r.Started, created = r.CreatedAt, a = r.PlayerAUserId != null, b = r.PlayerBUserId != null });
            return NotFound();
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok", now = System.DateTime.UtcNow });
    }
}