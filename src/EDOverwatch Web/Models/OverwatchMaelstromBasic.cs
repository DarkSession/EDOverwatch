namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromBasic : OverwatchMaelstrom
    {
        public int SystemsInAlert { get; }
        public int SystemsInInvasion { get; }
        public int SystemsThargoidControlled { get; }
        public int SystemsInRecovery { get; }

        public OverwatchMaelstromBasic(ThargoidMaelstrom thargoidMaelstrom, int systemsInAlert, int systemsInInvasion, int systemsThargoidControlled, int systemsInRecovery) : base(thargoidMaelstrom)
        {
            SystemsInAlert = systemsInAlert;
            SystemsInInvasion = systemsInInvasion;
            SystemsThargoidControlled = systemsThargoidControlled;
            SystemsInRecovery = systemsInRecovery;
        }
    }
}
