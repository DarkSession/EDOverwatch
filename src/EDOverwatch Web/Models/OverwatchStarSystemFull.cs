namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemFull : OverwatchStarSystem
    {
        public decimal EffortFocus { get; }
        public int FactionOperations { get; protected set; }
        public int FactionAxOperations { get; protected set; }
        public int FactionGeneralOperations { get; protected set; }
        public int FactionRescueOperations { get; protected set; }
        public int FactionLogisticsOperations { get; protected set; }
        public List<OverwatchStarSystemSpecialFactionOperation> SpecialFactionOperations { get; }
        public int StationsUnderRepair { get; protected set; }
        public int StationsDamaged { get; protected set; }
        public int StationsUnderAttack { get; protected set; }
        public List<string> Features { get; } = new();

        public OverwatchStarSystemFull(
                    StarSystem starSystem,
                    decimal effortFocus,
                    int factionAxOperations,
                    int factionGeneralOperations,
                    int factionRescueOperations,
                    int factionLogisticsOperations,
                    List<OverwatchStarSystemSpecialFactionOperation> specialFactionOperations,
                    int stationsUnderRepair,
                    int stationsDamaged,
                    int stationsUnderAttack,
                    bool odysseySettlements,
                    bool federalFaction,
                    bool imperialFaction,
                    bool axConflictZones,
                    bool groundPortUnderAttack)
            :  base(starSystem)
        {
            EffortFocus = effortFocus;
            FactionOperations = (factionAxOperations + factionGeneralOperations + factionRescueOperations + factionLogisticsOperations);
            FactionAxOperations = factionAxOperations;
            FactionGeneralOperations = factionGeneralOperations;
            FactionRescueOperations = factionRescueOperations;
            FactionLogisticsOperations = factionLogisticsOperations;
            SpecialFactionOperations = specialFactionOperations;
            StationsUnderRepair = stationsUnderRepair;
            StationsDamaged = stationsDamaged;
            StationsUnderAttack = stationsUnderAttack;
            if (starSystem.BarnacleMatrixInSystem)
            {
#pragma warning disable CS0612 // Type or member is obsolete
                Features.Add(OverwatchStarSystemFeature.BarnacleMatrix.ToString());
#pragma warning restore CS0612 // Type or member is obsolete
                Features.Add(OverwatchStarSystemFeature.ThargoidSpires.ToString());
            }
            if (odysseySettlements)
            {
                Features.Add(OverwatchStarSystemFeature.OdysseySettlements.ToString());
            }
            if (ThargoidLevel.Level != StarSystemThargoidLevelState.Controlled)
            {
                if (federalFaction)
                {
                    Features.Add(OverwatchStarSystemFeature.FederalFaction.ToString());
                }
                if (imperialFaction)
                {
                    Features.Add(OverwatchStarSystemFeature.ImperialFaction.ToString());
                }
            }
            if (ThargoidLevel.Level == StarSystemThargoidLevelState.Alert && starSystem.ReactivationMissionsNearby)
            {
                Features.Add(OverwatchStarSystemFeature.ThargoidControlledReactivationMissions.ToString());
            }
            if (axConflictZones && ThargoidLevel.Level == StarSystemThargoidLevelState.Controlled && (StateProgress.ProgressPercent ?? 0) < 1m)
            {
                Features.Add(OverwatchStarSystemFeature.AXConflictZones.ToString());
            }
            if (groundPortUnderAttack && ThargoidLevel.Level == StarSystemThargoidLevelState.Invasion && (StateProgress.ProgressPercent ?? 0) < 1m)
            {
                Features.Add(OverwatchStarSystemFeature.GroundPortAXCZ.ToString());
            }
            if ((starSystem.ThargoidLevel?.IsCounterstrike ?? false) && ThargoidLevel.Level == StarSystemThargoidLevelState.Controlled)
            {
                Features.Add(OverwatchStarSystemFeature.Counterstrike.ToString());
            }
        }
    }

    public enum OverwatchStarSystemFeature
    {
        [Obsolete]
        BarnacleMatrix,
        ThargoidSpires,
        OdysseySettlements,
        FederalFaction,
        ImperialFaction,
        ThargoidControlledReactivationMissions,
        AXConflictZones,
        GroundPortAXCZ,
        Counterstrike,
    }
}
