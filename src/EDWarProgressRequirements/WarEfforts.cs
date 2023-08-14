using EDDatabase;

namespace EDWarProgressRequirements
{
    public static class WarEfforts
    {
        private static List<WarEffortRequirement> TissueRequirements { get; } = new()
        {
            new(StarSystemThargoidLevelState.Controlled, 5, false, 116280),
            new(StarSystemThargoidLevelState.Controlled, 10, false, 11850),
            new(StarSystemThargoidLevelState.Controlled, 15, false, 3100),
            new(StarSystemThargoidLevelState.Controlled, 20, false, 1080),
            new(StarSystemThargoidLevelState.Controlled, 25, false, 460),
            new(StarSystemThargoidLevelState.Controlled, 30, false, 305),
            new(StarSystemThargoidLevelState.Controlled, 35, false, 300),
            new(StarSystemThargoidLevelState.Controlled, 40, false, 300),
            new(StarSystemThargoidLevelState.Controlled, 5, true, 120000),
            new(StarSystemThargoidLevelState.Controlled, 10, true, 58970),
            new(StarSystemThargoidLevelState.Controlled, 15, true, 15500),
            new(StarSystemThargoidLevelState.Controlled, 20, true, 4630),
            new(StarSystemThargoidLevelState.Controlled, 25, true, 1540),
            new(StarSystemThargoidLevelState.Controlled, 30, true, 1505),
            new(StarSystemThargoidLevelState.Controlled, 35, true, 1470),
        };

        public static int? GetRequirements(decimal distanceToMaelstrom, bool wasPopulated, StarSystemThargoidLevelState state)
        {
            int lower5Ly = (int)Math.Round(Math.Floor(distanceToMaelstrom / 5m) * 5m);
            int upper5Ly = lower5Ly + 5;

            WarEffortRequirement? lowerRequirements = TissueRequirements.FirstOrDefault(t => t.State == state && t.IsPopulated == wasPopulated && t.DistanceToMaelstrom == lower5Ly);
            WarEffortRequirement? upperRequirements = TissueRequirements.FirstOrDefault(t => t.State == state && t.IsPopulated == wasPopulated && t.DistanceToMaelstrom == upper5Ly);

            if (lowerRequirements == null || upperRequirements == null)
            {
                return null;
            }

            int requirements = 5;
            requirements += upperRequirements.Requirement;
            int differenceMinMax = Math.Abs(lowerRequirements.Requirement - upperRequirements.Requirement);
            if (differenceMinMax > 0)
            {
                decimal distanceToUpper5Ly = Math.Abs(upper5Ly - distanceToMaelstrom);
                requirements += (int)Math.Ceiling((decimal)differenceMinMax / 5m * distanceToUpper5Ly);
            }
            return (int)(Math.Ceiling((decimal)requirements / 10m) * 10m);
        }
    }

    internal class WarEffortRequirement
    {
        public StarSystemThargoidLevelState State { get; }
        public int DistanceToMaelstrom { get; }
        public bool IsPopulated { get; }
        public int Requirement { get; }

        public WarEffortRequirement(StarSystemThargoidLevelState state, int distanceToMaelstrom, bool isPopulated, int requirement)
        {
            State = state;
            DistanceToMaelstrom = distanceToMaelstrom;
            IsPopulated = isPopulated;
            Requirement = requirement;
        }
    }
}
