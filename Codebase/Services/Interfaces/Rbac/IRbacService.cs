using Codebase.Models.Dtos.Requests;
using Codebase.Models.Dtos.Requests.Search;

namespace Codebase.Services.Interfaces.Rbac;

public interface IRbacService
{
    Task<IResult> CreatePermissionGroupAsync(PermissionGroupSaveRequest request);
    Task<IResult> UpdatePermissionGroupAsync(PermissionGroupSaveRequest request);
    Task<IResult> SearchPermissionGroupsAsync(PermissionGroupFilterRequest request);
    Task<IResult> CreateRoleAsync(RoleSaveRequest request);
    Task<IResult> UpdateRoleAsync(RoleSaveRequest request);
    Task<IResult> SearchRolesAsync(RoleFilterRequest request);
    Task<IResult> CreatePermissionAsync(List<PermissionSaveRequest> request);
    Task<IResult> UpdatePermissionAsync(List<PermissionSaveRequest> request);
}