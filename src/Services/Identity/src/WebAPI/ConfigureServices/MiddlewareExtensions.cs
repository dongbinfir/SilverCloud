namespace Identity.WebAPI.ConfigureServices
{
    public static class MiddlewareExtensions
    {
        public static WebApplication UseAppPipeline(this WebApplication app)
        {
            // ---------------- 中间件管道（重点）----------------

            // HTTPS 重定向（必须早）
            app.UseHttpsRedirection();

            // 静态文件（提前 short-circuit）
            app.UseStaticFiles();

            // Routing（必须）
            app.UseRouting();

            // CORS（在 Routing 后，Auth 前）
            app.UseCors("AllowAdminWeb");

            // 限流
            app.UseRateLimiter();

            // 认证 & 授权（顺序固定）
            app.UseAuthentication();
            app.UseAuthorization();

            // Endpoint（必须最后）
            app.MapControllers();

            return app;
        }
    }
}
