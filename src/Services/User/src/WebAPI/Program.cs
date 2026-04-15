using Scalar.AspNetCore;
using User.WebAPI.ConfigureServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpServices()
    .AddJwtAuth(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAppPipeline();

app.Run();
