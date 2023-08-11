using EDDatabase;
using System.Numerics;

namespace EDOverwatchAlertPrediction
{
    public class StarSystemCycleState
    {
        public long Id { get; }

        public long SystemAddress { get; }

        public string Name { get; }

        public decimal LocationX { get; }

        public decimal LocationY { get; }

        public decimal LocationZ { get; }

        public bool WasPopulated { get; }

        public StarSystemCycleStateThargoidLevel? ThargoidLevel { get; }

        public int LastAttackCycle { get; set; } = int.MinValue;

        public string Maelstrom { get; }

        private int Cycle { get; }

        private int LastAlertCycle { get; }

        private int LastControlCycle { get; }

        public StarSystemCycleState(StarSystem dbStarSystem, int cycle)
        {
            Id = dbStarSystem.Id;
            SystemAddress = dbStarSystem.SystemAddress;
            Name = dbStarSystem.Name;
            LocationX = dbStarSystem.LocationX;
            LocationY = dbStarSystem.LocationY;
            LocationZ = dbStarSystem.LocationZ;
            WasPopulated = dbStarSystem.OriginalPopulation > 0;
            List<StarSystemCycleStateThargoidLevel> systemStates = dbStarSystem.ThargoidLevelHistory!.Select(t => new StarSystemCycleStateThargoidLevel(t)).ToList();
            if (systemStates.FirstOrDefault(s => s.EndCycle == null) is StarSystemCycleStateThargoidLevel currentThargoidLevel && currentThargoidLevel.Expires is int expiresCycle && expiresCycle < cycle &&
                (currentThargoidLevel.State != StarSystemThargoidLevelState.Controlled || currentThargoidLevel.Completed))
            {
                currentThargoidLevel.EndCycle = expiresCycle;
                bool completed = currentThargoidLevel.Completed || currentThargoidLevel.State == StarSystemThargoidLevelState.Recovery;
                StarSystemThargoidLevelState newThargoidLevelState = StarSystemThargoidLevel.GetNextThargoidState(currentThargoidLevel.State, WasPopulated, completed);
                StarSystemCycleStateThargoidLevel newThargoidLevel = new(newThargoidLevelState, expiresCycle + 1, null, null, false);
                systemStates.Add(newThargoidLevel);
            }
            ThargoidLevel = systemStates.SingleOrDefault(s => s.StartCycle <= cycle && (s.EndCycle is not int endCycle || endCycle >= cycle));
            Maelstrom = dbStarSystem.ThargoidLevelHistory!.Where(t => t.State != StarSystemThargoidLevelState.None).FirstOrDefault()?.Maelstrom!.Name ?? string.Empty;
            Cycle = cycle;
            LastAlertCycle = systemStates
                .Where(s => s.State == StarSystemThargoidLevelState.Alert && s.EndCycle < cycle)
                .Select(s => s.EndCycle ?? default)
                .DefaultIfEmpty(int.MinValue)
                .Max();
            LastControlCycle = systemStates
                .Where(s => s.State == StarSystemThargoidLevelState.Controlled && s.EndCycle < cycle)
                .Select(s => s.EndCycle ?? default)
                .DefaultIfEmpty(int.MinValue)
                .Max();
        }

        public int AttackCost()
        {
            if (!WasPopulated && Cycle <= 15)
            {
                return 1;
            }
            return 4;
        }

        public bool CanBeAttacked()
        {
            if (ThargoidLevel != null)
            {
                if (ThargoidLevel?.State != StarSystemThargoidLevelState.None)
                {
                    return false;
                }
                if (ThargoidLevel?.State == StarSystemThargoidLevelState.Recovery)
                {
                    return false;
                }
            }
            if (LastAlertCycle > 0 && (Cycle - LastAlertCycle) <= 2)
            {
                return false;
            }
            if (LastControlCycle > 0 && (Cycle - LastControlCycle) <= 4)
            {
                return false;
            }
            return true;
        }

        public bool CanAttack()
        {
            if (Cycle <= (LastAttackCycle + 1))
            {
                return false;
            }
            if (ThargoidLevel?.StartCycle == Cycle || (ThargoidLevel?.State != StarSystemThargoidLevelState.Controlled && ThargoidLevel?.State != StarSystemThargoidLevelState.Titan))
            {
                return false;
            }
            if (ThargoidLevel != null && ThargoidLevel.State == StarSystemThargoidLevelState.Controlled && ThargoidLevel.Completed)
            {
                return false;
            }
            return true;
        }

        public bool CanAttackSystem(StarSystemCycleState starSystem)
        {
            if (DistanceTo(starSystem) > 10.02f)
            {
                return false;
            }
            return true;
        }

        public float DistanceTo(StarSystemCycleState system) => DistanceTo((float)system.LocationX, (float)system.LocationY, (float)system.LocationZ);

        public float DistanceTo(StarSystem system) => DistanceTo((float)system.LocationX, (float)system.LocationY, (float)system.LocationZ);

        public float DistanceTo(float x, float y, float z) => DistanceTo(new Vector3(x, y, z));

        public float DistanceTo(Vector3 location)
        {
            Vector3 system2 = new((float)LocationX, (float)LocationY, (float)LocationZ);
            return Vector3.Distance(location, system2);
        }
    }
}