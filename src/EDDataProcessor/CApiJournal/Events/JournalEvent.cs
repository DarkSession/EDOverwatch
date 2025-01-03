﻿namespace EDDataProcessor.CApiJournal.Events
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
            string eventHash = CommanderJournalProcessedEvent.GetEventHash(journalParameters.Commander, starSystem, Timestamp, Event, type);
            if (await dbContext.CommanderJournalProcessedEvents
                .AnyAsync(c => (c.Hash == eventHash && c.Line == 0) || (c.Hash == eventHash && c.Line == journalParameters.Line), cancellationToken))
            {
                return;
            }
            dbContext.CommanderJournalProcessedEvents.Add(new(0, Timestamp, journalParameters.Line, eventHash)
            {
                Commander = journalParameters.Commander,
            });

            WarEffort? warEffort = await dbContext.WarEfforts
                .FirstOrDefaultAsync(w =>
                        w.Commander == journalParameters.Commander &&
                        w.Date == Day &&
                        w.Type == type &&
                        w.Side == side &&
                        w.StarSystem == starSystem &&
                        w.Source == journalParameters.Source, cancellationToken);
            if (warEffort == null)
            {
                warEffort = new(0, type, Day, amount, side, journalParameters.Source)
                {
                    Commander = journalParameters.Commander,
                    StarSystem = starSystem,
                    Cycle = await dbContext.GetThargoidCycle(Day, cancellationToken),
                };
                dbContext.WarEfforts.Add(warEffort);
            }
            else
            {
                warEffort.Amount += amount;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
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
        public IAnonymousProducer? ActiveMqProducer { get; }
        public Transaction? ActiveMqTransaction { get; }
        public List<long>? WarEffortsUpdatedSystemAddresses { get; set; }
        public int Line { get; set; }

        public JournalParameters(bool isDeferred, WarEffortSource source, Commander commander, StarSystem? commanderCurrentStarSystem, IAnonymousProducer? activeMqProducer, Transaction? activeMqTransaction, int line)
        {
            IsDeferred = isDeferred;
            Source = source;
            Commander = commander;
            CommanderCurrentStarSystem = commanderCurrentStarSystem;
            ActiveMqProducer = activeMqProducer;
            ActiveMqTransaction = activeMqTransaction;
            Line = line;
        }

        public void AddWarEffortSystemAddress(long systemAddress)
        {
            WarEffortsUpdatedSystemAddresses ??= [];
            WarEffortsUpdatedSystemAddresses.Add(systemAddress);
        }

        public Task SendMqMessage(string address, RoutingType routingType, Message message, CancellationToken cancellationToken)
        {
            if (ActiveMqProducer == null || ActiveMqTransaction == null)
            {
                return Task.CompletedTask;
            }
            return ActiveMqProducer.SendAsync(address, routingType, message, ActiveMqTransaction, cancellationToken);
        }
    }
}
