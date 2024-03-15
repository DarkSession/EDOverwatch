namespace EDOverwatch_Web.Models
{
    public class OverwatchOverviewV2CycleChange
    {
        public int AlertsDefended { get; }
        public int InvasionsDefended { get; }
        public int ControlsDefended { get; }
        public int TitansDefeated { get; }
        public int ThargoidInvasionStarted { get; }
        public int ThargoidsGained { get; }

        public OverwatchOverviewV2CycleChange(int alertsDefended, int invasionsDefended, int controlsDefended, int titansDefeated, int thargoidInvasionStarted, int thargoidsGained)
        {
            AlertsDefended = alertsDefended;
            InvasionsDefended = invasionsDefended;
            ControlsDefended = controlsDefended;
            TitansDefeated = titansDefeated;
            ThargoidInvasionStarted = thargoidInvasionStarted;
            ThargoidsGained = thargoidsGained;
        }
    }
}
