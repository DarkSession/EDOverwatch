using EDDatabase;

namespace EDOverwatch.Archival.Models;
internal class ThargoidState(StarSystemThargoidLevel starSystemThargoidLevel)
{
    public string State => starSystemThargoidLevel.State.ToString();
    public Cycle Start => new(starSystemThargoidLevel.CycleStart!);
    public Cycle End => new(starSystemThargoidLevel.CycleEnd!);
    public Titan Titan => new (starSystemThargoidLevel.Maelstrom!);
    public List<ThargoidStateProgress> Progress => starSystemThargoidLevel.ProgressHistory!
        .OrderBy(p => p.Updated)
        .Select(p => new ThargoidStateProgress(p))
        .ToList();
}
