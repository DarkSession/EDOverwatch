namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemWarEffort
    {
        public DateOnly Date { get; }
        public string Type { get; }
        public int TypeId { get; }
        public string Source { get; }
        public int SourceId { get; }
        public string TypeGroup { get; }
        public long Amount { get; }

        public OverwatchStarSystemWarEffort(DateOnly date, WarEffortType type, WarEffortSource source, long amount)
        {
            Date = date;
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
