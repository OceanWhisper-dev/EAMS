using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EAMS2026.Infrastructure.Services;

/// <summary>
/// JWT 认证令牌服务。
/// 负责生成和验证 JSON Web Token，将用户身份、角色和权限信息嵌入令牌中。
/// </summary>
public class JwtService : EAMS2026.Application.Common.Interfaces.IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 生成 JWT Token。
    /// 包含以下 Claims：
    /// - nameidentifier: 用户ID
    /// - name: 用户名
    /// - role: 角色编码（可多个）
    /// - permission: 权限编码（可多个）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="username">用户名</param>
    /// <param name="roles">角色编码列表</param>
    /// <param name="permissions">权限编码列表</param>
    /// <returns>JWT Token字符串</returns>
    public string GenerateToken(long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secretKey = jwtSection["SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey 未配置");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var issuer = jwtSection["Issuer"] ?? "EAMS2026";
        var audience = jwtSection["Audience"] ?? "EAMS2026";
        var expirationMinutes = int.Parse(jwtSection["ExpirationMinutes"] ?? "480");

        // 构建 Claims 集合
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT唯一ID，防止重放
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // 签发时间
        };

        // 为每个角色添加一个 claim
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // 为每个权限添加一个 claim
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 验证 JWT Token 并返回 ClaimsPrincipal。
    /// 用于需要手动验证 Token 的场景（如 WebSocket 连接）。
    /// </summary>
    /// <param name="token">JWT Token字符串</param>
    /// <returns>验证成功返回 ClaimsPrincipal，失败返回 null</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secretKey = jwtSection["SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"] ?? "EAMS2026",
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"] ?? "EAMS2026",
                ValidateLifetime = true,       // 验证令牌是否过期
                ClockSkew = TimeSpan.Zero      // 不允许时钟偏差
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}