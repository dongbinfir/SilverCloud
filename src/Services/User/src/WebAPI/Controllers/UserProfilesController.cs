using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using User.Application.UserProfiles.Commands.CreateUserProfile;
using User.Application.UserProfiles.Commands.DeleteUserProfile;
using User.Application.UserProfiles.Commands.UpdateUserProfile;
using User.Application.UserProfiles.Dtos;
using User.Application.UserProfiles.Queries.GetUserProfile;
using User.Application.UserProfiles.Queries.SearchUserProfiles;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [AllowAnonymous]
        [HttpPost("Get")]
        public async Task<ActionResult<LoginResponseDto>> Get(GetUserProfileQuery query)
        {
            return await _sender.Send(query);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create(CreateUserProfileCommand command)
        {
            return await _sender.Send(command);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _sender.Send(new DeleteUserProfileCommand(id));

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, UpdateUserProfileCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            await _sender.Send(command);

            return Ok();
        }

        [HttpPost("Search")]
        public async Task<ActionResult<PaginatedList<UserProfileBriefDto>>> Search(SearchUserProfilesQuery query)
        {
            return await _sender.Send(query);
        }
    }
}
