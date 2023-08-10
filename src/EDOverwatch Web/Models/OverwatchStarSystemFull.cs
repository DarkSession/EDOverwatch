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
                    bool imperialFaction)
            : base(starSystem)
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
                Features.Add(OverwatchStarSystemFeature.BarnacleMatrix.ToString());
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
        }
    }

    public enum OverwatchStarSystemFeature
    {
        BarnacleMatrix,
        OdysseySettlements,
        FederalFaction,
        ImperialFaction,
        ThargoidControlledReactivationMissions,
    }
}
