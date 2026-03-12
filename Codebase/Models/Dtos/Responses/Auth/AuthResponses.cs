using Codebase.Entities.Auth;

namespace Codebase.Models.Dtos.Responses.Auth;

public record UserFullInfo(
    User User, 
    HashSet<Guid> RoleIds, 
    HashSet<string> Permissions
    );