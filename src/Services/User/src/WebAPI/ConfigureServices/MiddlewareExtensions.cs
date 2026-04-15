namespace User.WebAPI.ConfigureServices
{
    public static class MiddlewareExtensions
    {
        public static WebApplication UseAppPipeline(this WebApplication app)
        {
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            return app;
        }
    }
}