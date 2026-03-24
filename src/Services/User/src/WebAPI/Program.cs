using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using User.Infrastructure;
using User.Infrastructure.Persistence.MongoDb;
using User.Infrastructure.Persistence.MongoDb.Interfaces;
using User.Infrastructure.Persistence.SqlServer;
using WebAPI.Helpers;

var builder = WebApplication.CreateBuilder(args);

// --- 关键代码：注册 HttpContext 访问器 ---
builder.Services.AddHttpContextAccessor();

// 1. 添加 JWT 配置
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// 2. 配置 JWT Bearer 认证
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
var key = Encoding.UTF8.GetBytes(jwtSettings!.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// --- 添加速率限制（优化版）---
builder.Services.AddRateLimiter(options =>
{
    // 全局拒绝处理
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "请求过于频繁，请稍后再试",
            retryAfter = TimeSpan.FromMinutes(1).TotalSeconds
        }, cancellationToken);
    };

    // 全局默认限流器：所有未标记的接口都使用此策略（每分钟 100 次）
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => RateLimiterHelper.CreateFixedWindowLimiter(100, 1)));

    // 身份验证专用策略：登录、刷新Token 等敏感操作（每分钟 10 次）
    options.AddPolicy("auth", httpContext =>
    {
        string username = httpContext.Request.Headers["identity"].ToString() ?? "anon";
        string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon-ip";

        return RateLimitPartition.GetFixedWindowLimiter(
             partitionKey: $"auth_{ip}_{username}",
             factory: _ => RateLimiterHelper.CreateFixedWindowLimiter(10, 1));
    });
});

// 添加数据连接
builder.Services.AddInfrastructureServices(builder.Configuration);

// 添加 application 服务
builder.Services.AddApplicationServices();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // 初始化 MongoDB
    var mongoDbContext = scope.ServiceProvider.GetRequiredService<IMongoDbContext>();
    await mongoDbContext.InitializeAsync();

    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

    // 判断是否执行 update-database
    if (app.Environment.IsDevelopment())
    {
        await initialiser.InitialiseAsync();
    }

    // sql 默认数据
    await initialiser.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    //app.UseExceptionHandler("/Error"); // 生产环境通用错误页
    app.UseHsts(); // 强制 HTTPS 严格传输UseStaticFiles
}

app.UseHttpsRedirection();// 强制跳转到 HTTPS

// 静态文件（如果你有图片或网页，放在这里可以跳过认证提高速度）
app.UseStaticFiles();

// --- 启用速率限制 ---
app.UseRateLimiter();

// 3. 启用认证和授权
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
