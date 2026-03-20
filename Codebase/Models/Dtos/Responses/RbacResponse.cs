using Codebase.Models.Dtos.Responses.Shared;

namespace Codebase.Models.Dtos.Responses;

public class PermissionGroupResponse:BaseResponse
{
    public string Name { get; set; } = null!; 
    public string Code { get; set; } = null!; 
    public int SortOrder { get; set; }        
}