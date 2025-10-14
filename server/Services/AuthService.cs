using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Paeezan.Server.Repositories;
using Paeezan.Server.Models;
using Microsoft.Extensions.Options;

namespace Paeezan.Server.Services
{
    public class JwtSettings { public string? Key { get; set; } public string? Issuer { get; set; } public string? Audience { get; set; } public int ExpiresMinutes { get; set; } }

    public class AuthService
    {
        private readonly UserRepository _userRepo;
        private readonly JwtSettings _jwt;
        public AuthService(UserRepository userRepo, IOptions<JwtSettings> jwt)
        {
            _userRepo = userRepo;
            _jwt = jwt.Value;
        }

        public async Task<(bool ok, string message)> Register(string username, string password)
        {
            var exists = await _userRepo.GetByUsername(username);
            if (exists != null) return (false, "username_taken");
            var user = new User { Username = username, PasswordHash = Hash(password), Wins = 0 };
            await _userRepo.Create(user);
            return (true, "ok");
        }

        public async Task<(bool ok, string? token, string message)> Login(string username, string password)
        {
            var user = await _userRepo.GetByUsername(username);
            if (user == null) return (false, null, "invalid_credentials");
            if (!Verify(password, user.PasswordHash ?? string.Empty)) return (false, null, "invalid_credentials");
            var token = GenerateToken(user);
            return (true, token, "ok");
        }

        private string GenerateToken(User u)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key ?? "REPLACE_THIS_WITH_A_VERY_STRONG_SECRET_CHANGE_ME"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, u.Id ?? string.Empty), new Claim("username", u.Username ?? string.Empty) };
            var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var b = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(b);
        }
        private static bool Verify(string plain, string? hashed) => Hash(plain) == (hashed ?? string.Empty);
    }
}