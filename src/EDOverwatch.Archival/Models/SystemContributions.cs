namespace EDOverwatch.Archival.Models;
internal class SystemContributions
{
    public required long Address { get; set; }
    public required string Name { get; set; }
    public required List<Contribution> Contributions { get; set; }
}

internal class Contribution
{
    public required DateOnly Date { get; set; }
    public required string Source { get; set; }
    public required string Type { get; set; }
    public required long Amount { get; set; }
}
