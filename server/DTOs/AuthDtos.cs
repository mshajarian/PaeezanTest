namespace Paeezan.Server.DTOs
{
    public record RegisterDto(string Username, string Password);

    public record LoginDto(string Username, string Password);

    public record AuthResultDto(bool Success, string? Token, string Message);
}