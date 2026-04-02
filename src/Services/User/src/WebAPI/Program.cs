using Scalar.AspNetCore;
using User.Infrastructure;
using User.Infrastructure.Persistence.MongoDb.Interfaces;
using User.Infrastructure.Persistence.SqlServer;
using WebAPI.ConfigureServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpServices()
    .AddJwtAuth(builder.Configuration)
    .AddRateLimiting()
    .AddCorsPolicy();

// 添加 application 服务
builder.Services.AddApplicationServices();

// 添加 infrastructure 服务
builder.Services.AddInfrastructureServices(builder.Configuration);

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

app.UseAppPipeline();

app.Run();
