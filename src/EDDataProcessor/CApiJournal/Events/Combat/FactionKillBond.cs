namespace EDDataProcessor.CApiJournal.Events.Combat
{
    internal class FactionKillBond : JournalEvent
    {
        public int Reward { get; set; }

        public string AwardingFaction { get; set; }

        public string VictimFaction { get; set; }

        public FactionKillBond(int reward, string awardingFaction, string victimFaction)
        {
            Reward = reward;
            AwardingFaction = awardingFaction;
            VictimFaction = victimFaction;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (AwardingFaction == "$faction_PilotsFederation;" && VictimFaction == "$faction_Thargoid;")
            {
                WarEffortType warEffortType = Reward switch
                {
                    65_000 or 75_000 => WarEffortType.KillThargoidScout, // Scout
                    6_500_000 => WarEffortType.KillThargoidCyclops, // Cyclops
                    20_000_000 => WarEffortType.KillThargoidBasilisk, // Basilisk
                    34_000_000 => WarEffortType.KillThargoidMedusa, // Medusa
                    50_000_000 => WarEffortType.KillThargoidHydra, // Hydra
                    25_000_000 or 40_000_000 => WarEffortType.KillThargoidOrthrus, // Orthrus
                    _ => WarEffortType.KillGeneric,
                };
                if (warEffortType == WarEffortType.KillGeneric)
                {
                    await DeferEvent(journalParameters, dbContext, cancellationToken);
                }
                else
                {
                    await AddOrUpdateWarEffort(journalParameters, warEffortType, 1, WarEffortSide.Humans, dbContext, cancellationToken);
                }
            }
        }
    }
}
