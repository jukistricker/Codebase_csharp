using Codebase.Models.Dtos.Requests.Auth;

namespace Codebase.Services.Interfaces.Auth;

public interface IAuthService
{
    Task<IResult> SignUpAsync(SignUpDto dto);
    Task<IResult> SignInAsync(SignInDto dto);
    Task<IResult> SignOutAsync(string jti);
}