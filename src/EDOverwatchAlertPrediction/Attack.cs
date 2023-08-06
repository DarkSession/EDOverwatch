namespace EDOverwatchAlertPrediction
{
    internal class Attack
    {
        public StarSystemCycleState VictimSystem { get; }
        public List<StarSystemCycleState> Attackers { get; } = new();

        public Attack(StarSystemCycleState victimSystem)
        {
            VictimSystem = victimSystem;
        }
    }
}
