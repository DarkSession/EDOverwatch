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
        public override async ValueTask ProcessEvent(Commander commander, EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            if (Name.StartsWith("Mission_TW"))
            {
                CommanderMission? commanderMission = await dbContext.CommanderMissions
                    .Include(c => c.System)
                    .FirstOrDefaultAsync(c => c.MissionId == MissionID && c.Commander == commander && c.Status == CommanderMissionStatus.Accepted, cancellationToken);
                    if (commanderMission != null)
                {
                    commanderMission.Status = CommanderMissionStatus.Completed;
                    WarEffortType warEffortType = Name switch
                    {
                        "Mission_TW_Collect_Alert_name" => WarEffortType.MissionCompletionDelivery,
                        "Mission_TW_Rescue_Alert_name" => WarEffortType.MissionCompletionDelivery,
                        _ => WarEffortType.MissionCompletionGeneric,
                    };
                    await AddOrUpdateWarEffort(commander, commanderMission.System, warEffortType, 1, WarEffortSide.Humans, dbContext, cancellationToken);
                }
            }
        }
    }
}
