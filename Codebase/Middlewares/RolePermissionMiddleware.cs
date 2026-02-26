using Codebase.Models.Dtos.Requests.Auth;

namespace Codebase.Middlewares;
using Attributes;
using Models.Dtos.Responses.Shared;
using Utils;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;

public class RolePermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDatabase _redis;

    public RolePermissionMiddleware(RequestDelegate next, IConnectionMultiplexer redis)
    {
        _next = next;
        _redis = redis.GetDatabase();
    }

    public async Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // 1. Metadata 
        var allowAnonymous = endpoint.Metadata
            .GetMetadata<IAllowAnonymous>() != null;

        if (allowAnonymous)
        {
            await _next(context);
            return;
        }

        var requiredPermission = endpoint.Metadata
            .GetMetadata<RequiredPermissionAttribute>()?.Permission;

        var authorizeOnly = endpoint.Metadata
            .GetMetadata<AuthorizeAttribute>() != null;

        if (string.IsNullOrEmpty(requiredPermission) && !authorizeOnly)
        {
            await _next(context);
            return;
        }

        //  2. Extract token 
        if (!context.Request.Headers.TryGetValue("Authorization", out var header) ||
            !header.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await ReturnError(context, ResponseCatalog.Unauthorized);
            return;
        }

        var token = header.ToString()["Bearer ".Length..].Trim();

        if (string.IsNullOrEmpty(token))
        {
            await ReturnError(context, ResponseCatalog.Unauthorized);
            return;
        }

        //  3. Load session 
        UserSession? session;
        try
        {
            var redisValue = await _redis.StringGetAsync($"session:{token}");

            if (redisValue.IsNullOrEmpty)
            {
                await ReturnError(context, ResponseCatalog.Unauthorized);
                return;
            }

            session = DataUtil.RedisValueToObject<UserSession>(redisValue);

            if (session == null)
            {
                await ReturnError(context, ResponseCatalog.Unauthorized);
                return;
            }
        }
        catch (RedisException)
        {
            // Redis chết, không verify được auth
            await ReturnError(context, ResponseCatalog.Internal);
            return;
        }

        //  4. Attach session 
        context.Items["UserSession"] = session;

        //  5. Permission check 
        if (!string.IsNullOrEmpty(requiredPermission))
        {
            var hasPermission =
                session.Permissions?.Contains(requiredPermission) ?? false;

            if (!hasPermission)
            {
                await ReturnError(context, ResponseCatalog.Forbidden);
                return;
            }
        }

        await _next(context);
    }

    private static async Task ReturnError(HttpContext context, ResponseCatalog catalog)
    {
        await ResponseDto.Create(catalog).ExecuteAsync(context);
    }
}