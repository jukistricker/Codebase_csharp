using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests.RBAC;
using Codebase.Models.Dtos.Responses.Shared;
using Codebase.Repositories.Interfaces;
using Codebase.Services.Interfaces.Rbac;

namespace Codebase.Services.Rbac;

public class RbacService : IRbacService
{
    private readonly IRbacRepository _repo;
    private readonly IHttpContextAccessor _accessor;

    public RbacService(IRbacRepository repo, IHttpContextAccessor accessor)
    {
        _repo = repo;
        _accessor = accessor;
    }


    public async Task<IResult> SavePermissionGroupAsync(PermissionGroupPostRequest request)
    {
        PermissionGroup group = new PermissionGroup
        {
            Name = request.Name,
            Code = request.Code,
            SortOrder = request.SortOrder
        };
        if(request.Id!= null||request.Id != Guid.Empty)
        {
            if(await _repo.CheckGroupCodeExistsAsync(request.Code,request.Id))
                return ResponseDto.Create(ResponseCatalog.BadRequest,"rbac.permission.group_code_exists");
            group.Id = request.Id.Value;
            _repo.Update(group);
        }
        else
        {
            if(await _repo.CheckGroupCodeExistsAsync(request.Code))
                return ResponseDto.Create(ResponseCatalog.BadRequest,"rbac.permission.group_code_exists");
        }
        
    }
}