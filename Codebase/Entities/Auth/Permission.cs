namespace Codebase.Entities.Auth;

public class Permission:BaseEntity
{
    public string Name { get; set; }
    public string Code { get; set; }
    public Guid PermissionGroupId { get; set; }
}