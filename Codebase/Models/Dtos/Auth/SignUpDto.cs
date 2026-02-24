using Codebase.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Codebase.Models.Dtos.Auth;

public class SignUpDto
{
    [Required(ErrorMessage = "auth.username_required")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "auth.password_required")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "auth.roles_required")]
    [MinLength(1, ErrorMessage = "auth.roles_empty")]
    public List<Guid> RoleIds { get; set; }
    public LanguageEnum InitLang { get; set; }
}


