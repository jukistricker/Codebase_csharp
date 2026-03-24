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

    public async Task<IResult> CreatePermissionGroupAsync(PermissionGroupSaveRequest request)
    {
        var entity = request.ToEntity();
        entity.Id = Guid.Empty;
        entity = await _rbacRepo.SavePermissionGroupAsync(entity, false);
        return ResponseDto.Create(ResponseCatalog.Created, "rbac.permission_group.created", entity);
    }

    public async Task<IResult> UpdatePermissionGroupAsync(PermissionGroupSaveRequest request)
    {
        if (!request.Id.HasValue || request.Id == Guid.Empty)
            return ResponseDto.Create(ResponseCatalog.BadRequest, "rbac.permission_group.id_required");

        var entity = request.ToEntity();
        entity = await _rbacRepo.SavePermissionGroupAsync(entity, true);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission_group.updated", entity);
    }


    public async Task<IResult> SearchPermissionGroupsAsync(PermissionGroupFilterRequest request)
    {
        var (items, nextCursor) = await _rbacRepo.GetPermissionGroupsAsync(request);

        var response = new PagedResponse<PermissionGroupResponse>(items, nextCursor);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission_groups.list", response);
    }

    public async Task<IResult> CreateRoleAsync(RoleSaveRequest request)
    {
        var entity = request.ToEntity();
        entity.Id = Guid.Empty;
        entity = await _rbacRepo.SaveRoleAsync(entity, false);
        return ResponseDto.Create(ResponseCatalog.Created, "rbac.role.created", entity);
    }

    public async Task<IResult> UpdateRoleAsync(RoleSaveRequest request)
    {
        if (!request.Id.HasValue || request.Id == Guid.Empty)
            return ResponseDto.Create(ResponseCatalog.BadRequest, "rbac.role.id_required");

        var entity = request.ToEntity();
        entity = await _rbacRepo.SaveRoleAsync(entity, true);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.role.updated", entity);
    }

    public async Task<IResult> SearchRolesAsync(RoleFilterRequest request)
    {
        var (items, nextCursor) = await _rbacRepo.GetRolesAsync(request);

        var response = new PagedResponse<Role>(items, nextCursor);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.role.list", response);
    }

    public async Task<IResult> CreatePermissionAsync(List<PermissionSaveRequest> requests)
    {
        // 1. Thu thập dữ liệu đối chiếu (Thêm check null cho RoleId)
        var codesToCheck = requests.Select(r => r.Code).Distinct().ToList();

        var groupIdsToCheck = requests
            .Where(r => r.PermissionGroupId.HasValue)
            .Select(r => r.PermissionGroupId!.Value)
            .Distinct()
            .ToList();

        // Dùng ?? new List<Guid>() để SelectMany không bị nổ khi gặp null
        var roleIdsToCheck = requests
            .SelectMany(r => r.RoleId ?? new List<Guid>())
            .Distinct()
            .ToList();

        // 2. Một lượt quét Database
        var existingCodes = await _rbacRepo.ValidPermissionCodes(codesToCheck);
        var validGroupIds = await _rbacRepo.ValidPermissionGroups(groupIdsToCheck);
        var validRoleIds = roleIdsToCheck.Any()
            ? await _rbacRepo.ValidRoles(roleIdsToCheck)
            : new List<Guid>();

        // 3. Duyệt lỗi logic
        var errorList = new List<object>();
        var duplicateInRequest = requests.GroupBy(x => x.Code).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        foreach (var req in requests)
        {
            var itemErrors = new List<string>();

            if (existingCodes.Contains(req.Code)) itemErrors.Add("rbac.permission.code_already_exists");
            if (duplicateInRequest.Contains(req.Code)) itemErrors.Add("rbac.permission.duplicate_code");

            // Check null PermissionGroupId trước khi Contains
            if (!req.PermissionGroupId.HasValue || !validGroupIds.Contains(req.PermissionGroupId.Value))
                itemErrors.Add("rbac.permission_group.not_found");

            // Kiểm tra RoleId: Chỉ check nếu RoleId không null và có phần tử
            if (req.RoleId != null && req.RoleId.Any(id => !validRoleIds.Contains(id)))
                itemErrors.Add("rbac.role.one_or_more_not_found");

            if (itemErrors.Any())
                errorList.Add(new { req.Code, Errors = itemErrors });
        }

        if (errorList.Any())
            return ResponseDto.Create(ResponseCatalog.BadRequest, "rbac.permission.save_failed", errorList);

        // 4. Mapping
        var permissions = new List<Permission>();
        var rolePermissions = new List<RolePermission>();

        foreach (var req in requests)
        {
            var pId = req.Id ?? Guid.CreateVersion7();
            permissions.Add(new Permission
            {
                Id = pId,
                Code = req.Code,
                Name = req.Name,
                PermissionGroupId = req.PermissionGroupId!.Value
            });

            // Chỉ AddRange nếu RoleId không null
            if (req.RoleId != null)
                rolePermissions.AddRange(req.RoleId.Select(rId => new RolePermission
                {
                    PermissionId = pId,
                    RoleId = rId
                }));
        }

        if (!await _rbacRepo.SavePermissionBatchAsync(permissions, rolePermissions))
            return ResponseDto.Create(ResponseCatalog.Internal, "rbac.permission.save_failed");

        return ResponseDto.Create(ResponseCatalog.Created, "rbac.permission.created");
    }

    public async Task<IResult> UpdatePermissionAsync(List<PermissionSaveRequest> requests)
    {
        // 1. Thu thập dữ liệu để check
        var idsInRequest = requests.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToList();
        var codesInRequest = requests.Select(r => r.Code).Distinct().ToList();

        // Thu thập GroupId (Xử lý null để tránh lỗi Select)
        var groupIdsToCheck = requests
            .Where(r => r.PermissionGroupId.HasValue)
            .Select(r => r.PermissionGroupId!.Value)
            .Distinct().ToList();

        // Thu thập RoleId (Xử lý null để SelectMany không crash)
        var roleIdsToCheck = requests
            .SelectMany(r => r.RoleId ?? new List<Guid>())
            .Distinct().ToList();

        // 2. Quét DB một lần duy nhất
        var existingInDb = await _rbacRepo.GetPermissionsByIds(idsInRequest);
        var existingCodes = await _rbacRepo.ValidPermissionCodes(codesInRequest);
        var validGroupIds = await _rbacRepo.ValidPermissionGroups(groupIdsToCheck);
        var validRoleIds = roleIdsToCheck.Any()
            ? await _rbacRepo.ValidRoles(roleIdsToCheck)
            : new List<Guid>();

        // 3. Bắt lỗi logic
        var errorList = new List<object>();
        var duplicateInRequest = requests.GroupBy(x => x.Code).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        foreach (var req in requests)
        {
            var itemErrors = new List<string>();

            // Check tồn tại Permission
            if (req.Id.HasValue && !existingInDb.Any(p => p.Id == req.Id))
                itemErrors.Add("rbac.permission.not_found");

            // Check GroupId
            if (!req.PermissionGroupId.HasValue || !validGroupIds.Contains(req.PermissionGroupId.Value))
                itemErrors.Add("rbac.permission.group_not_found");

            // Check trùng Code (trừ chính nó)
            if (existingCodes.Contains(req.Code))
            {
                var isOwnCode = req.Id.HasValue && existingInDb.Any(p => p.Id == req.Id && p.Code == req.Code);
                if (!isOwnCode) itemErrors.Add("rbac.permission.code_already_exists");
            }

            if (duplicateInRequest.Contains(req.Code))
                itemErrors.Add("rbac.permission.duplicate_code");

            // Validate RoleId: Chỉ check nếu RoleId có dữ liệu (Nếu null hoặc rỗng thì bỏ qua vì ta chấp nhận xóa hết)
            if (req.RoleId != null && req.RoleId.Any(id => !validRoleIds.Contains(id)))
                itemErrors.Add("rbac.role.one_or_more_not_found");

            if (itemErrors.Any()) errorList.Add(new { req.Code, Errors = itemErrors });
        }

        if (errorList.Any())
            return ResponseDto.Create(ResponseCatalog.BadRequest, "rbac.permission.save_failed", errorList);

        // 4. Phân loại để Upsert
        var permissionsToSave = new List<Permission>();
        var rolePermissionsToSave = new List<RolePermission>();

        foreach (var req in requests)
        {
            var isUpdate = req.Id.HasValue && existingInDb.Any(p => p.Id == req.Id);
            var pId = isUpdate ? req.Id!.Value : req.Id ?? Guid.CreateVersion7();

            permissionsToSave.Add(new Permission
            {
                Id = pId,
                Code = req.Code,
                Name = req.Name,
                PermissionGroupId = req.PermissionGroupId!.Value
            });

            // Ánh xạ Role: Nếu RoleId != null, thêm vào list re-insert. 
            // Nếu RoleId == null hoặc rỗng, không thêm gì (tức là sau khi xóa cũ sẽ không có gì mới)
            if (req.RoleId != null && req.RoleId.Any())
                rolePermissionsToSave.AddRange(req.RoleId.Select(rId => new RolePermission
                {
                    PermissionId = pId,
                    RoleId = rId
                }));
        }

        // 5. Lưu xuống Repo: idsInRequest chứa tất cả các ID cần được "làm sạch" bảng trung gian
        if (!await _rbacRepo.UpsertPermissionsBatchAsync(idsInRequest, permissionsToSave, rolePermissionsToSave))
            return ResponseDto.Create(ResponseCatalog.Internal, "rbac.permission.save_failed");

        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission.update_success");
    }

    public async Task<IResult> SearchPermissionsAsync(PermissionFilterRequest request)
    {
        var (items, nextCursor) = await _rbacRepo.GetPermissionsAsync(request);

        var response = new PagedResponse<PermissionResponse>(items, nextCursor);
        return ResponseDto.Create(ResponseCatalog.Success, "rbac.permission.list", response);
    }
}