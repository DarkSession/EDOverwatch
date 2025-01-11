using EDDatabase;

namespace EDOverwatch.Archival.Models;
internal class Cycle(ThargoidCycle thargoidCycle)
{
    public static readonly DateTimeOffset CycleZero = new(2022, 11, 24, 7, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset CycleMax = new(2024, 12, 26, 7, 0, 0, TimeSpan.Zero);

    public int CycleNumber => (int)(thargoidCycle.Start - CycleZero).TotalDays / 7;
    public DateTimeOffset Start => thargoidCycle.Start;
    public DateTimeOffset End => thargoidCycle.End;
}
