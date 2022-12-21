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

        public OverwatchStarSystemWarEffort(EDDatabase.WarEffort warEffort)
        {
            Date = warEffort.Date;
            Type = warEffort.Type.GetEnumMemberValue();
            TypeId = (int)warEffort.Type;
            Source = warEffort.Source.GetEnumMemberValue();
            SourceId = (int)warEffort.Source;
            if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(warEffort.Type, out WarEffortTypeGroup group))
            {
                TypeGroup = group.GetEnumMemberValue();
            }
            else
            {
                TypeGroup = string.Empty;
            }
            Amount = warEffort.Amount;
        }
    }
}
