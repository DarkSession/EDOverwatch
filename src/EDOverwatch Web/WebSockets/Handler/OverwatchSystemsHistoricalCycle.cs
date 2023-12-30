using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.NotTracked;
using LazyCache;
using Newtonsoft.Json;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchSystemsHistoricalCycle : WebSocketHandler
    {
        class OverwatchSystemsHistoricalCycleRequest
        {
            public DateOnly? Cycle { get; set; }

            [JsonProperty(Required = Required.Default)]
            public int DefaultWeek { get; set; }

            [JsonProperty(Required = Required.Default)]
            public bool IgnoreClear { get; set; }
        }

        protected override Type? MessageDataType => typeof(OverwatchSystemsHistoricalCycleRequest);

        public override bool AllowAnonymous => true;

        private IAppCache AppCache { get; }

        public OverwatchSystemsHistoricalCycle(IAppCache appCache)
        {
            AppCache = appCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchSystemsHistoricalCycleRequest? data = message.Data?.ToObject<OverwatchSystemsHistoricalCycleRequest>();
            if (data != null)
            {
                DateOnly cycle = data.Cycle ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(data.DefaultWeek * 7);
                if (cycle > DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date) || !await dbContext.ThargoidCycleExists(cycle, cancellationToken))
                {
                    cycle = DateOnly.FromDateTime(WeeklyTick.GetTickTime(DateTimeOffset.UtcNow).DateTime);
                }
                OverwatchStarSystemsHistorical result = await OverwatchStarSystemsHistorical.Create(cycle, dbContext, AppCache, cancellationToken);
                if (data.IgnoreClear)
                {
                    result.Systems = result.Systems.Where(s => s.ThargoidLevel.Level != StarSystemThargoidLevelState.None).ToList();
                }
                return new WebSocketHandlerResultSuccess(result, new NotTrackedObject());
            }
            return new WebSocketHandlerResultError();
        }
    }
}
