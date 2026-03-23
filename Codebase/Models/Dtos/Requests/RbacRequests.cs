using System.ComponentModel.DataAnnotations;
using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests.Search;

namespace Codebase.Models.Dtos.Requests;

// Permission & Group
public class PermissionGroupSaveRequest
{
    public Guid? Id { get; set; }
    
    [Required(ErrorMessage = "rbac.permission_group.name_required")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "rbac.permission_group.code_required")]
    public string Code { get; set; }
    
    [Range(0, 10, ErrorMessage = "rbac.permission_group.sort_order_0_10")]
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

public class PermissionGroupFilterRequest : BaseFilterRequest
{
    // Bạn có thể thêm các filter cứng ở đây nếu cần
    // ví dụ: public bool? IsActive { get; set; }
    public Guid? Id { get; set; }
    public string? Code { get; set; }
}

public class RoleSaveRequest
{
    public Guid? Id { get; set; }
    
    [Required(ErrorMessage = "rbac.role.name_required")]
    public string Name { get; set; }
    
    
    public Role ToEntity()
    {
        return new Role
        {
            Id = Id ?? Guid.Empty, 
            Name = Name,
        };
    }
}

public class RoleFilterRequest : BaseFilterRequest
{
    public Guid? Id { get; set; }
}
