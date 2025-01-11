using EDDatabase;

namespace EDOverwatch.Archival.Models;
internal class TitanHearts(ThargoidMaelstrom thargoidMaelstrom)
{
    public string Name => thargoidMaelstrom.Name;
    public DateTimeOffset? MeltdownTimeEstimate => thargoidMaelstrom.MeltdownTimeEstimate;
    public DateTimeOffset? CompletionTimeEstimate => thargoidMaelstrom.CompletionTimeEstimate;
    public List<TitanHeart> Hearts => thargoidMaelstrom.Hearts!
        .Select(h => new TitanHeart(h))
        .OrderBy(h => h.HeartNumber)
        .ToList();
}

internal class  TitanHeart(ThargoidMaelstromHeart thargoidMaelstromHeart)
{
    public int HeartNumber => 9 - thargoidMaelstromHeart.Heart;
    public DateTimeOffset? DestructionTime => thargoidMaelstromHeart.DestructionTime;
}