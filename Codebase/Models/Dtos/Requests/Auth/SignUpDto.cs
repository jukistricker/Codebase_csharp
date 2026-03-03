using Codebase.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Codebase.Models.Dtos.Requests.Auth;

public class SignUpDto
{
    [Required(ErrorMessage = "auth.username_required")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "auth.password_required")]
    public string Password { get; set; } = null!;
    public LanguageEnum InitLang { get; set; }
}


