using Codebase.Contexts;
using Codebase.Entities.Auth;
using Codebase.Models.Dtos.Auth;
using Codebase.Models.Dtos.Responses.Shared;
using Codebase.Services.Interfaces.Auth;
using Codebase.Utils;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Codebase.Services.Auth;

public class AuthService(
    AppDbContext db,
    IConnectionMultiplexer redis,
    JwtUtil jwtUtil // Đã tích hợp JwtUtil như thảo luận
) : IAuthService
{
    private readonly IDatabase _redisDb = redis.GetDatabase();
    private readonly TimeSpan _sessionTtl = TimeSpan.FromHours(2);
    private const string KeyCachePrefix = "session:";

    public async Task<IResult> SignUpAsync(SignUpDto dto)
    {
        // 1. Kiểm tra username tồn tại
        if (await db.Users.AnyAsync(u => u.Username == dto.Username))
            return ResponseDto.Create(ResponseCatalog.Conflict, "auth.username_exists");

        // 2. Validate danh sách RoleIds
        if (dto.RoleIds == null || dto.RoleIds.Count == 0)
            return ResponseDto.Create(ResponseCatalog.BadRequest, "auth.role_id_missing");

        // Lấy danh sách RoleIds duy nhất từ DTO để tránh gửi trùng
        var uniqueRoleIds = dto.RoleIds.Distinct().ToList();

        // Truy vấn một lần duy nhất để lấy các Id tồn tại trong bảng Roles
        var existingRoleIds = await db.Roles
            .Where(r => uniqueRoleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        // Nếu số lượng tìm thấy không khớp với số lượng gửi lên -> Có RoleId không hợp lệ
        if (existingRoleIds.Count != uniqueRoleIds.Count)
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

        db.Users.Add(user);

        // 4. AddAll UserRoles
        var userRoles = existingRoleIds.Select(roleId => new UserRole
        {
            UserId = user.Id,
            RoleId = roleId
        });

        await db.UserRoles.AddRangeAsync(userRoles);

        // 5. Lưu thay đổi (Transaction ngầm định)
        await db.SaveChangesAsync();

        return ResponseDto.Create(ResponseCatalog.Created, "auth.signup_success");
    }


    public async Task<IResult> SignInAsync(SignInDto dto)
    {
        // 1. Tìm User
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            return ResponseDto.Create(ResponseCatalog.Unauthorized, "invalid_credential");
        }

        // 2. Lấy RoleIds và gộp thành chuỗi
        var roleIdsList = await db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        string roleIdsStr = string.Join(",", roleIdsList);

        // 3. Lấy Permission Codes (JOIN 3 bảng) và gộp thành chuỗi
        var permissionCodes = await (from ur in db.UserRoles
                                     join rp in db.RolePermissions on ur.RoleId equals rp.RoleId
                                     join p in db.Permissions on rp.PermissionId equals p.Id
                                     where ur.UserId == user.Id
                                     select p.Code)
                                     .Distinct()
                                     .ToListAsync();

        string permissionsStr = string.Join(",", permissionCodes);

        // 4. Khởi tạo Session với các chuỗi đã gộp
        var session = new UserSession
        {
            UserId = user.Id,
            Username = user.Username,
            RoleIds = roleIdsStr,
            Permissions = permissionsStr,
            Lang = user.Lang,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_sessionTtl)
        };

        // 5. Tạo JWT (Dùng RoleId đầu tiên cho thông tin định danh trong token)
        var token = jwtUtil.GenerateToken(session.UserId, session.Username, roleIdsStr, session.Lang);

        // 6. Lưu vào Redis Hash
        // Vì RoleIds và Permissions bây giờ là string, RedisUtil sẽ lưu chúng như Bulk String.
        // Khi Deserialize, nó sẽ vào nhánh (type == typeof(string)) cực nhanh, KHÔNG qua JSON.
        await RedisUtil.SetObjectToHashAsync(
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
