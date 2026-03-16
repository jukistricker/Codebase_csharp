namespace Codebase.Models.Dtos.Requests.RBAC;

// --- Permission Group ---
public record PermissionGroupDto(Guid Id, string Name, string Code, int SortOrder);
public record PermissionGroupDetailDto(Guid Id, string Name, string Code, List<PermissionDto> Permissions);
public record CreatePermissionGroupDto(string Name, string Code, int SortOrder);

// --- Permission ---
public record PermissionDto(Guid Id, string Code, Guid PermissionGroupId);
public record CreatePermissionDto(string Code, Guid PermissionGroupId);

// --- Role ---
public record RoleDto(Guid Id, string Name);
public record RoleDetailDto(Guid Id, string Name, List<PermissionDto> Permissions);
public record CreateRoleDto(string Name, List<Guid> PermissionIds); // Gán quyền ngay khi tạo
public record UpdateRoleDto(string Name, List<Guid> PermissionIds);

