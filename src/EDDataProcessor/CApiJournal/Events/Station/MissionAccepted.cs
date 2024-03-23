namespace EDDataProcessor.CApiJournal.Events.Station
{
    internal class MissionAccepted : JournalEvent
    {
        public string Name { get; set; }
        public long MissionID { get; set; }
        public int PassengerCount { get; set; }
        public int Count { get; set; }

        public MissionAccepted(string name, long missionID)
        {
            Name = name;
            MissionID = missionID;
        }
        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (Name.StartsWith("Mission_TW") &&
                !await dbContext.CommanderMissions.AnyAsync(c => c.MissionId == MissionID, cancellationToken))
            {
                int count = Count;
                if (count == 0 && PassengerCount > 0)
                {
                    count = PassengerCount;
                }
                dbContext.CommanderMissions.Add(new(0, MissionID, Timestamp, CommanderMissionStatus.Accepted, count)
                {
                    Commander = journalParameters.Commander,
                    System = journalParameters.CommanderCurrentStarSystem,
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
