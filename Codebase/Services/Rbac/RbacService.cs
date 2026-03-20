using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests.RBAC;
using Codebase.Models.Dtos.Responses.Shared;
using Codebase.Repositories.Interfaces;
using Codebase.Services.Interfaces.Rbac;

namespace Codebase.Services.Rbac;

public class RbacService : IRbacService
{
    private readonly IRbacRepository _repo;

    public RbacService(IRbacRepository repo)
    {
        _repo = repo;
    }

    public async Task<IResult> SavePermissionGroupAsync(PermissionGroupPostRequest request)
    {
        bool isUpdate = request.Id.HasValue && request.Id != Guid.Empty;
        
        PermissionGroup entity = request.ToEntity();
        
        entity= await _repo.SavePermissionGroupAsync(entity, isUpdate);

        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission_group.saved",entity);
    }

    public Task<IResult> SearchPermissionGroupsAsync()
    {
        throw new NotImplementedException();
    }
}