using Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;

namespace EDOverwatch_Web.Controllers.V1
{
    [Route("api/v1/[controller]/[action]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "CommanderApiKeyAuthenticationHandler")]
    public class Commander : ControllerBase
    {
        private static JsonSchema BasicEventsSchema {get;} = JsonSchema.FromType<List<BasicEvent>>(new()
        {
            AlwaysAllowAdditionalObjectProperties = true,
        });

        private EdDbContext DbContext { get; }
        private ActiveMqMessageProducer Producer { get; }
        private ILogger Log { get; }
        public Commander(EdDbContext dbContext, ActiveMqMessageProducer producer, ILogger<Commander> log)
        {
            DbContext = dbContext;
            Producer = producer;
            Log = log;
        }

        [HttpPost]
        [RequestSizeLimit(2_097_152)] // 2MB
        public async Task<ActionResult> Events([FromBody] Newtonsoft.Json.Linq.JArray events, CancellationToken cancellationToken)
        {
            if (HttpContext.User.Claims.FirstOrDefault(c => c.Type == "CommanderId")?.Value is not string commanderIdString)
            {
                return Unauthorized();
            }
            int commanderId = int.Parse(commanderIdString);
            EDDatabase.Commander? commander = await DbContext.Commanders
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == commanderId, cancellationToken);
            if (commander == null)
            {
                return Unauthorized();
            }
            ICollection<ValidationError> validationErrors = BasicEventsSchema.Validate(events);
            if (validationErrors.Any())
            {
                Log.LogWarning("Received message with {count} validation errors", validationErrors.Count);
                return BadRequest();
            }

            List<JObject>? commanderEvents = events.ToObject<List<JObject>>();
            if (commanderEvents == null || commanderEvents.Count > 40)
            {
                return new StatusCodeResult(413);
            }

            CommanderPublicApiEvents commanderPublicApiEvents = new(commander.Id)
            {
                Events = commanderEvents,
            };
            await Producer.SendAsync(CommanderPublicApiEvents.QueueName, CommanderPublicApiEvents.Routing, commanderPublicApiEvents.Message, cancellationToken);
            return Ok();
        }
    }

    public class BasicEvent
    {
        [JsonProperty("event", Required = Required.Always)]
        public string Event { get; set; } = string.Empty;

        [JsonProperty("timestamp", Required = Required.Always)]
        public string Timestamp { get; set; } = string.Empty;

        [JsonProperty("cmdr", Required = Required.Always)]
        public string CMDR { get; set; } = string.Empty;

        [JsonProperty("SystemAddress", Required = Required.Always)]
        public long SystemAddress { get; set; }
    }
}
