using Codebase.Contexts;
using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Requests.Auth;
using Codebase.Models.Dtos.Responses.Shared;
using Codebase.Services.Interfaces.Auth;
using Codebase.Utils;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Codebase.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtUtil _jwtUtil;
    private readonly IDatabase _redisDb;

    private readonly TimeSpan _sessionTtl = TimeSpan.FromHours(2);
    private const string KeyCachePrefix = "session:";

    public AuthService(
        AppDbContext db,
        IConnectionMultiplexer redis,
        JwtUtil jwtUtil
    )
    {
        _db = db;
        _jwtUtil = jwtUtil;
        _redisDb = redis.GetDatabase();
    }

    public async Task<IResult> SignUpAsync(SignUpDto dto)
    {
        // 1. Kiểm tra username tồn tại
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            return ResponseDto.Create(ResponseCatalog.Conflict, "auth.username_exists");

        // 2. Validate danh sách RoleIds
        HashSet<Guid> roleIds = new HashSet<Guid>();
        if (dto.RoleIds == null || dto.RoleIds.Count == 0)
        {
            roleIds.Add(_db.Roles
                .Where(r => r.Name == "user")
                .Select(r => r.Id)
                .Single());
        }
        else
        {
            roleIds = dto.RoleIds.ToHashSet();
        }
        

        // Truy vấn một lần duy nhất để lấy các Id tồn tại trong bảng Roles
        var existingRoleIds = await _db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        // Nếu số lượng tìm thấy không khớp với số lượng gửi lên -> Có RoleId không hợp lệ
        if (existingRoleIds.Count != roleIds.Count)
            return ResponseDto.Create(ResponseCatalog.BadRequest, "auth.some_role_ids_invalid");

        // 3. Khởi tạo User (UUID v7)
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = dto.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Lang = dto.InitLang
        };

        // Audit Info
        user.CreatedBy = user.Id;
        user.UpdatedBy = user.Id;

        _db.Users.Add(user);

        // 4. AddAll UserRoles
        var userRoles = existingRoleIds.Select(roleId => new UserRole
        {
            UserId = user.Id,
            RoleId = roleId
        });

        await _db.UserRoles.AddRangeAsync(userRoles);

        // 5. Lưu thay đổi (Transaction ngầm định)
        await _db.SaveChangesAsync();

        return ResponseDto.Create(ResponseCatalog.Created, "auth.signup_success");
    }


    public async Task<IResult> SignInAsync(SignInDto dto)
    {
        // 1. Tìm User
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            return ResponseDto.Create(ResponseCatalog.Unauthorized, "invalid_credential");
        }

        // 2. Lấy RoleIds 
        /*TODO: Overhead của việc tạo thêm 1 List rồi convert sang HashSet là cực kỳ nhỏ (microseconds),
         trong khi nếu dùng thẳng ToHashSet() từ đầu thì sẽ block thread cả lúc I/O đến DB, thường mất hàng chục đến hàng trăm milliseconds.*/
        HashSet<Guid> roleIdsSet = (await _db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.RoleId)
                .ToListAsync())
            .ToHashSet();


        // 3. Lấy Permission Codes (JOIN 3 bảng) 
        HashSet<string> permissionCodes = (await (from ur in _db.UserRoles
                    join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                    join p in _db.Permissions on rp.PermissionId equals p.Id
                    where ur.UserId == user.Id
                    select p.Code)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        // 4. Khởi tạo Session 
        var session = new UserSession
        {
            UserId = user.Id,
            Username = user.Username,
            RoleIds = roleIdsSet,
            Permissions = permissionCodes,
            Lang = user.Lang,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_sessionTtl)
        };

        // 5. Tạo JWT 
        var token = _jwtUtil.GenerateToken(session.UserId, session.Username, session.Lang);

        // 6. Lưu vào Redis 
        await RedisUtil.SetObjectAsJsonAsync(
            _redisDb,
            $"{KeyCachePrefix}{token}",
            session,
            _sessionTtl
        );

        return ResponseDto.Create(ResponseCatalog.Success,"auth.login_success", token);
    }


    public async Task SignOutAsync(string token)
    {
        await _redisDb.KeyDeleteAsync($"{KeyCachePrefix}{token}");
    }
}
