using EDOverwatch_Web.Models;
using Newtonsoft.Json;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchOperationSearch : WebSocketHandler
    {
        protected override Type? MessageDataType => typeof(OverwatchOperationSearchRequest);

        public override bool AllowAnonymous => true;

        class OverwatchOperationSearchRequest
        {
            [JsonProperty(Required = Required.Default)]
            public DcohFactionOperationType? Type { get; set; }

            [JsonProperty(Required = Required.Default)]
            public string? Maelstrom { get; set; }

            [JsonProperty(Required = Required.Default)]
            public string? SystemName { get; set; }
        }

        class OverwatchOperationSearchResponse
        {
            public List<Models.OverwatchMaelstrom> Maelstroms { get; }
            public List<FactionOperationStarSystemLevel>? Operations { get; set; }

            public OverwatchOperationSearchResponse(List<Models.OverwatchMaelstrom> maelstroms)
            {
                Maelstroms = maelstroms;
            }
        }

        public OverwatchOperationSearch()
        {
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Include(t => t.StarSystem)
                .ToListAsync(cancellationToken);

            OverwatchOperationSearchResponse response = new(maelstroms.Select(m => new Models.OverwatchMaelstrom(m)).ToList());

            OverwatchOperationSearchRequest? data = message.Data?.ToObject<OverwatchOperationSearchRequest>();
            if (data != null)
            {
                IQueryable<DcohFactionOperation> operations = dbContext.DcohFactionOperations
                    .AsNoTracking()
                    .Include(d => d.StarSystem)
                    .ThenInclude(s => s!.ThargoidLevel)
                    .ThenInclude(t => t!.Maelstrom)
                    .Include(d => d.Faction)
                    .Where(d => d.Status == DcohFactionOperationStatus.Active);
                if (data.Type != null)
                {
                    operations = operations.Where(d => d.Type == DcohFactionOperationType.General || d.Type == data.Type);
                }

                List<DcohFactionOperation>? factionOperations = null;
                if (!string.IsNullOrEmpty(data.SystemName))
                {
                    string systemName = data.SystemName.Replace("%", string.Empty).Trim();
                    if (systemName.Length > 2)
                    {
                        StarSystem? starSystem = await dbContext.StarSystems.FirstOrDefaultAsync(s => EF.Functions.Like(s.Name, systemName), cancellationToken);
                        if (starSystem != null)
                        {
                            decimal maxDistance = 40m;
                            operations = operations.Where(o =>
                                o.StarSystem!.LocationX >= starSystem.LocationX - maxDistance && o.StarSystem!.LocationX <= starSystem.LocationX + maxDistance &&
                                o.StarSystem!.LocationY >= starSystem.LocationY - maxDistance && o.StarSystem!.LocationY <= starSystem.LocationY + maxDistance &&
                                o.StarSystem!.LocationZ >= starSystem.LocationZ - maxDistance && o.StarSystem!.LocationZ <= starSystem.LocationZ + maxDistance);
                            factionOperations = await operations.ToListAsync(cancellationToken);
                            factionOperations = factionOperations.Where(f => f.StarSystem != null && (decimal)f.StarSystem.DistanceTo(starSystem) < maxDistance).ToList();
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(data.Maelstrom))
                    {
                        operations = operations.Where(o => o.StarSystem!.ThargoidLevel!.Maelstrom!.Name == data.Maelstrom);
                    }
                    factionOperations = await operations.ToListAsync(cancellationToken);
                }

                if (factionOperations != null)
                {
                    response.Operations = factionOperations.Select(f => new FactionOperationStarSystemLevel(f)).ToList();
                }
            }

            return new WebSocketHandlerResultSuccess(response, null);
        }
    }
}
