using System.Security.Claims;
using System.Text;
using Codebase.Models.Enums;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Codebase.Utils;

public sealed class JwtUtil
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expireMinutes;

    public JwtUtil(IConfiguration config)
    {
        _secretKey = config["Jwt:SecretKey"] ?? throw new ArgumentNullException("Jwt SecretKey missing");
        _issuer = config["Jwt:Issuer"] ?? "CodebaseIssuer";
        _audience = config["Jwt:Audience"] ?? "CodebaseAudience";
        _expireMinutes = int.Parse(config["Jwt:ExpireMinutes"] ?? "1440"); // Mặc định 1 ngày
    }

    public string GenerateToken(Guid userId, string username, string roleIds, LanguageEnum lang)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        if (lang != LanguageEnum.En)
        {
            lang = LanguageEnum.Vi;
        }

        // Map dữ liệu vào Dictionary Claims
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = userId.ToString(),      // UserId
            [JwtRegisteredClaimNames.UniqueName] = username,        // Username
            ["roleIds"] = roleIds,                         // Custom claim cho RoleId
            ["lang"] = lang                                         // Ngôn ngữ ưu tiên
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(_expireMinutes),
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(descriptor);
    }


    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var handler = new JsonWebTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Không cho phép lệch thời gian để bảo mật tuyệt đối
        };

        var result = await handler.ValidateTokenAsync(token, validationParameters);
        return result.IsValid ? new ClaimsPrincipal(result.ClaimsIdentity) : null;
    }
}
