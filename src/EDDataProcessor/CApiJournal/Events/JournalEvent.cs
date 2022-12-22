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

        public abstract ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken);

        protected Task AddOrUpdateWarEffort(JournalParameters journalParameters, WarEffortType type, long amount, WarEffortSide side, EdDbContext dbContext, CancellationToken cancellationToken)
            => AddOrUpdateWarEffort(journalParameters, journalParameters.CommanderCurrentStarSystem, type, amount, side, dbContext, cancellationToken);

        protected async Task AddOrUpdateWarEffort(JournalParameters journalParameters, StarSystem? starSystem, WarEffortType type, long amount, WarEffortSide side, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            WarEffort? warEffort = await dbContext.WarEfforts
                .FirstOrDefaultAsync(w =>
                        w.Commander == journalParameters.Commander &&
                        w.Date == Day &&
                        w.Type == type &&
                        w.Side == side &&
                        w.Source == WarEffortSource.OverwatchCAPI, cancellationToken);
            if (warEffort == null)
            {
                warEffort = new(0, type, Day, amount, side, WarEffortSource.OverwatchCAPI)
                {
                    Commander = (side == WarEffortSide.Humans) ? journalParameters.Commander : null,
                    StarSystem = starSystem,
                };
                dbContext.WarEfforts.Add(warEffort);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                warEffort.Amount += amount;
            }
            journalParameters.AddWarEffortSystemAddress(starSystem?.SystemAddress ?? 0);
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
        public bool IsDeferred { get; }
        public WarEffortSource Source { get; }
        public Commander Commander { get; }
        public StarSystem? CommanderCurrentStarSystem { get; }
        public bool DeferRequested { get; set; }
        public IAnonymousProducer ActiveMqProducer { get; }
        public Transaction ActiveMqTransaction { get; }
        public List<long>? WarEffortsUpdatedSystemAddresses { get; set; }

        public JournalParameters(bool isDeferred, WarEffortSource source, Commander commander, StarSystem? commanderCurrentStarSystem, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction)
        {
            IsDeferred = isDeferred;
            Source = source;
            Commander = commander;
            CommanderCurrentStarSystem = commanderCurrentStarSystem;
            ActiveMqProducer = activeMqProducer;
            ActiveMqTransaction = activeMqTransaction;
        }

        public void AddWarEffortSystemAddress(long systemAddress)
        {
            WarEffortsUpdatedSystemAddresses ??= new();
            WarEffortsUpdatedSystemAddresses.Add(systemAddress);
        }
    }
}
