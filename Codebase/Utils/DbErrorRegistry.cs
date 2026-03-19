namespace Codebase.Utils;

public static class DatabaseErrorRegistry
{
    /** TODO: Tên key phải trùng với tên constraint trong DB để có thể map chính xác lỗi
        Key: Tên Constraint trong DB | Value: Thông điệp lỗi trả về cho Client **/
    private static readonly Dictionary<string, string> _mappers = new()
    {
        // Permission Groups
        { "permission_groups_code_key", "rbac.permission_group.code_exists" },
        
        
        // Permissions
        { "permissions_code_key", "rbac.permission.code_exists" },
        // Foreign Keys (check lỗi not exists khi insert/update)
        { "permissions_permission_group_id_fkey", "rbac.permission.group_not_found" },
        
        // Roles
        { "roles_name_key", "rbac.role.name_exists" },
        
        // Users
        { "users_username_key", "rbac.user.username_exists" },

        
    };

    public static string? GetErrorMessage(string? constraintName)
    {
        if (string.IsNullOrEmpty(constraintName)) return null;
        return _mappers.GetValueOrDefault(constraintName);
    }
}