using Identity.Application.Authorizations.Commands.Login;
using Identity.Application.Authorizations.Commands.Logout;
using Identity.Application.Authorizations.Commands.RefreshToken;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.WebAPI.Controllers
{
    /// <summary>
    /// 认证相关 API
    /// </summary>
    [ApiController]
    [Route("identity/[controller]")]
    public class AuthorizationsController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<AuthorizationsController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthorizationsController(
            ISender sender,
            ILogger<AuthorizationsController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _sender = sender;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取客户端 IP 地址
        /// </summary>
        private string? GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // 优先从 X-Forwarded-For 获取（代理/负载均衡器）
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // 其次从 X-Real-IP 获取
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // 最后从 Connection 获取
            return context.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request">登录请求</param>
        /// <returns>登录响应，包含用户信息和 Token</returns>
        [AllowAnonymous]
        [EnableRateLimiting("IdentityAuth")]
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginCommand command)
        {
            try
            {
                var result = await _sender.Send(command);

                _logger.LogInformation("用户 {UserId} 登录成功，IP: {IP}", result.User.Id, GetClientIpAddress());

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("登录失败：{Message}，IP: {IP}", ex.Message, GetClientIpAddress());
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 刷新 Token
        /// </summary>
        /// <param name="request">刷新 Token 请求</param>
        /// <returns>新的 Token 对</returns>
        [AllowAnonymous]
        [EnableRateLimiting("IdentityAuth")]
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(RefreshTokenCommand command)
        {
            try
            {
                var result = await _sender.Send(command);

                _logger.LogInformation("Token 刷新成功，IP: {IP}", GetClientIpAddress());

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Token 刷新失败：{Message}，IP: {IP}", ex.Message, GetClientIpAddress());
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <param name="request">登出请求</param>
        /// <returns>登出结果</returns>
        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout(LogoutCommand command)
        {
            var accountInfoId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (command.AccountInfoId == null)
            {
                command.AccountInfoId = accountInfoId;
            }

            await _sender.Send(command);

            _logger.LogInformation("用户 {AccountInfoId} 登出成功，IP: {IP}", accountInfoId, GetClientIpAddress());

            return Ok(new { message = "登出成功" });
        }
    }
}
