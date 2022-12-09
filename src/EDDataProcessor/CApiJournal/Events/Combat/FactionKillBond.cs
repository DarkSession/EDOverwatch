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

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            if (AwardingFaction == "$faction_PilotsFederation;" && VictimFaction == "$faction_Thargoid;")
            {
                WarEffortType warEffortType = Reward switch
                {
                    80000 => WarEffortType.KillThargoidScout, // Scout
                    8000000 => WarEffortType.KillThargoidCyclops, // Cyclops
                    2400000 => WarEffortType.KillThargoidBasilisk, // Basilisk
                    4000000 => WarEffortType.KillThargoidMedusa, // Medusa
                    6000000 => WarEffortType.KillThargoidHydra, // Hydra
                    5000000 or 300000 => WarEffortType.KillThargoidOrthrus, // Orthrus
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
