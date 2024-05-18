namespace EDOverwatchAlertPrediction
{
    internal class Attack
    {
        public StarSystemCycleState VictimSystem { get; }
        public List<StarSystemCycleState> Attackers { get; } = [];

        public Attack(StarSystemCycleState victimSystem)
        {
            VictimSystem = victimSystem;
        }
    }
}
