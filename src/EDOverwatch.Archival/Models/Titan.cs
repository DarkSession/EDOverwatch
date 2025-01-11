using EDDatabase;

namespace EDOverwatch.Archival.Models;
internal class Titan(ThargoidMaelstrom maelstrom)
{
    public string Name => maelstrom.Name;
}
