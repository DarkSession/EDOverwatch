﻿namespace EDOverwatch_Web.Models
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
        public List<string> Features { get; } = [];

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
                    bool groundPortUnderAttack,
                    bool hasAlertPrediction)
            : base(starSystem, hasAlertPrediction)
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
#pragma warning disable CS0618 // Type or member is obsolete
                Features.Add(OverwatchStarSystemFeature.BarnacleMatrix.ToString());
#pragma warning restore CS0618 // Type or member is obsolete
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
            if ((ThargoidLevel.Level == StarSystemThargoidLevelState.Alert || ThargoidLevel.Level == StarSystemThargoidLevelState.Invasion) && starSystem.ReactivationMissionsNearby && (StateProgress.ProgressPercent ?? 0) < 1m)
            {
                Features.Add(OverwatchStarSystemFeature.ThargoidControlledReactivationMissions.ToString());
            }
            var isCompleted = (StateProgress.ProgressPercent ?? 0) >= 1m;
            if (axConflictZones && !isCompleted
                && (ThargoidLevel.Level == StarSystemThargoidLevelState.Controlled || (ThargoidLevel.Level == StarSystemThargoidLevelState.Invasion && StateExpiration != null && StateExpiration.RemainingCycles > 0)))
            {
                Features.Add(OverwatchStarSystemFeature.AXConflictZones.ToString());
            }
            if (groundPortUnderAttack && (ThargoidLevel.Level == StarSystemThargoidLevelState.Controlled || ThargoidLevel.Level == StarSystemThargoidLevelState.Invasion) && (StateProgress.ProgressPercent ?? 0) < 1m)
            {
                Features.Add(OverwatchStarSystemFeature.GroundPortAXCZ.ToString());
            }
            if ((starSystem.ThargoidLevel?.IsCounterstrike ?? false) && ThargoidLevel.Level == StarSystemThargoidLevelState.Controlled)
            {
                Features.Add(OverwatchStarSystemFeature.Counterstrike.ToString());
            }
            if (!isCompleted)
            {
                if (StationsUnderRepair > 0)
                {
                    Features.Add(OverwatchStarSystemFeature.StarportUnderRepair.ToString());
                }
                if (StationsDamaged > 0)
                {
                    Features.Add(OverwatchStarSystemFeature.StarportDamaged.ToString());
                }
                if (StationsUnderAttack > 0)
                {
                    Features.Add(OverwatchStarSystemFeature.StarportUnderAttack.ToString());
                }
            }
        }

        public static async Task<(int startDateHour, int totalActivity)> GetTotalPlayerActivity(EdDbContext dbContext)
        {
            var time = WeeklyTick.GetLastTick();
            if (DateTimeOffset.UtcNow.AddHours(-6) > time)
            {
                time = DateTimeOffset.UtcNow.AddHours(-6);
            }

            var dateHour = time.Year * 1000000 + time.Month * 10000 + time.Day * 100 + time.Hour;

            var totalActivity = await dbContext.PlayerActivities
                .Where(p => p.DateHour >= dateHour)
                .CountAsync();

            return (dateHour, totalActivity);
        }
    }

    public enum OverwatchStarSystemFeature
    {
        [Obsolete("Use ThargoidSpires instead")]
        BarnacleMatrix,
        ThargoidSpires,
        OdysseySettlements,
        FederalFaction,
        ImperialFaction,
        ThargoidControlledReactivationMissions,
        AXConflictZones,
        GroundPortAXCZ,
        Counterstrike,
        StarportUnderAttack,
        StarportDamaged,
        StarportUnderRepair,
    }
}
