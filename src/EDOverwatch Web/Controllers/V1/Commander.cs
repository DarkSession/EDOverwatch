using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDOverwatch_Web.Controllers.V1
{
    [Route("api/v1/[controller]/[action]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "CommanderApiKeyAuthenticationHandler")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class Commander : ControllerBase
    {
        private EdDbContext DbContext { get; }
        private ActiveMqMessageProducer Producer { get; }
        public Commander(EdDbContext dbContext, ActiveMqMessageProducer producer)
        {
            DbContext = dbContext;
            Producer = producer;
        }

        /*
        [HttpGet]
        [AllowAnonymous]
        public async Task<CommanderTokenResponse> Token(CancellationToken cancellationToken)
        {
            CommanderApiKey commanderApiKey = new(0, Guid.NewGuid(), DateTimeOffset.Now, CommanderApiKeyStatus.Active);
            DbContext.CommanderApiKeys.Add(commanderApiKey);
            await DbContext.SaveChangesAsync(cancellationToken);
            return new CommanderTokenResponse(commanderApiKey.Key);
        }
        */
    }

    public class CommanderTokenResponse
    {
        public Guid ApiKey { get; }

        public CommanderTokenResponse(Guid apiKey)
        {
            ApiKey = apiKey;
        }
    }
}
