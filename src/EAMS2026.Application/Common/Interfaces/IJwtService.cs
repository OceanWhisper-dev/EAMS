namespace EAMS2026.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions);
    global::System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);
}