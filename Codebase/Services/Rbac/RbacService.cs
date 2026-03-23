using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests;
using Codebase.Models.Dtos.Requests.Search;
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

    public async Task<IResult> CreatePermissionGroupAsync(PermissionGroupSaveRequest request)
    {
        PermissionGroup entity = request.ToEntity();
        request.Id = Guid.Empty;
        entity= await _rbacRepo.SavePermissionGroupAsync(entity, false);
        return ResponseDto.Create(ResponseCatalog.Created, "rbac.permission_group.created",entity);
    }
    
    public async Task<IResult> UpdatePermissionGroupAsync(PermissionGroupSaveRequest request)
    {
        if(!request.Id.HasValue || request.Id == Guid.Empty)
            return ResponseDto.Create(ResponseCatalog.BadRequest,"rbac.permission_group.id_required");
        
        PermissionGroup entity = request.ToEntity();
        entity= await _rbacRepo.SavePermissionGroupAsync(entity, true);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission_group.updated",entity);
    }
    

    public async Task<IResult> SearchPermissionGroupsAsync(PermissionGroupFilterRequest request)
    {
        var (items, nextCursor) = await _rbacRepo.GetPermissionGroupsAsync(request);

        PagedResponse<PermissionGroupResponse> response= new PagedResponse<PermissionGroupResponse>(items, nextCursor);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission_groups_list", response);
    }

    public async Task<IResult> CreateRoleAsync(RoleSaveRequest request)
    {
        Role entity = request.ToEntity();
        request.Id = Guid.Empty;
        entity= await _rbacRepo.SaveRoleAsync(entity, false);
        return ResponseDto.Create(ResponseCatalog.Created, "rbac.role.created",entity);
    }

    public async Task<IResult> UpdateRoleAsync(RoleSaveRequest request)
    {
        if(!request.Id.HasValue || request.Id == Guid.Empty)
            return ResponseDto.Create(ResponseCatalog.BadRequest,"rbac.role.id_required");
        
        Role entity = request.ToEntity();
        entity= await _rbacRepo.SaveRoleAsync(entity, true);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.role.updated",entity);
    }

    public async Task<IResult> SearchRolesAsync(RoleFilterRequest request)
    {
        var (items, nextCursor) = await _rbacRepo.GetRolesAsync(request);

        PagedResponse<Role> response= new PagedResponse<Role>(items, nextCursor);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.role_list", response);
    }
}