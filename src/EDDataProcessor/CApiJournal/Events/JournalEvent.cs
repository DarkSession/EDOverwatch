namespace EDDataProcessor.CApiJournal.Events
{
    internal abstract class JournalEvent
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonIgnore]
        public DateOnly Day => DateOnly.FromDateTime(Timestamp.Date);

        [JsonIgnore]
        public virtual bool BypassLiveStatusCheck => false;

        public abstract ValueTask ProcessEvent(Commander commander, EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken);

        protected Task AddOrUpdateWarEffort(Commander commander, WarEffortType type, long amount, WarEffortSide side, EdDbContext dbContext, CancellationToken cancellationToken)
            => AddOrUpdateWarEffort(commander, commander.System, type, amount, side, dbContext, cancellationToken);

        protected async Task AddOrUpdateWarEffort(Commander commander, StarSystem? starSystem, WarEffortType type, long amount, WarEffortSide side, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            WarEffort? warEffort = await dbContext.WarEfforts
                .FirstOrDefaultAsync(w =>
                        w.Commander == commander &&
                        w.Date == Day &&
                        w.Type == type &&
                        w.Side == side &&
                        w.Source == WarEffortSource.OverwatchCAPI, cancellationToken);
            if (warEffort == null)
            {
                warEffort = new(0, type, Day, amount, side, WarEffortSource.OverwatchCAPI)
                {
                    Commander = (side == WarEffortSide.Humans) ? commander : null,
                    StarSystem = starSystem,
                };
                dbContext.WarEfforts.Add(warEffort);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                warEffort.Amount += amount;
            }
        }
    }
}
