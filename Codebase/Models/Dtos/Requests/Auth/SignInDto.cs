namespace Codebase.Models.Dtos.Requests.Auth;

public class SignInDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
