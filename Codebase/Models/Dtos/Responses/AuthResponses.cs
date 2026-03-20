using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Responses.Shared;

namespace Codebase.Models.Dtos.Responses;

public record UserFullInfo(
    User User, 
    HashSet<Guid> RoleIds, 
    HashSet<string> Permissions
    );
    
public class UserResponse:BaseResponse
{
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Provider { get; set; }
} 