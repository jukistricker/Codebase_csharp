using System.Linq.Dynamic.Core;
using Codebase.Contexts;
using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests;
using Codebase.Models.Dtos.Requests.Search;
using Codebase.Models.Dtos.Responses;
using Codebase.Repositories.Interfaces;
using Codebase.Utils;
using Microsoft.EntityFrameworkCore;

namespace Codebase.Repositories;

public class RbacRepository : IRbacRepository
{
    
    private readonly AppDbContext _db;
    private readonly ILogger<RbacRepository> _logger;

    public RbacRepository(AppDbContext context, ILogger<RbacRepository> logger)
    {
        _db = context;
        _logger = logger;
    }

    // public async Task<PermissionGroupDetailDto?> GetGroupPermissionDetailAsync(Guid groupPermissionId)
    // {
    //     // Sử dụng Query Projection để tránh lỗi Ambiguous và tối ưu RAM
    //     IQueryable<PermissionGroupDetailDto> query = _db.PermissionGroups
    //         .AsNoTracking()
    //         .Where(g => g.Id == groupPermissionId)
    //         .Select(g => new PermissionGroupDetailDto(
    //             g.Id, 
    //             g.Name, 
    //             g.Code,
    //             _db.Permissions
    //                 .Where(p => p.PermissionGroupId == g.Id)
    //                 .Select(p => new PermissionDto(p.Id, p.Code))
    //                 .ToList() 
    //         ));
    //
    //     return await query.FirstOrDefaultAsync();
    // }

    public async Task<bool> CheckGroupCodeExistsAsync(string code, Guid? excludeId = null)
    {
        return await _db.PermissionGroups
            .AsNoTracking()
            .AnyAsync(g => g.Code == code && (!excludeId.HasValue || g.Id != excludeId.Value));
    }
    
    public async Task<PermissionGroup> SavePermissionGroupAsync(PermissionGroup entity, bool isUpdate)
    {
        if (isUpdate)
        {
            // Chỉ Attach và đánh dấu Modified (0 Roundtrip SELECT)
            _db.PermissionGroups.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            _db.PermissionGroups.Add(entity);
        }
        // Đẩy xuống DB 
        // Lỗi Unique/Concurrency sẽ sinh tại đây và bay thẳng lên GEH
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<(List<PermissionGroupResponse> Items, string? NextCursor)> GetPermissionGroupsAsync(PermissionGroupFilterRequest req)
    {
        IQueryable<PermissionGroup> query = _db.PermissionGroups.AsNoTracking();

        if (req.Id.HasValue) query = query.Where(u => u.Id == req.Id.Value);
        if (!string.IsNullOrWhiteSpace(req.Code)) query = query.Where(u => u.Code == req.Code);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            query = query.Where(u => EF.Functions.ILike(u.Name, $"%{req.Search.Trim()}%"));
        }
        
        List<PermissionGroupResponse> items = await query
            .ApplyCursor<PermissionGroup, Guid>(req.Cursor, req.SortField, req.IsDescending) 
            .ApplyDeterministicSort(req.FullSortParam)
            .Take(req.Limit + 1)
            .ApplySelect<PermissionGroup, PermissionGroupResponse>(StringUtil.GetSelectFields<PermissionGroupResponse>(req.Select))
            .ToListAsync();

        string? nextCursor = null;
        if (items.Count > req.Limit)
        {
            PermissionGroupResponse lastValidItem = items[req.Limit - 1];
            nextCursor = lastValidItem.GetType().GetProperty(req.SortField)?.GetValue(lastValidItem)?.ToString() 
                         ?? lastValidItem.Id.ToString();
            
            items.RemoveAt(req.Limit);
        }
        return (items, nextCursor);
    }




    // public async Task<RoleDetailDto?> GetRoleDetailAsync(Guid roleId)
    // {
    //     return await _db.Roles
    //         .AsNoTracking()
    //         .Where(r => r.Id == roleId)
    //         .Select(r => new RoleDetailDto(
    //             r.Id, 
    //             r.Name,
    //             _db.RolePermissions
    //                 .Where(rp => rp.RoleId == r.Id)
    //                 .Select(rp => new PermissionDto(rp.PermissionId, rp.Permission.Code))
    //                 .ToList()
    //         )).FirstOrDefaultAsync();
    // }

    public async Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds)
    {
        // Xóa cũ bằng ExecuteDelete 
        await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ExecuteDeleteAsync();

        // Thêm mới
        if (permissionIds.Any())
        {
            var newItems = permissionIds.Select(pId => new RolePermission { 
                RoleId = roleId, 
                PermissionId = pId 
            });
            await _db.RolePermissions.AddRangeAsync(newItems);
            await _db.SaveChangesAsync();
        }
    }
    
    public async Task<bool> SavePermissionBatchAsync(List<Permission> permissions, List<RolePermission> rolePermissions)
    {
        try 
        {
            await _db.Permissions.AddRangeAsync(permissions);
            await _db.RolePermissions.AddRangeAsync(rolePermissions);
            return await _db.SaveChangesAsync() > 0;
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Internal Server Error. Details: {Message}", 
                ex.InnerException?.Message ?? ex.Message);
            // Dọn dẹp tracker nếu lưu thất bại để tránh ảnh hưởng các lệnh Save sau này
            _db.ChangeTracker.Clear(); 
            return false; 
        }
    }
    
    public async Task<List<string>> ValidPermissionCodes(List<string> codes)
    {
        return await _db.Permissions
            .Where(p => codes.Contains(p.Code))
            .Select(p => p.Code).ToListAsync();
    }
    
    public async Task<List<Guid>> ValidPermissionGroups(List<Guid> groupIds)
    {
        return await _db.PermissionGroups
            .Where(g => groupIds.Contains(g.Id))
            .Select(g => g.Id).ToListAsync();
    }
    
    public async Task<List<Guid>> ValidRoles(List<Guid> roleIds)
    {
        return await _db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id).ToListAsync();
    }
    
    public Task Update<T>(T entity) where T : class
    { 
        _db.Set<T>().Update(entity);
        return Task.CompletedTask;
    }
    
    public async Task AddAsync<T>(T entity) where T : class 
        => await _db.Set<T>().AddAsync(entity);

    public void Delete<T>(T entity) where T : class 
        => _db.Set<T>().Remove(entity);

    public async Task<bool> SaveChangesAsync() 
        => await _db.SaveChangesAsync() > 0;

    public async Task<Role> SaveRoleAsync(Role entity, bool isUpdate)
    {
        if (isUpdate)
        {
            _db.Roles.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            _db.Roles.Add(entity);
        }
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<(List<Role> Items, string? NextCursor)> GetRolesAsync(RoleFilterRequest requests)
    {
        IQueryable<Role> query = _db.Roles.AsNoTracking();

        if (requests.Id.HasValue) query = query.Where(u => u.Id == requests.Id.Value);
        if (!string.IsNullOrWhiteSpace(requests.Search))
        {
            query = query.Where(u => EF.Functions.ILike(u.Name, $"%{requests.Search.Trim()}%"));
        }
        
        List<Role> items = await query
            .ApplyCursor<Role, Guid>(requests.Cursor, requests.SortField, requests.IsDescending) 
            .ApplyDeterministicSort(requests.FullSortParam)
            .Take(requests.Limit + 1)
            .ApplySelect<Role, Role>(StringUtil.GetSelectFields<Role>(requests.Select))
            .ToListAsync();

        string? nextCursor = null;
        if (items.Count > requests.Limit)
        {
            Role lastValidItem = items[requests.Limit - 1];
            nextCursor = lastValidItem.GetType().GetProperty(requests.SortField)?.GetValue(lastValidItem)?.ToString() 
                         ?? lastValidItem.Id.ToString();
            
            items.RemoveAt(requests.Limit);
        }
        return (items, nextCursor);
    }

    public async Task<List<PermissionGroup>> GetAllGroupsAsync()
        => await _db.PermissionGroups.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync();
    
}