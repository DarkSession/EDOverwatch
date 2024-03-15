namespace EDOverwatch_Web.Models
{
    public class OverwatchOverviewV2Cycle
    {
        public DateTimeOffset CycleStart { get; }
        public DateTimeOffset CycleEnd { get; }
        public int Alerts { get; }
        public int Invasions { get; }
        public int Controls { get; }
        public int Titans { get; }
        public int Recovery { get; }

        public OverwatchOverviewV2Cycle(DateTimeOffset cycleStart, DateTimeOffset cycleEnd, int alerts, int invasions, int controls, int titans, int recovery)
        {
            CycleStart = cycleStart;
            CycleEnd = cycleEnd;
            Alerts = alerts;
            Invasions = invasions;
            Controls = controls;
            Titans = titans;
            Recovery = recovery;
        }
    }
}
