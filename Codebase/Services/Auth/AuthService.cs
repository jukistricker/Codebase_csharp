using Codebase.Models.Dtos.Requests.Auth;
using Codebase.Models.Enums;

namespace Codebase.Services.Auth;

using Entities.Auth;
using Models.Dtos.Requests.Auth;
using Models.Dtos.Responses.Shared;
using Repositories.Interfaces;
using Services.Interfaces.Auth;
using Utils;


public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly TokenUtil _tokenUtil;

    private readonly TimeSpan _sessionTtl = TimeSpan.FromHours(2);

    public AuthService(
         IAuthRepository authRepo,
        ISessionRepository sessionRepo,
        TokenUtil tokenUtil)
    {
        _authRepo = authRepo;
        _sessionRepo = sessionRepo;
        _tokenUtil = tokenUtil;
    }

    public async Task<IResult> SignUpAsync(SignUpDto dto)
    {
        // 1. Chặn sớm bằng DB 
        if (await _authRepo.UsernameExistsAsync(dto.Username))
            return ResponseDto.Create(ResponseCatalog.Conflict, "auth.username_exists");

        // 2.  Cache Default Role ID
        var defaultRoleId = GlobalCache.DefaultUserRoleId;

        // 3. Logic Language 
        var lang = (dto.InitLang == LanguageEnum.En) ? LanguageEnum.En : LanguageEnum.Vi;

        var userId = Guid.CreateVersion7();
    
        // 4. BCrypt
        String hashedPassword = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(dto.Password));

        var user = new User
        {
            Id = userId,
            Username = dto.Username,
            Password = hashedPassword,
            Lang = lang,
            CreatedBy = userId,
            UpdatedBy = userId,
            UserRoles = new List<UserRole> 
            { 
                new UserRole { UserId = userId, RoleId = defaultRoleId } 
            }
        };
    
        await _authRepo.AddUserAsync(user);
        await _authRepo.SaveChangesAsync();

        return ResponseDto.Create(ResponseCatalog.Created, "auth.signup_success");
    }
    public async Task<IResult> SignInAsync(SignInDto dto)
    {
        var fullInfo = await _authRepo.GetFullUserInfoAsync(dto.Username);

        if (fullInfo == null)
            return ResponseDto.Create(ResponseCatalog.Unauthorized, "auth.invalid_credential");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, fullInfo.User.Password))
            return ResponseDto.Create(ResponseCatalog.Unauthorized, "auth.invalid_credential");

        var session = new UserSession
        {
            UserId = fullInfo.User.Id,
            Username = fullInfo.User.Username,
            RoleIds = fullInfo.RoleIds,
            Permissions = fullInfo.Permissions,
            Lang = fullInfo.User.Lang,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_sessionTtl)
        };

        var (token, jti) = await _tokenUtil.GenerateToken(fullInfo.User.Id, fullInfo.User.Username, fullInfo.User.Lang);

        await _sessionRepo.StoreAsync(jti, session, _sessionTtl);

        return ResponseDto.Create(ResponseCatalog.Success, "auth.login_success", token);
    }

    public Task SignOutAsync(string token)
        => _sessionRepo.DeleteAsync(token);
}