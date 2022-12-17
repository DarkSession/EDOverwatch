namespace EDOverwatch_Web.Models
{
    public class OverwatchOverviewContested
    {
        public int SystemsInInvasion { get; set; }
        public int SystemsWithAlerts { get; set; }
        public int SystemsBeingRecaptured { get; set; }
        public int SystemsInRecovery { get; set; }

        public OverwatchOverviewContested(int systemsInInvasion, int systemsWithAlerts, int systemsBeingRecaptured, int systemsInRecovery)
        {
            SystemsInInvasion = systemsInInvasion;
            SystemsWithAlerts = systemsWithAlerts;
            SystemsBeingRecaptured = systemsBeingRecaptured;
            SystemsInRecovery = systemsInRecovery;
        }
    }
}
