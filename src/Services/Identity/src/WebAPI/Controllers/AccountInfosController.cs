using Identity.Application.AccountInfos.Commands.CreateAccountInfo;
using Identity.Application.AccountInfos.Commands.DeleteAccountInfo;
using Identity.Application.AccountInfos.Commands.UpdateAccountInfo;
using Identity.Application.AccountInfos.Dtos;
using Identity.Application.AccountInfos.Queries.GetAccountInfo;
using Identity.Application.AccountInfos.Queries.SearchAccountInfos;

namespace Identity.WebAPI.Controllers
{
    [ApiController]
    [Route("identity/[controller]")]
    [Authorize]  // 需要认证
    public class AccountInfosController : ControllerBase
    {
        private readonly ILogger<AccountInfosController> _logger;
        private readonly ISender _sender;

        public AccountInfosController(ILogger<AccountInfosController> logger, ISender sender)
        {
            _logger = logger;
            _sender = sender;
        }

        [HttpPost("Get")]
        public async Task<ActionResult<AccountInfoDto>> Get(GetAccountInfoQuery query)
        {
            return await _sender.Send(query);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create(CreateAccountInfoCommand command)
        {
            return await _sender.Send(command);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _sender.Send(new DeleteAccountInfoCommand(id));

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, UpdateAccountInfoCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            await _sender.Send(command);

            return Ok();
        }

        [HttpPost("Search")]
        public async Task<ActionResult<PaginatedList<AccountInfoDto>>> Search(SearchAccountInfosQuery query)
        {
            return await _sender.Send(query);
        }
    }
}
