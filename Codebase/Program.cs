using Codebase.Contexts;
using Codebase.Middlewares;
using Codebase.Models.Dtos.Responses.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseGlobalApiErrorHandling(app.Environment);

app.UseAuthorization();

app.MapControllers();

app.UseHttpMetrics(); // Theo dõi các yêu cầu HTTP (tùy chọn)
app.MapMetrics();

app.Run();
