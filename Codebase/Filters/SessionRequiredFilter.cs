using Codebase.Models.Dtos.Responses.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Codebase.Filters;

public class SessionRequiredFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            return;
        }

        if (context.HttpContext.Items["UserSession"] == null)
        {
            // Sử dụng một trick nhỏ để biến IResult thành IActionResult
            // Hoặc cách nhanh nhất là tạo mới một JsonResult/ObjectResult
            var catalog = ResponseCatalog.Unauthorized;

            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
            {
                status = catalog.Status,
                message = catalog.DefaultMessage,
                data = (object?)null
            })
            {
                StatusCode = catalog.Status
            };
        }
    }

}

