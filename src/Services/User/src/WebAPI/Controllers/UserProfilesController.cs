using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using User.Application.UserProfiles.Commands.CreateUserProfile;
using User.Application.UserProfiles.Dtos;
using User.Application.UserProfiles.Queries.GetUserProfile;

namespace User.WebAPI.Controllers
{
    [ApiController]
    [Route("user/[controller]")]
    [Authorize]  // 需要认证
    public class UserProfilesController : ControllerBase
    {
        private readonly ILogger<UserProfilesController> _logger;
        private readonly ISender _sender;

        public UserProfilesController(ILogger<UserProfilesController> logger, ISender sender)
        {
            _logger = logger;
            _sender = sender;
        }

        [HttpPost("Get")]
        public async Task<ActionResult<UserProfileDto>> Get(GetUserProfileQuery query)
        {
            return await _sender.Send(query);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create(CreateUserProfileCommand command)
        {
            return await _sender.Send(command);
        }

    }
}
