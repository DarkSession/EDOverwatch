namespace EDOverwatch_Web.Models
{
    public class OverwatchOverviewContested
    {
        public int SystemsInInvasion { get; set; }
        public int SystemsWithAlerts { get; set; }
        public int SystemsBeingRecaptured { get; set; }

        public OverwatchOverviewContested(int systemsInInvasion, int systemsWithAlerts, int systemsBeingRecaptured)
        {
            SystemsInInvasion = systemsInInvasion;
            SystemsWithAlerts = systemsWithAlerts;
            SystemsBeingRecaptured = systemsBeingRecaptured;
        }
    }
}
