using Codebase.Contexts;
using Codebase.Entities.Auth;
using Codebase.Middlewares;
using Codebase.Models.Dtos.Responses.Shared;
using Codebase.Repositories;
using Codebase.Repositories.Interfaces;
using Codebase.Services.Auth;
using Codebase.Services.Interfaces.Auth;
using Codebase.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt=>{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // Lấy lỗi đầu tiên từ ModelState
        var errorMessage = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault() ?? "invalid.request_format";

        // Trả về ResponseDto chuẩn của bạn
        return new BadRequestObjectResult(ResponseDto.Create(ResponseCatalog.BadRequest, errorMessage));
    };
});

builder.Services.Configure<PasswordHasherOptions>(opt =>
{
    // Giảm xuống mức 10,000 hoặc 5,000
    opt.IterationCount = 10000; 
    
    // Đảm bảo dùng PBKDF2 với SHA256 .CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Codebase API", Version = "v1" });

    // 1. Định nghĩa kiểu bảo mật JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập Token theo định dạng: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // 2. Áp dụng bảo mật này cho tất cả API
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

// Đăng ký Repository 
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();

// Đăng ký Service
builder.Services.AddScoped<IAuthService, AuthService>();

//Đăng ký các Unstatic Util
builder.Services.AddSingleton<TokenUtil>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthorization();
var app = builder.Build();

var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
HttpContextUtil.Configure(accessor);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UsePathBase("/api");

app.UseHttpsRedirection();

app.UseGlobalApiErrorHandling(app.Environment);

using (var scope = app.Services.CreateScope())
{
    try 
    {
        var authRepo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
        var defaultRoleId = await authRepo.GetDefaultRoleIdAsync();

        if (defaultRoleId.HasValue)
        {
            GlobalCache.DefaultUserRoleId = defaultRoleId.Value;
        }
        else
        {
            throw new Exception("Default role 'user' not found. Please seed the database.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Critical] Failed to init GlobalCache: {ex.Message}");
    }
}

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();

app.UseMiddleware<RolePermissionMiddleware>();

app.MapControllers();

app.UseHttpMetrics(); // Theo dõi các yêu cầu HTTP (tùy chọn)
app.MapMetrics();

app.Run();
