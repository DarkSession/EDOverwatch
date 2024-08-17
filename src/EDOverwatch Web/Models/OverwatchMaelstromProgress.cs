using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromProgress : OverwatchMaelstrom
    {
        public short HeartsRemaining { get; }
        public decimal HeartProgress { get; }
        public decimal TotalProgress { get; }
        public string State { get; }
        public DateTimeOffset? MeltdownTimeEstimate { get; }
        public DateTimeOffset? CompletionTimeEstimate { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TitanCausticLevel CausticLevel { get; }

        public OverwatchMaelstromProgress(ThargoidMaelstrom thargoidMaelstrom) :
            base(thargoidMaelstrom)
        {
            HeartsRemaining = thargoidMaelstrom.HeartsRemaining;
            HeartProgress = thargoidMaelstrom.StarSystem?.ThargoidLevel?.CurrentProgress?.ProgressPercent ?? 0m;
            if (HeartsRemaining > 0)
            {
                State = "Active";
                TotalProgress = 1m - (1m / 8m * thargoidMaelstrom.HeartsRemaining - 1m / 8m * HeartProgress);
                CompletionTimeEstimate = thargoidMaelstrom.CompletionTimeEstimate;
            }
            else
            {
                MeltdownTimeEstimate = thargoidMaelstrom.MeltdownTimeEstimate;
                HeartProgress = 1m;
                TotalProgress = 1m;
                if (thargoidMaelstrom.MeltdownTimeEstimate is DateTimeOffset meltdownEstimate && meltdownEstimate > DateTimeOffset.UtcNow)
                {
                    State = "Meltdown";
                }
                else
                {
                    State = "Destroyed";
                }
            }
            CausticLevel = TitanCausticLevel.None;
            if (thargoidMaelstrom.DefeatCycle is not null && thargoidMaelstrom.MeltdownTimeEstimate is not null && thargoidMaelstrom.MeltdownTimeEstimate <= DateTimeOffset.UtcNow)
            {
                var ticksSinceDefated = WeeklyTick.GetNumberOfTicksSinceDate(thargoidMaelstrom.DefeatCycle.Start);
                if (ticksSinceDefated >= 0)
                {
                    CausticLevel = ticksSinceDefated switch
                    {
                        < 0 => TitanCausticLevel.None,
                        <= 1 => TitanCausticLevel.Extreme,
                        <= 2 => TitanCausticLevel.Medium,
                        <= 3 => TitanCausticLevel.Low,
                        _ => TitanCausticLevel.None,
                    };
                }
            }
        }
    }

    public enum TitanCausticLevel
    {
        None,
        Extreme,
        Medium,
        Low,
    }
}
