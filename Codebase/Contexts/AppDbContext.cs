using Codebase.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Codebase.Contexts;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public override int SaveChanges()
    {
        ApplyUuidV7();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ApplyUuidV7();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyUuidV7()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added)
                continue;

            var idProp = entry.Properties
                .FirstOrDefault(p =>
                    p.Metadata.Name == "Id" &&
                    p.Metadata.ClrType == typeof(Guid));

            if (idProp == null)
                continue;

            if ((Guid)idProp.CurrentValue! == Guid.Empty)
            {
                idProp.CurrentValue = Guid.CreateVersion7();
            }
        }
    }
}