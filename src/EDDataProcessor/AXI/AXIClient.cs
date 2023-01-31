using EDDataProcessor.AXI.Models;

namespace EDDataProcessor.AXI
{
    internal class AXIClient : IDisposable
    {
        private ILogger Log { get; }
        private IConfiguration Configuration { get; }
        private HttpClient HttpClient { get; } = new();
        private EdDbContext DbContext { get; }

        public AXIClient(ILogger<AXIClient> log, IConfiguration configuration, EdDbContext dbContext)
        {
            Log = log;
            Configuration = configuration;
            DbContext = dbContext;
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }

        public async Task GetAndUpdate(CancellationToken cancellationToken)
        {
            string url = Configuration.GetValue<string>("AXI:Url") ?? throw new Exception("AXI:Url is not configured");
            using HttpResponseMessage response = await HttpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                SystemModel? systemModel = JsonConvert.DeserializeObject<SystemModel>(content);
                DcohFaction? faction = await DbContext.DcohFactions.FirstOrDefaultAsync(d => d.Short == "AXIN", cancellationToken);
                if (systemModel != null && faction != null)
                {
                    List<DcohFactionOperation> activeFactionOperations = new();
                    foreach (SystemModelRow systemModelRow in systemModel.Message.Rows.Where(r => r.Priority != null && r.Priority > 0))
                    {
                        StarSystem? starSystem = await DbContext.StarSystems.FirstOrDefaultAsync(s => s.Name == systemModelRow.Name, cancellationToken);
                        if (starSystem != null)
                        {
                            DcohFactionOperation? factionOperation = await DbContext.DcohFactionOperations
                                .FirstOrDefaultAsync(d => d.StarSystem == starSystem && d.Faction == faction && d.Status == DcohFactionOperationStatus.Active, cancellationToken);
                            if (factionOperation == null)
                            {
                                factionOperation = new(0, DcohFactionOperationType.AXCombat, DcohFactionOperationStatus.Active, DateTimeOffset.UtcNow, null)
                                {
                                    Faction = faction,
                                    StarSystem = starSystem,
                                };
                                DbContext.DcohFactionOperations.Add(factionOperation);
                            }
                            activeFactionOperations.Add(factionOperation);
                        }
                    }
                    await DbContext.SaveChangesAsync(cancellationToken);

                    await DbContext.DcohFactionOperations
                        .Where(d => d.Faction == faction && d.Status == DcohFactionOperationStatus.Active && !activeFactionOperations.Contains(d))
                        .ForEachAsync(d => d.Status = DcohFactionOperationStatus.Expired, cancellationToken);

                    await DbContext.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                Log.LogError("Response code: {responseCode}", response.StatusCode);
            }
        }
    }
}