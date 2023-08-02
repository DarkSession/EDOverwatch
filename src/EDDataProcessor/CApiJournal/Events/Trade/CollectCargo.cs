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
                WarEffortType? warEffort = Type.ToLower() switch
                {
                    "unknownartifact2" => WarEffortType.ThargoidProbeCollection,
                    "thargoidtissuesampletype1" => WarEffortType.TissueSampleCyclops,
                    "thargoidtissuesampletype2" => WarEffortType.TissueSampleBasilisk,
                    "thargoidtissuesampletype3" => WarEffortType.TissueSampleMedusa,
                    "thargoidtissuesampletype4" => WarEffortType.TissueSampleHydra,
                    "thargoidtissuesampletype5" => WarEffortType.TissueSampleOrthrus,
                    "thargoidscouttissuesample" => WarEffortType.TissueSampleScout,
                    "thargoidtissuesampletype6" => WarEffortType.TissueSampleGlaive,
                    "thargoidtissuesampletype9a" => WarEffortType.TissueSampleTitan,
                    "thargoidtissuesampletype9b" => WarEffortType.TissueSampleTitan,
                    "thargoidtissuesampletype9c" => WarEffortType.TissueSampleTitan,
                    "thargoidtissuesampletype10a" => WarEffortType.TissueSampleTitanMaw,
                    "thargoidtissuesampletype10b" => WarEffortType.TissueSampleTitanMaw,
                    "thargoidtissuesampletype10c" => WarEffortType.TissueSampleTitanMaw,
                    "unknownsack" => WarEffortType.ProtectiveMembraneScrap,
                    "usscargoblackbox" => WarEffortType.Recovery,
                    "occupiedcryopod" => WarEffortType.Recovery,
                    "damagedescapepod" => WarEffortType.Recovery,
                    "wreckagecomponents" => WarEffortType.Recovery,
                    _ => default,
                };
                if (warEffort is WarEffortType warEffortType && warEffortType != default)
                {
                    await AddOrUpdateWarEffort(journalParameters, warEffortType, 1, WarEffortSide.Humans, dbContext, cancellationToken);
                }
            }
        }
    }
}
