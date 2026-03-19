using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests.RBAC;

namespace Codebase.Repositories.Interfaces;

public interface IRbacRepository
{
    // Queries
    Task<List<PermissionGroup>> GetAllGroupsAsync();
    Task<PermissionGroupDetailDto?> GetGroupPermissionDetailAsync(Guid groupId);
    Task<bool> CheckGroupCodeExistsAsync(string code, Guid? excludeId = null);
    
    // Commands
    Task Update<T>(T entity) where T : class;
    Task AddAsync<T>(T entity) where T : class;
    void Delete<T>(T entity) where T : class;
    Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds);
    Task<bool> SaveChangesAsync();
}