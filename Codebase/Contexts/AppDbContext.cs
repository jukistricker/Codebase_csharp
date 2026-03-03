using Codebase.Entities;
using Codebase.Entities.Auth;
using Codebase.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Codebase.Contexts;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        

        // Định nghĩa khóa chính tổ hợp cho RolePermission
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Định nghĩa khóa chính tổ hợp cho UserRole
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
        
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Đổi tên Table: PermissionGroup -> permission_groups
            // (Lưu ý: EF thường tự thêm 's', nếu SQL của bạn là số ít thì dùng entity.ClrType.Name)
            var tableName = ToSnakeCase(entity.GetTableName() ?? entity.ClrType.Name);
            entity.SetTableName(tableName);

            foreach (var property in entity.GetProperties())
            {
                // Đổi tên Column: PermissionGroupId -> permission_group_id
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }
    
    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return string.Concat(input.Select((x, i) =>
                i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString()))
            .ToLower();
    }

    public override int SaveChanges()
    {
        ApplyAuditLog();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ApplyAuditLog();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditLog()
{
    var now = DateTimeOffset.UtcNow;
    var rawUserId = HttpContextUtil.CurrentUserId; // chuỗi string từ token

    // Chỉ lọc những thực thể kế thừa IAuditEntity
    foreach (var entry in ChangeTracker.Entries<IAuditEntity>())
    {
        // 1. Cập nhật thời gian (Common cho cả Add và Update)
        entry.Entity.UpdatedAt = now;

        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAt = now;

            // 2. Xử lý ID tự động (Chỉ gán nếu là Guid và đang trống)
            var idProp = entry.Property("Id");
            if (idProp.Metadata.ClrType == typeof(Guid) && (Guid)idProp.CurrentValue! == Guid.Empty)
            {
                idProp.CurrentValue = Guid.CreateVersion7();
            }

            // 3. Gán người tạo/người sửa
            SetAuditUser(entry, "CreatedBy", rawUserId, true);
            SetAuditUser(entry, "UpdatedBy", rawUserId, false);
        }
        else if (entry.State == EntityState.Modified)
        {
            // 4. Chỉ gán người sửa khi cập nhật
            SetAuditUser(entry, "UpdatedBy", rawUserId, false);
            
            // Bảo vệ các trường không cho phép sửa
            entry.Property("CreatedAt").IsModified = false;
            entry.Property("CreatedBy").IsModified = false;
        }
    }
}

private void SetAuditUser(EntityEntry entry, string propName, string? rawUserId, bool isNew)
{
    var prop = entry.Property(propName);
    var targetType = prop.Metadata.ClrType;

    // A. Nếu có Token (User đã đăng nhập)
    if (!string.IsNullOrEmpty(rawUserId))
    {
        // Chuyển đổi linh hoạt sang bất kỳ kiểu dữ liệu nào (Guid, int, long, string)
        object convertedId = targetType == typeof(Guid) 
            ? Guid.Parse(rawUserId) 
            : Convert.ChangeType(rawUserId, targetType);
            
        prop.CurrentValue = convertedId;
    }
    // B. Nếu không có Token (Trường hợp SignUp)
    else if (isNew && propName == "CreatedBy")
    {
        // Gán CreatedBy = Id của chính bản ghi đó
        // Cách này hoạt động cho mọi kiểu dữ liệu của Id
        prop.CurrentValue = entry.Property("Id").CurrentValue;
    }
}
}