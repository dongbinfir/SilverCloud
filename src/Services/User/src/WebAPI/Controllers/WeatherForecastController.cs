using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Commons.Interfaces;

namespace User.WebAPI.Controllers
{
    [ApiController]
    [Route("user/[controller]")]
    [Authorize]  // 需要认证
    public class WeatherForecastController : ControllerBase
    {
        private readonly ICurrentAccountService _currentAccountService;

        public WeatherForecastController(ICurrentAccountService currentAccountService)
        {
            _currentAccountService = currentAccountService;
        }

        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Me()
        {
            return Ok(new
            {
                _currentAccountService.Id,
                _currentAccountService.Name,
                _currentAccountService.Email,
                _currentAccountService.PhoneNum
            });
        }
    }
}
