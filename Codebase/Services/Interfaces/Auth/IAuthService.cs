using Codebase.Models.Dtos.Requests;

namespace Codebase.Services.Interfaces.Auth;

public interface IAuthService
{
    Task<IResult> SignUpAsync(SignUpDto dto);
    Task<IResult> SignInAsync(SignInDto dto);
    Task<IResult> SignOutAsync(string jti);
    Task<IResult> GetUsersAsync(AuthFilterRequest req);
}