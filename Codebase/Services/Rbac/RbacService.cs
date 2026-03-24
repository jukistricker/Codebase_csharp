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
        entity.Id = Guid.Empty;
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
        entity.Id = Guid.Empty;
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

    public async Task<IResult> CreatePermissionAsync(List<PermissionSaveRequest> requests)
{
    // 1. Thu thập tất cả các ID/Code cần kiểm tra để tối ưu Round-trip
    var codesToCheck = requests.Select(r => r.Code).Distinct().ToList();
    var groupIdsToCheck = requests.Select(r => r.PermissionGroupId).Distinct().ToList();
    var roleIdsToCheck = requests.Select(r => r.RoleId).Distinct().ToList();

    // 2. Một lượt quét Database để lấy toàn bộ dữ liệu đối chiếu
    var existingCodes = await _rbacRepo.ValidPermissionCodes(codesToCheck);

    var validGroupIds = await _rbacRepo.ValidPermissionGroups(groupIdsToCheck);

    var validRoleIds = await _rbacRepo.ValidRoles(roleIdsToCheck);

    // 3. Duyệt qua từng request để "bắt lỗi"
    var errorList = new List<object>();
    var duplicateInRequest = requests.GroupBy(x => x.Code).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

    foreach (var req in requests)
    {
        var itemErrors = new List<string>();

        if (existingCodes.Contains(req.Code)) itemErrors.Add("rbac.permission.code_already_exists");
        if (duplicateInRequest.Contains(req.Code)) itemErrors.Add("rbac.request.duplicate_code");
        if (!validGroupIds.Contains(req.PermissionGroupId)) itemErrors.Add("rbac.group.not_found");
        if (!validRoleIds.Contains(req.RoleId)) itemErrors.Add("rbac.role.not_found");

        if (itemErrors.Any())
        {
            errorList.Add(new { 
                req.Code, 
                req.Name, 
                Errors = itemErrors 
            });
        }
    }

    // --- CHỐT CHẶN CUỐI CÙNG ---
    if (errorList.Any())
    {
        return Results.BadRequest(new { 
            Message = "rbac.save_failed_logic_errors", 
            Details = errorList 
        });
    }

    // 4. Nếu pass hết mới map và gọi Repo lưu 1 lần (Batch)
    var permissions = requests.Select(r => new Permission {
        Id = r.Id ?? Guid.CreateVersion7(),
        Code = r.Code,
        Name = r.Name,
        PermissionGroupId = r.PermissionGroupId
    }).ToList();

    var rolePermissions = requests.Select((r, i) => new RolePermission {
        PermissionId = permissions[i].Id,
        RoleId = r.RoleId
    }).ToList();

    if(!await _rbacRepo.SavePermissionBatchAsync(permissions, rolePermissions)
    {
        return ResponseDto.Create(ResponseCatalog.Internal,"rbac.permissionsave_failed_db_error");
    })

    return Results.Ok(new { Message = "rbac.save_success", Count = requests.Count });
}

    public async Task<IResult> UpdatePermissionAsync(List<PermissionSaveRequest> request)
    {
        throw new NotImplementedException();
    }
}