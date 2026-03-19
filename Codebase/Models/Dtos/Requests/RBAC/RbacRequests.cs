using Codebase.Entities.Auth;

namespace Codebase.Models.Dtos.Requests.RBAC;

// Permission & Group
public class PermissionGroupPostRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public int SortOrder { get; set; }
    
    public PermissionGroup ToEntity()
    {
        return new PermissionGroup
        {
            Id = Id ?? Guid.Empty, 
            Name = Name,
            Code = Code,
            SortOrder = SortOrder
        };
    }
    
}

