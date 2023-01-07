using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDOverwatch_Web.Controllers.V1
{
    [Route("api/v1/[controller]/[action]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "CommanderApiKeyAuthenticationHandler")]
    public class Commander : ControllerBase
    {
        private EdDbContext DbContext { get; }
        private ActiveMqMessageProducer Producer { get; }
        public Commander(EdDbContext dbContext, ActiveMqMessageProducer producer)
        {
            DbContext = dbContext;
            Producer = producer;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<CommanderTokenResponse> Token(CancellationToken cancellationToken)
        {
            CommanderApiKey commanderApiKey = new(0, Guid.NewGuid(), DateTimeOffset.Now, CommanderApiKeyStatus.Active);
            DbContext.CommanderApiKeys.Add(commanderApiKey);
            await DbContext.SaveChangesAsync(cancellationToken);
            return new CommanderTokenResponse(commanderApiKey.Key);
        }

        [HttpGet]
        [AllowAnonymous]
        public List<ApplicableJournalEvent> Events()
        {
            return new()
            {
                new ApplicableJournalEvent("MissionAccepted")
                    .AddFilter("Name", "^Mission_TW"),
                new ApplicableJournalEvent("MissionCompleted")
                    .AddFilter("Name", "^Mission_TW"),
                new ApplicableJournalEvent("Died")
                    .AddFilter("KillerShip", "scout_hq|scout_nq|scout_q|scout|thargonswarm|thargon"),
                new ApplicableJournalEvent("FactionKillBond")
                    .AddFilter("AwardingFaction", @"^\$faction_PilotsFederation;$")
                    .AddFilter("VictimFaction", @"^\$faction_Thargoid;$"),
                new ApplicableJournalEvent("CollectCargo")
                    .AddFilter("Type", "UnknownArtifact2"),
            };
        }
    }

    public class CommanderTokenResponse
    {
        public Guid ApiKey { get; }

        public CommanderTokenResponse(Guid apiKey)
        {
            ApiKey = apiKey;
        }
    }

    public class ApplicableJournalEvent
    {
        public string Name { get; set; }
        public Dictionary<string, string> Filter { get; set; } = new();

        public ApplicableJournalEvent(string name)
        {
            Name = name;
        }

        public ApplicableJournalEvent AddFilter(string name, string regex)
        {
            Filter[name] = regex;
            return this;
        }
    }
}
