var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    Service = "SilverCloud Gateway",
    Routes = new[]
    {
        "/identity/{**catch-all}",
        "/user/{**catch-all}"
    }
}));

app.MapReverseProxy();

app.Run();