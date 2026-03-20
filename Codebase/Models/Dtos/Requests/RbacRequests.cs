using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests.Search;

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

public class PermissionGroupFilterRequest : BaseFilterRequest
{
    // Bạn có thể thêm các filter cứng ở đây nếu cần
    // ví dụ: public bool? IsActive { get; set; }
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
}

