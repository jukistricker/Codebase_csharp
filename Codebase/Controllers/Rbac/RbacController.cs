using System.Runtime.CompilerServices;
using Codebase.Attributes;
using Codebase.Models.Dtos.Requests.RBAC;
using Codebase.Services.Interfaces.Rbac;
using Microsoft.AspNetCore.Mvc;

namespace Codebase.Controllers.Rbac;

[ApiController]
[Route("rbac")]
public class RbacController:ControllerBase
{
    private readonly IRbacService _rbacService;

    public RbacController(IRbacService rbacService)
    {
        _rbacService = rbacService;
    }

    [HttpPost("")]
    [RequiredPermission("rbac.save_permission_group")]
    public async Task<IResult> SavePermission([FromBody] PermissionGroupPostRequest request)
    {
        return await _rbacService.SavePermissionGroupAsync(request);
    }
    
}