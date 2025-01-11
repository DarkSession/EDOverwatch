using EDDatabase;

namespace EDOverwatch.Archival.Models;
internal class SingleSystem(StarSystem starSystem, int? cycleNumber)
{
    public long Address => starSystem.SystemAddress;
    public string Name => starSystem.Name;
    public long Population => starSystem.Population;
    public long LowestRecordedPopulation => starSystem.PopulationMin;
    public decimal X => starSystem.LocationX;
    public decimal Y => starSystem.LocationY;
    public decimal Z => starSystem.LocationZ;
    public List<ThargoidState> States => starSystem.ThargoidLevelHistory!
        .Where(s => s.CycleEndId is not null)
        .Select(s => new ThargoidState(s))
        .Where(s => s.Start.CycleNumber <= s.End.CycleNumber)
        .Where(s => cycleNumber is not int c || (s.State != "None" && s.Start.CycleNumber <= c && s.End.CycleNumber >= c))
        .OrderBy(s => s.Start.CycleNumber)
        .ToList();
}
