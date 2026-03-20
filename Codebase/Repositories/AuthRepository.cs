using Codebase.Contexts;
using System.Linq.Dynamic.Core;
using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests;
using Codebase.Models.Dtos.Responses;
using Codebase.Repositories.Interfaces;
using Codebase.Utils;
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
    
    public async Task<UserFullInfo> GetFullUserInfoAsync(string username)
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
    
    public async Task<(List<UserResponse> Items, string? NextCursor)> GetUsersAsync(AuthFilterRequest req)
    {
        // 1. Khởi tạo Query
        IQueryable<User> query = _db.Users.AsNoTracking();
        
        if (req.Id.HasValue)
        {
            query = query.Where(u => u.Id == req.Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.Username))
        {
            query = query.Where(u => u.Username == req.Username);
        }

        // 2. Search thủ công (Tự cấu hình theo cột có Index)
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            string term = req.Search.Trim();
            query = query.Where(u => EF.Functions.ILike(u.FullName, $"%{term}%"));
        }

        // 3. Chuẩn hóa Sort
        string? sortField = req.Sort?.StartsWith("-") == true 
            ? "-" + StringUtil.ToPascalCase(req.Sort.TrimStart('-')) 
            : StringUtil.ToPascalCase(req.Sort);

        // 4. Lấy SelectFields động dựa trên Class UserResponse
        // Nó sẽ tự sinh ra: "Id, Username, Email, FullName..." dựa trên các property bạn khai báo trong DTO
        string selectFields = StringUtil.GetSelectFields<UserResponse>(req.Select);

        // 5. Thực thi Query
        var items = await query
            .ApplyCursor<User, Guid>(req.Cursor, req.Limit, sortField)
            .Select<UserResponse>($"new({selectFields})") 
            .ToListAsync();

        // 6. Xác định Next Cursor (Dành cho Endless Scroll)
        string? nextCursor = null;
        if (items.Count > req.Limit)
        {
            // Lấy Id của bản ghi cuối cùng (bản ghi thứ Limit)
            nextCursor = items[req.Limit - 1].Id.ToString();
            // Xóa bản ghi thừa (bản ghi thứ Limit + 1 dùng để check trang tiếp)
            items.RemoveAt(req.Limit);
        }

        return (items, nextCursor);
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}