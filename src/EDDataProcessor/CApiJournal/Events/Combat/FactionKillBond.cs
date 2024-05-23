namespace EDDataProcessor.CApiJournal.Events.Combat
{
    // { "timestamp":"2023-03-23T17:44:06Z", "event":"FactionKillBond", "Reward":50000000, "AwardingFaction":"$faction_PilotsFederation;", "AwardingFaction_Localised":"Pilots' Federation", "VictimFaction":"$faction_Thargoid;", "VictimFaction_Localised":"Thargoids" }

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
                WarEffortType warEffortType;
                if (Timestamp <= new DateTimeOffset(2024, 5, 28, 8, 0, 0, TimeSpan.Zero))
                {
                    warEffortType = Reward switch
                    {
                        25_000 => WarEffortType.KillThargoidRevenant, // Revenant
                        65_000 or 75_000 => WarEffortType.KillThargoidScout, // Scout
                        1_000_000 => WarEffortType.KillThargoidBanshee, // Banshee
                        4_500_000 => WarEffortType.KillThargoidHunter, // Glaive
                        6_500_000 => WarEffortType.KillThargoidCyclops, // Cyclops
                        20_000_000 => WarEffortType.KillThargoidBasilisk, // Basilisk
                        34_000_000 => WarEffortType.KillThargoidMedusa, // Medusa
                        50_000_000 => WarEffortType.KillThargoidHydra, // Hydra
                        40_000_000 => WarEffortType.KillThargoidOrthrus, // Orthrus
                        _ => WarEffortType.KillGeneric,
                    };
                }
                else
                {
                    warEffortType = Reward switch
                    {
                        25_000 => WarEffortType.KillThargoidRevenant, // Revenant
                        65_000 or 75_000 => WarEffortType.KillThargoidScout, // Scout
                        1_000_000 => WarEffortType.KillThargoidBanshee, // Banshee
                        4_500_000 => WarEffortType.KillThargoidHunter, // Glaive
                        8_000_000 => WarEffortType.KillThargoidCyclops, // Cyclops
                        24_000_000 => WarEffortType.KillThargoidBasilisk, // Basilisk
                        40_000_000 => WarEffortType.KillThargoidMedusa, // Medusa
                        60_000_000 => WarEffortType.KillThargoidHydra, // Hydra
                        15_000_000 => WarEffortType.KillThargoidOrthrus, // Orthrus
                        _ => WarEffortType.KillGeneric,
                    };
                }

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
