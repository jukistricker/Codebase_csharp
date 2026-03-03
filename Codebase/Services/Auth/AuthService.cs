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
        if (await _authRepo.UsernameExistsAsync(dto.Username))
            return ResponseDto.Create(ResponseCatalog.Conflict, "auth.username_exists");

        HashSet<Guid> roleIds;

        var defaultRoleId = await _authRepo.GetDefaultRoleIdAsync();
        if (defaultRoleId == null)
            return ResponseDto.Create(ResponseCatalog.BadRequest, "auth.default_role_not_found");
        
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = dto.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Lang = dto.InitLang
        };

        user.CreatedBy = user.Id;
        user.UpdatedBy = user.Id;

        await _authRepo.AddUserAsync(user);
        
        List<UserRole> userRoles=new List<UserRole>();

        userRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = defaultRoleId.Value
        });

        await _authRepo.AddUserRolesAsync(userRoles);
        await _authRepo.SaveChangesAsync();

        return ResponseDto.Create(ResponseCatalog.Created, "auth.signup_success");
    }

    public async Task<IResult> SignInAsync(SignInDto dto)
    {
        var user = await _authRepo.GetByUsernameAsync(dto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            return ResponseDto.Create(ResponseCatalog.Unauthorized, "invalid_credential");

        var roleIds = await _authRepo.GetUserRoleIdsAsync(user.Id);
        var permissions = await _authRepo.GetUserPermissionCodesAsync(user.Id);

        var session = new UserSession
        {
            UserId = user.Id,
            Username = user.Username,
            RoleIds = roleIds,
            Permissions = permissions,
            Lang = user.Lang,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_sessionTtl)
        };

        var token = _tokenUtil.GenerateToken(user.Id, user.Username, user.Lang);

        await _sessionRepo.StoreAsync(token, session, _sessionTtl);

        return ResponseDto.Create(ResponseCatalog.Success, "auth.login_success", token);
    }

    public Task SignOutAsync(string token)
        => _sessionRepo.DeleteAsync(token);
}