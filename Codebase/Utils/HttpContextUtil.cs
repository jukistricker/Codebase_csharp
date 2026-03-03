using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens; 

namespace Codebase.Utils;

public static class HttpContextUtil
{
    private static IHttpContextAccessor? _accessor;

    public static void Configure(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public static string? CurrentUserId
    {
        get
        {
            // Lấy từ ClaimsPrincipal đã được Middleware xác thực
            var user = _accessor?.HttpContext?.User;
            
            // Tìm claim "sub" (JwtRegisteredClaimNames.Sub)
            return user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                                 ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
        }
    }

    // Tận dụng hàm cũ của bạn nếu cần
    public static string GetBearerToken(this HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var header))
            return null;

        var value = header.ToString();
        return value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
            ? value["Bearer ".Length..].Trim() 
            : null;
    }
}