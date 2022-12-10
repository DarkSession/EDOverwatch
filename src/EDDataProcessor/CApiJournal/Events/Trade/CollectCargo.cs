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
                }
            }
        }
    }
}
