using Microsoft.AspNetCore.Mvc;
using Paeezan.Server.DTOs;
using Paeezan.Server.Services;
using System.Threading.Tasks;

namespace Paeezan.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        public AuthController(AuthService auth) { _auth = auth; }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var (ok, msg) = await _auth.Register(dto.Username, dto.Password);
            if (!ok) return BadRequest(new AuthResultDto(false, null, msg));
            var (_, token, _) = await _auth.Login(dto.Username, dto.Password);
            return Ok(new AuthResultDto(true, token, "registered"));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var (ok, token, msg) = await _auth.Login(dto.Username, dto.Password);
            if (!ok) return Unauthorized(new AuthResultDto(false, null, msg));
            return Ok(new AuthResultDto(true, token, "ok"));
        }
    }
}