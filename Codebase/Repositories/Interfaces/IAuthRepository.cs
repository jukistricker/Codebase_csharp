using Codebase.Entities.Auth;

namespace Codebase.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<bool> UsernameExistsAsync(string username);
    Task<User?> GetByUsernameAsync(string username);

    Task<Guid?> GetDefaultRoleIdAsync();
    Task<List<Guid>> GetExistingRoleIdsAsync(HashSet<Guid> roleIds);

    Task<HashSet<Guid>> GetUserRoleIdsAsync(Guid userId);
    Task<HashSet<string>> GetUserPermissionCodesAsync(Guid userId);

    Task AddUserAsync(User user);
    Task AddUserRolesAsync(IEnumerable<UserRole> roles);

    Task SaveChangesAsync();
}