using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests;
using Codebase.Models.Dtos.Responses;
using Codebase.Models.Dtos.Responses.Search;
using Codebase.Models.Dtos.Responses.Shared;
using Codebase.Repositories.Interfaces;
using Codebase.Services.Interfaces.Rbac;

namespace Codebase.Services.Rbac;

public class RbacService : IRbacService
{
    private readonly IRbacRepository _rbacRepo;

    public RbacService(IRbacRepository repo)
    {
        _rbacRepo = repo;
    }

    public async Task<IResult> SavePermissionGroupAsync(PermissionGroupPostRequest request)
    {
        bool isUpdate = request.Id.HasValue && request.Id != Guid.Empty;
        
        PermissionGroup entity = request.ToEntity();
        
        entity= await _rbacRepo.SavePermissionGroupAsync(entity, isUpdate);

        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission_group.saved",entity);
    }

    public async Task<IResult> SearchPermissionGroupsAsync(PermissionGroupFilterRequest request)
    {
        var (items, nextCursor) = await _rbacRepo.GetPermissionGroupsAsync(request);

        PagedResponse<PermissionGroupResponse> response= new PagedResponse<PermissionGroupResponse>(items, nextCursor);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission_groups_list", response);
    }
}