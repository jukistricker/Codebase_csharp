using Codebase.Contexts;
using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests.RBAC;
using Codebase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Codebase.Repositories;

public class RbacRepository : IRbacRepository
{
    
    private readonly AppDbContext _context;

    public RbacRepository(AppDbContext context) => _context = context;

    // public async Task<PermissionGroupDetailDto?> GetGroupPermissionDetailAsync(Guid groupPermissionId)
    // {
    //     // Sử dụng Query Projection để tránh lỗi Ambiguous và tối ưu RAM
    //     IQueryable<PermissionGroupDetailDto> query = _context.PermissionGroups
    //         .AsNoTracking()
    //         .Where(g => g.Id == groupPermissionId)
    //         .Select(g => new PermissionGroupDetailDto(
    //             g.Id, 
    //             g.Name, 
    //             g.Code,
    //             _context.Permissions
    //                 .Where(p => p.PermissionGroupId == g.Id)
    //                 .Select(p => new PermissionDto(p.Id, p.Code))
    //                 .ToList() 
    //         ));
    //
    //     return await query.FirstOrDefaultAsync();
    // }

    public async Task<bool> CheckGroupCodeExistsAsync(string code, Guid? excludeId = null)
    {
        return await _context.PermissionGroups
            .AsNoTracking()
            .AnyAsync(g => g.Code == code && (!excludeId.HasValue || g.Id != excludeId.Value));
    }
    
    public async Task<PermissionGroup> SavePermissionGroupAsync(PermissionGroup entity, bool isUpdate)
    {
        if (isUpdate)
        {
            // Chỉ Attach và đánh dấu Modified (0 Roundtrip SELECT)
            _context.PermissionGroups.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            _context.PermissionGroups.Add(entity);
        }

        // Đẩy xuống DB 
        // Lỗi Unique/Concurrency sẽ sinh tại đây và bay thẳng lên GEH
        await _context.SaveChangesAsync();
        return entity;
    }

  
    // public async Task<RoleDetailDto?> GetRoleDetailAsync(Guid roleId)
    // {
    //     return await _context.Roles
    //         .AsNoTracking()
    //         .Where(r => r.Id == roleId)
    //         .Select(r => new RoleDetailDto(
    //             r.Id, 
    //             r.Name,
    //             _context.RolePermissions
    //                 .Where(rp => rp.RoleId == r.Id)
    //                 .Select(rp => new PermissionDto(rp.PermissionId, rp.Permission.Code))
    //                 .ToList()
    //         )).FirstOrDefaultAsync();
    // }

    public async Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds)
    {
        // Xóa cũ bằng ExecuteDelete 
        await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ExecuteDeleteAsync();

        // Thêm mới
        if (permissionIds.Any())
        {
            var newItems = permissionIds.Select(pId => new RolePermission { 
                RoleId = roleId, 
                PermissionId = pId 
            });
            await _context.RolePermissions.AddRangeAsync(newItems);
            await _context.SaveChangesAsync();
        }
    }
    
    public Task Update<T>(T entity) where T : class
    { 
        _context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }
    
    public async Task AddAsync<T>(T entity) where T : class 
        => await _context.Set<T>().AddAsync(entity);

    public void Delete<T>(T entity) where T : class 
        => _context.Set<T>().Remove(entity);

    public async Task<bool> SaveChangesAsync() 
        => await _context.SaveChangesAsync() > 0;

    public async Task<List<PermissionGroup>> GetAllGroupsAsync()
        => await _context.PermissionGroups.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync();
    
}