namespace Codebase.Models.Dtos.Requests.RBAC;

// Permission & Group
public class PermissionGroupPostRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public int SortOrder { get; set; }
}
