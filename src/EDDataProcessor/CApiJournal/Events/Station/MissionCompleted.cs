namespace EDDataProcessor.CApiJournal.Events.Station
{
    internal class MissionCompleted : JournalEvent
    {
        public string Name { get; set; }
        public long MissionID { get; set; }

        public MissionCompleted(string name)
        {
            Name = name;
        }
        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (Name.StartsWith("Mission_TW"))
            {
                CommanderMission? commanderMission = await dbContext.CommanderMissions
                    .Include(c => c.System)
                    .FirstOrDefaultAsync(c => c.MissionId == MissionID && c.Commander == journalParameters.Commander && c.Status == CommanderMissionStatus.Accepted, cancellationToken);
                if (commanderMission != null)
                {
                    commanderMission.Status = CommanderMissionStatus.Completed;
                    WarEffortType warEffortType;
                    if (Name.StartsWith("Mission_TW_Collect"))
                    {
                        warEffortType = WarEffortType.MissionCompletionDelivery;
                    }
                    else if (Name.StartsWith("Mission_TW_Rescue"))
                    {
                        warEffortType = WarEffortType.MissionCompletionRescue;
                    }
                    else if (Name.StartsWith("Mission_TW_PassengerEvacuation"))
                    {
                        warEffortType = WarEffortType.MissionCompletionPassengerEvacuation;
                    }
                    else if (Name.StartsWith("Mission_TW_Massacre"))
                    {
                        // Mission_TW_Massacre_Scout_Plural
                        // Mission_TW_Massacre_Cyclops_Plural
                        // Mission_TW_Massacre_Basilisk_Plural
                        // Mission_TW_Massacre_Medusa_Plural
                        // Mission_TW_Massacre_Hydra_Plural
                        warEffortType = WarEffortType.MissionCompletionThargoidKill;
                    }
                    else
                    {
                        warEffortType = WarEffortType.MissionCompletionGeneric;
                    }
                    if (warEffortType == WarEffortType.MissionCompletionGeneric)
                    {
                        await DeferEvent(journalParameters, dbContext, cancellationToken);
                    }
                    else
                    {
                        await AddOrUpdateWarEffort(journalParameters, commanderMission.System, warEffortType, 1, WarEffortSide.Humans, dbContext, cancellationToken);
                    }
                }
            }
        }
    }
}
