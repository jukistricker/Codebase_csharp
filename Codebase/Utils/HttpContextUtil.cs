namespace Codebase.Utils;

public static class HttpContextUtil
{
    public static string GetBearerToken(this HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var header))
            return null;

        var value = header.ToString();

        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return value["Bearer ".Length..].Trim();
    }
}