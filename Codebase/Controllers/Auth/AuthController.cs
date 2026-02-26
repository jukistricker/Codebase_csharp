using Codebase.Models.Dtos.Requests.Auth;
using Codebase.Services.Interfaces.Auth;
using Codebase.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codebase.Controllers.Auth;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IResult> SignUp([FromBody] SignUpDto dto)
    {
        return await _authService.SignUpAsync(dto);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IResult> Login([FromBody] SignInDto dto)
    {
        return await _authService.SignInAsync(dto);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task LogOut()
    {
        string token = HttpContext.GetBearerToken();
        await _authService.SignOutAsync(token);
    }
}