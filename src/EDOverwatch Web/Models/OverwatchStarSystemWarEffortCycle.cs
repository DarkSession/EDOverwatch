namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemWarEffortCycle
    {
        public DateOnly CycleStart { get; }
        public DateOnly CycleEnd { get; }
        public List<OverwatchStarSystemWarEffortCycleEntry> EffortTotals { get; }

        public OverwatchStarSystemWarEffortCycle(DateOnly cycleStart, DateOnly cycleEnd, List<OverwatchStarSystemWarEffortCycleEntry> effortTotals)
        {
            CycleStart = cycleStart;
            CycleEnd = cycleEnd;
            EffortTotals = effortTotals;
        }
    }

    public class OverwatchStarSystemWarEffortCycleEntry
    {
        public string Type { get; }
        public int TypeId { get; }
        public string Source { get; }
        public int SourceId { get; }
        public string TypeGroup { get; }
        public long Amount { get; }

        public OverwatchStarSystemWarEffortCycleEntry(WarEffortType type, WarEffortSource source, long amount)
        {
            Type = type.GetEnumMemberValue();
            TypeId = (int)type;
            Source = source.GetEnumMemberValue();
            SourceId = (int)source;
            if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(type, out WarEffortTypeGroup group))
            {
                TypeGroup = group.GetEnumMemberValue();
            }
            else
            {
                TypeGroup = string.Empty;
            }
            Amount = amount;
        }
    }
}
