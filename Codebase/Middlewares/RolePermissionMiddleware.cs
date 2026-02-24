using Codebase.Attributes;
using Codebase.Models.Dtos.Auth;
using Codebase.Models.Dtos.Responses.Shared;
using Codebase.Utils;
using StackExchange.Redis;

namespace Codebase.Middlewares;

public class RolePermissionMiddleware(RequestDelegate next, IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await next(context);
            return;
        }

        var requiredPermission = endpoint.Metadata.GetMetadata<RequiredPermissionAttribute>()?.Permission;
        var isAuthorizeOnly = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>() != null;
        var isAllowAnonymous = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null;

        if (isAllowAnonymous || (string.IsNullOrEmpty(requiredPermission) && !isAuthorizeOnly))
        {
            await next(context);
            return;
        }

        // 1. Lấy Token
        string authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await ReturnError(context, ResponseCatalog.Unauthorized);
            return;
        }
        var token = authHeader["Bearer ".Length..].Trim();

        // 2. MIGRATE: Lấy Session từ Redis Hash
        // Sử dụng hàm GetObjectFromHashAsync đã tối ưu hóa Reflection
        var userSession = await RedisUtil.GetObjectFromHashAsync<UserSession>(_db, $"session:{token}");

        if (userSession == null)
        {
            await ReturnError(context, ResponseCatalog.Unauthorized);
            return;
        }

        // Gán vào context để dùng ở Controller
        context.Items["UserSession"] = userSession;

        // 3. Kiểm tra quyền
        if (!string.IsNullOrEmpty(requiredPermission))
        {
            var hasPermission = userSession.Permissions?.Contains(requiredPermission) ?? false;
            if (!hasPermission)
            {
                await ReturnError(context, ResponseCatalog.Forbidden);
                return;
            }
        }

        await next(context);
    }


    private static async Task ReturnError(HttpContext context, ResponseCatalog catalog)
    {
        // Sử dụng ExecuteAsync để trả về DTO chuẩn cho Client
        await ResponseDto.Create(catalog).ExecuteAsync(context);
    }
}

