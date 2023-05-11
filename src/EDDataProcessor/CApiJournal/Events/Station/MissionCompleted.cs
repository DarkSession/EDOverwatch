namespace EDDataProcessor.CApiJournal.Events.Station
{
    internal class MissionCompleted : JournalEvent
    {
        public string Name { get; set; }
        public long MissionID { get; set; }
        public int Count { get; set; }
        public int PassengerCount { get; set; }

        public MissionCompleted(string name, long missionId)
        {
            Name = name;
            MissionID = missionId;
        }
        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (Name.StartsWith("Mission_TW"))
            {
                CommanderMission? commanderMission = await dbContext.CommanderMissions
                    .Include(c => c.System)
                    .FirstOrDefaultAsync(c => c.MissionId == MissionID && c.Commander == journalParameters.Commander && c.Status == CommanderMissionStatus.Accepted, cancellationToken);
                if (commanderMission == null)
                {
                    // Sometimes the MissionAccepted event isn't written in the game journal
                    // Then we simulate the mission accepted event
                    // The current system parameter might be wrong for some mission types however, namely rescue and evac missions
                    MissionAccepted missionAccepted = new(Name, MissionID)
                    {
                        Timestamp = Timestamp,
                        Event = nameof(MissionAccepted),
                        Count = Count,
                        PassengerCount = PassengerCount,
                    };
                    await missionAccepted.ProcessEvent(journalParameters, dbContext, cancellationToken);

                    commanderMission = await dbContext.CommanderMissions
                        .Include(c => c.System)
                        .FirstOrDefaultAsync(c => c.MissionId == MissionID && c.Commander == journalParameters.Commander && c.Status == CommanderMissionStatus.Accepted, cancellationToken);
                }

                if (commanderMission != null && commanderMission.Status != CommanderMissionStatus.Completed)
                {
                    WarEffortType missionWarEffortType;
                    WarEffortType? countWarEffortType = null;
                    if (Name.StartsWith("Mission_TW_Collect"))
                    {
                        missionWarEffortType = WarEffortType.MissionCompletionDelivery;
                        countWarEffortType = WarEffortType.SupplyDelivery;
                    }
                    else if (Name.StartsWith("Mission_TW_Rescue"))
                    {
                        missionWarEffortType = WarEffortType.MissionCompletionRescue;
                        countWarEffortType = WarEffortType.Rescue;
                    }
                    else if (Name.StartsWith("Mission_TW_PassengerEvacuation"))
                    {
                        missionWarEffortType = WarEffortType.MissionCompletionPassengerEvacuation;
                        countWarEffortType = WarEffortType.Rescue;
                    }
                    else if (Name.StartsWith("Mission_TW_Massacre"))
                    {
                        // Mission_TW_Massacre_Scout_Plural
                        // Mission_TW_Massacre_Cyclops_Plural
                        // Mission_TW_Massacre_Basilisk_Plural
                        // Mission_TW_Massacre_Medusa_Plural
                        // Mission_TW_Massacre_Hydra_Plural
                        missionWarEffortType = WarEffortType.MissionCompletionThargoidKill;
                    }
                    else if (Name.StartsWith("Mission_TW_OnFoot_Reboot_NR") || Name.StartsWith("Mission_TW_OnFoot_Reboot_MB"))
                    {
                        missionWarEffortType = WarEffortType.MissionCompletionSettlementReboot;
                    }
                    else if (Name.StartsWith("Mission_TW_OnFoot_Reboot_Occupied_MB"))
                    {
                        missionWarEffortType = WarEffortType.MissionCompletionThargoidControlledSettlementReboot;
                    }
                    else
                    {
                        await DeferEvent(journalParameters, dbContext, cancellationToken);
                        return;
                    }
                    commanderMission.Status = CommanderMissionStatus.Completed;
                    await AddOrUpdateWarEffort(journalParameters, commanderMission.System, missionWarEffortType, 1, WarEffortSide.Humans, dbContext, cancellationToken);
                    if (commanderMission.Count > 0 && countWarEffortType != null)
                    {
                        await AddOrUpdateWarEffort(journalParameters, commanderMission.System, (WarEffortType)countWarEffortType, commanderMission.Count, WarEffortSide.Humans, dbContext, cancellationToken);
                    }
                }
            }
        }
    }
}
