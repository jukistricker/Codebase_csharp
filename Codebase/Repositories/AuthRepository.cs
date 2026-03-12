using Codebase.Contexts;
using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Responses.Auth;
using Codebase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Codebase.Repositories;

public class AuthRepository: IAuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> UsernameExistsAsync(string username)
        => _db.Users.AnyAsync(u => u.Username == username);

    public Task<User?> GetByUsernameAsync(string username)
        => _db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<Guid?> GetDefaultRoleIdAsync()
    {
        return await _db.Roles
            .Where(r => r.Name == "user")
            .Select(r => r.Id)
            .SingleOrDefaultAsync();
    }

    public async Task<List<Guid>> GetExistingRoleIdsAsync(HashSet<Guid> roleIds)
    {
        return await _db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<HashSet<Guid>> GetUserRoleIdsAsync(Guid userId)
    {
        return (await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync())
            .ToHashSet();
    }

    public async Task<HashSet<string>> GetUserPermissionCodesAsync(Guid userId)
    {
        return (await (from ur in _db.UserRoles
                    join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                    join p in _db.Permissions on rp.PermissionId equals p.Id
                    where ur.UserId == userId
                    select p.Code)
                .Distinct()
                .ToListAsync())
            .ToHashSet();
    }
    
    public async Task<UserFullInfo?> GetFullUserInfoAsync(string username)
    {
        var data = await _db.Users
            .Where(u => u.Username == username)
            .Select(u => new 
            {
                User = u,
                RoleIds = _db.UserRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.RoleId).ToList(),
                Permissions = (from ur in _db.UserRoles
                    join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                    join p in _db.Permissions on rp.PermissionId equals p.Id
                    where ur.UserId == u.Id
                    select p.Code).Distinct().ToList()
            })
            .FirstOrDefaultAsync();

        if (data == null) return null;

        return new UserFullInfo(data.User, data.RoleIds.ToHashSet(), data.Permissions.ToHashSet());
    }

    public Task AddUserAsync(User user)
        => _db.Users.AddAsync(user).AsTask();

    public Task AddUserRolesAsync(IEnumerable<UserRole> roles)
        => _db.UserRoles.AddRangeAsync(roles);

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}