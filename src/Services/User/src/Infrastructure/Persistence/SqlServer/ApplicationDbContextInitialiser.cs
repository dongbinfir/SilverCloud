using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using User.Domain.Entities;
using User.Domain.ValueObjects;

namespace User.Infrastructure.Persistence.SqlServer
{
    public static class InitialiserExtensions
    {
        public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

            await initialiser.InitialiseAsync();
            //await initialiser.SeedAsync();
        }
    }

    public class ApplicationDbContextInitialiser
    {
        private readonly ILogger<ApplicationDbContextInitialiser> _logger;
        private readonly ApplicationDbContext _context;

        public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task InitialiseAsync()
        {
            try
            {
                if (_context.Database.IsSqlServer())
                {
                    await _context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initialising the database.");
                throw;
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                await TrySeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        public async Task TrySeedAsync()
        {
            //// Default data
            //// 先清空现有数据（因为密码格式已更新为 BCrypt）
            //if (_context.Set<UserProfile>().Any())
            //{
            //    var existingUsers = await _context.Set<UserProfile>().ToListAsync();
            //    _context.Set<UserProfile>().RemoveRange(existingUsers);
            //    await _context.SaveChangesAsync();
            //}

            // 创建使用 BCrypt 哈希的新用户
            if (!_context.Set<UserProfile>().Any())
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword("dongbin123456");

                _context.Set<UserProfile>().Add(new UserProfile
                {
                    Name = "admin",
                    Email = Email.Create("2529411612@qq.com"),
                    PhoneNum = "13487810907",
                    Password = hashedPassword
                });

                await _context.SaveChangesAsync();
            }
        }
    }
}
