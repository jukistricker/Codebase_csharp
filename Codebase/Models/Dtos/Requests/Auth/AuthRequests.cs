using System.ComponentModel.DataAnnotations;
using Codebase.Models.Enums;

namespace Codebase.Models.Dtos.Requests.Auth;

public class SignUpDto
{
    [Required(ErrorMessage = "auth.username_required")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "auth.password_required")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "auth.password_too_weak"
    )]
    public string Password { get; set; } = null!;

    public LanguageEnum InitLang { get; set; }
}

public class SignInDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}