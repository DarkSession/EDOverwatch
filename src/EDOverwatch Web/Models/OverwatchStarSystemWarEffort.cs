using EDUtils;

namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemWarEffort
    {
        public DateOnly Date { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public long Amount { get; set; }

        public OverwatchStarSystemWarEffort(WarEffort warEffort)
        {
            Date = warEffort.Date;
            Type = warEffort.Type.GetEnumMemberValue();
            Source = warEffort.Source.GetEnumMemberValue();
            Amount = warEffort.Amount;
        }
    }
}
