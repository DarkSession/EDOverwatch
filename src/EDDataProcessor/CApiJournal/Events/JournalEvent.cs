namespace EDDataProcessor.CApiJournal.Events
{
    internal abstract class JournalEvent
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; } = string.Empty;

        [JsonIgnore]
        public DateOnly Day => DateOnly.FromDateTime(Timestamp.Date);

        [JsonIgnore]
        public virtual bool BypassLiveStatusCheck => false;

        public abstract ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken);

        protected Task AddOrUpdateWarEffort(JournalParameters journalParameters, WarEffortType type, long amount, WarEffortSide side, EdDbContext dbContext, CancellationToken cancellationToken)
            => AddOrUpdateWarEffort(journalParameters.Commander, journalParameters.CommanderCurrentStarSystem, type, amount, side, dbContext, cancellationToken);

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

        protected async Task DeferEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            journalParameters.DeferRequested = true;
            if (journalParameters.IsDeferred)
            {
                return;
            }
            CommanderDeferredJournalEvent commanderDeferredJournalEvent = new(0, Timestamp, Event, journalParameters.Source, JsonConvert.SerializeObject(this), CommanderDeferredJournalEventStatus.Pending)
            {
                Commander = journalParameters.Commander,
                System = journalParameters.CommanderCurrentStarSystem,
            };
            dbContext.CommanderDeferredJournalEvents.Add(commanderDeferredJournalEvent);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    internal class JournalParameters
    {
        public bool IsDeferred { get; set; }
        public WarEffortSource Source { get; set; }
        public Commander Commander { get; set; }
        public StarSystem? CommanderCurrentStarSystem { get; set; }
        public bool DeferRequested { get; set; }

        public JournalParameters(bool isDeferred, WarEffortSource source, Commander commander, StarSystem? commanderCurrentStarSystem)
        {
            IsDeferred = isDeferred;
            Source = source;
            Commander = commander;
            CommanderCurrentStarSystem = commanderCurrentStarSystem;
        }
    }
}
