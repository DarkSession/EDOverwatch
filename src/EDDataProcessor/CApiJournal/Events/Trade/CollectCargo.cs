namespace EDDataProcessor.CApiJournal.Events.Trade
{
    internal class CollectCargo : JournalEvent
    {
        public string Type { get; set; }

        public bool Stolen { get; set; }

        [JsonProperty(Required = Required.Default)]
        public long MissionID { get; set; }

        public CollectCargo(string type)
        {
            Type = type;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (journalParameters.CommanderCurrentStarSystem != null && !Stolen && MissionID == 0)
            {
                switch (Type)
                {
                    case "UnknownArtifact2":
                        {
                            await dbContext.Entry(journalParameters.CommanderCurrentStarSystem)
                                .Reference(j => j.ThargoidLevel)
                                .LoadAsync(cancellationToken);
                            if (journalParameters.CommanderCurrentStarSystem.ThargoidLevel != null &&
                                journalParameters.CommanderCurrentStarSystem.ThargoidLevel.State >= StarSystemThargoidLevelState.Alert)
                            {
                                await AddOrUpdateWarEffort(journalParameters, WarEffortType.ThargoidProbeCollection, 1, WarEffortSide.Humans, dbContext, cancellationToken);
                            }
                            break;
                        }
                    case "ThargoidTissueSampleType1":
                    case "ThargoidTissueSampleType2":
                    case "ThargoidTissueSampleType3":
                    case "ThargoidTissueSampleType4":
                    case "ThargoidTissueSampleType5":
                    case "ThargoidScoutTissueSample":
                    case "USSCargoBlackBox":
                    case "OccupiedCryoPod":
                    case "DamagedEscapePod":
                    case "WreckageComponents":
                        {
                            Commodity commodity = await Commodity.GetCommodity(Type, dbContext, cancellationToken);
                            CommanderCargoItem? commanderCargoItem = await dbContext.CommanderCargoItems
                                .FirstOrDefaultAsync(c =>
                                    c.Commander == journalParameters.Commander &&
                                    c.SourceStarSystem == journalParameters.CommanderCurrentStarSystem &&
                                    c.Commodity == commodity, cancellationToken);
                            if (commanderCargoItem == null)
                            {
                                commanderCargoItem = new(0, 1)
                                {
                                    Commodity = commodity,
                                    Commander = journalParameters.Commander,
                                    SourceStarSystem = journalParameters.CommanderCurrentStarSystem,
                                };
                                dbContext.CommanderCargoItems.Add(commanderCargoItem);
                            }
                            else
                            {
                                commanderCargoItem.Amount += 1;
                            }
                            await dbContext.SaveChangesAsync(cancellationToken);
                            break;
                        }
                }
            }
        }
    }
}
