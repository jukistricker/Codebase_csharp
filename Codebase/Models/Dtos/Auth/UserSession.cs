using Codebase.Models.Enums;

namespace Codebase.Models.Dtos.Auth;

public class UserSession
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;

    // Lưu dưới dạng chuỗi: "guid1,guid2,..."
    public string RoleIds { get; set; } = string.Empty;

    // Lưu dưới dạng chuỗi: "CODE1,CODE2,..."
    public string Permissions { get; set; } = string.Empty;

    public LanguageEnum Lang { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
