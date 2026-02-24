using Codebase.Models.Dtos.Auth;

namespace Codebase.Services.Interfaces.Auth;

public interface IAuthService
{
    Task<IResult> SignUpAsync(SignUpDto dto);
    Task<IResult> SignInAsync(SignInDto dto);
    Task SignOutAsync(string token);
}