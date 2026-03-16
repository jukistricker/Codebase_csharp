namespace Codebase.Repositories.Interfaces;

public class IRBACRepository
{
    
}

public interface IRbacRepository
{
    // Permission Groups
    Task<IEnumerable<PermissionGroup>> GetGroupsAsync();
    Task<PermissionGroup?> GetGroupByIdAsync(Guid id);
    
    // Roles
    Task<Role?> GetRoleByIdAsync(Guid id);
    Task<List<Permission>> GetPermissionsByRoleIdAsync(Guid roleId);
    
    // Command
    Task AddAsync<T>(T entity) where T : class;
    void Delete<T>(T entity) where T : class;
    Task SaveChangesAsync();
    
    // Bulk Assignment
    Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds);
}