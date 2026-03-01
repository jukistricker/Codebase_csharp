namespace Codebase.Entities.Auth;

public class Permission:BaseEntity
{
    public string Code { get; set; }
    public Guid PermissionGroupId { get; set; }
}