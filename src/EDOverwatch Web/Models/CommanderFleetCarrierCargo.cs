﻿namespace EDOverwatch_Web.Models
{
    public class CommanderFleetCarrierCargo
    {
        public DateTimeOffset LastUpdated { get; }
        public bool HasFleetCarrier { get; }
        public List<CommanderFleetCarrierCargoEntry> Cargo { get; }

        protected CommanderFleetCarrierCargo(bool hasFleetCarrier, List<CommanderFleetCarrierCargoItem> fleetCarrierCargoItems, DateTimeOffset lastUpdated)
        {
            HasFleetCarrier = hasFleetCarrier;
            Cargo = fleetCarrierCargoItems.Select(f => new CommanderFleetCarrierCargoEntry(f)).ToList();
            LastUpdated = lastUpdated;
        }

        public static Task<CommanderFleetCarrierCargo> Create(EdDbContext dbContext, Commander commander, CancellationToken cancellationToken)
            => Create(dbContext, commander.Id, cancellationToken);

        public static async Task<CommanderFleetCarrierCargo> Create(EdDbContext dbContext, int commanderId, CancellationToken cancellationToken)
        {
            Commander commander = await dbContext.Commanders.FindAsync(commanderId, cancellationToken) ?? throw new Exception("Invalid commander");
            List<CommanderFleetCarrierCargoItem> fleetCarrierCargoItems = await dbContext.CommanderFleetCarrierCargoItems
                .Where(c => c.Commander == commander)
                .ToListAsync(cancellationToken);

            CommanderFleetCarrierCargo result = new(commander.HasFleetCarrier == CommanderFleetHasFleetCarrier.Yes, fleetCarrierCargoItems, commander.JournalLastProcessed);
            return result;
        }
    }

    public class CommanderFleetCarrierCargoEntry
    {
        public string Commodity { get; }
        public OverwatchStarSystemMin StarSystem { get; }
        public int Quantity { get; set; }

        public CommanderFleetCarrierCargoEntry(CommanderFleetCarrierCargoItem commanderFleetCarrierCargoItem)
        {
            if (commanderFleetCarrierCargoItem.Commodity is null)
            {
                throw new Exception("Commodity cannot be null");
            }
            Commodity = commanderFleetCarrierCargoItem.Commodity.NameEnglish ?? "Localisation missing: " + commanderFleetCarrierCargoItem.Commodity.Name;
            StarSystem = new(commanderFleetCarrierCargoItem.SourceStarSystem ?? throw new Exception("SourceStarSystem cannot be null"));
            Quantity = commanderFleetCarrierCargoItem.Amount;
        }
    }
}
