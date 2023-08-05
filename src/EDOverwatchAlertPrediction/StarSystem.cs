using System.Numerics;

namespace EDOverwatchAlertPrediction
{
    public class StarSystem
    {
        public long SystemAddress { get; }

        public string Name { get; }

        public decimal LocationX { get; }

        public decimal LocationY { get; }

        public decimal LocationZ { get; }

        public bool WasPopulated { get; }

        public List<StarSystemState> SystemStates { get; }

        public StarSystem(EDDatabase.StarSystem dbStarSystem)
        {
            SystemAddress = dbStarSystem.SystemAddress;
            Name = dbStarSystem.Name;
            LocationX = dbStarSystem.LocationX;
            LocationY = dbStarSystem.LocationY;
            LocationZ = dbStarSystem.LocationZ;
            WasPopulated = dbStarSystem.OriginalPopulation > 0;
            SystemStates = dbStarSystem.ThargoidLevelHistory!.Select(t => new StarSystemState(t)).ToList();
        }

        public int AttackCost(DateTimeOffset date)
        {
            if (!WasPopulated && date < new DateTimeOffset(2023, 3, 16, 0, 0, 0, TimeSpan.Zero))
            {
                return 1;
            }
            return 4;
        }

        public bool CanBeAttacked(DateTimeOffset date)
        {
            StarSystemState? activeSystemState = SystemStates.FirstOrDefault(s => s.IsActive(date));
            if (activeSystemState?.State != EDDatabase.StarSystemThargoidLevelState.None)
            {
                return false;
            }
            DateTimeOffset lastAlert = SystemStates
                .Where(s => s.State == EDDatabase.StarSystemThargoidLevelState.Alert && s.StateEndDate < date)
                .Select(s => s.StateEndDate ?? default)
                .DefaultIfEmpty()
                .Max();
            if ((date - lastAlert).TotalDays < 14)
            {
                return false;
            }
            DateTimeOffset lastControl = SystemStates
                .Where(s => s.State == EDDatabase.StarSystemThargoidLevelState.Controlled && s.StateEndDate < date)
                .Select(s => s.StateEndDate ?? default)
                .DefaultIfEmpty()
                .Max();
            if ((date - lastControl).TotalDays < 28)
            {
                return false;
            }
            return true;
        }

        public bool CanAttack(StarSystem starSystem, DateTimeOffset date)
        {
            if (DistanceTo(starSystem) >= 10.02f)
            {
                return false;
            }
            StarSystemState? activeSystemState = SystemStates.FirstOrDefault(s => s.IsActive(date));
            if (activeSystemState == null || (activeSystemState.State != EDDatabase.StarSystemThargoidLevelState.Controlled && activeSystemState.State != EDDatabase.StarSystemThargoidLevelState.Titan))
            {
                return false;
            }
            return true;
        }

        public float DistanceTo(StarSystem system) => DistanceTo((float)system.LocationX, (float)system.LocationY, (float)system.LocationZ);

        public float DistanceTo(float x, float y, float z) => DistanceTo(new Vector3(x, y, z));

        public float DistanceTo(Vector3 location)
        {
            Vector3 system2 = new((float)LocationX, (float)LocationY, (float)LocationZ);
            return Vector3.Distance(location, system2);
        }
    }
}