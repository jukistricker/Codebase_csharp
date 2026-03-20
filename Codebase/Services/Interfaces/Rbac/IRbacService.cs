using Codebase.Models.Dtos.Requests.RBAC;

namespace Codebase.Services.Interfaces.Rbac;

public interface IRbacService
{
    Task<IResult> SavePermissionGroupAsync(PermissionGroupPostRequest request);
    Task<IResult> SearchPermissionGroupsAsync();
}