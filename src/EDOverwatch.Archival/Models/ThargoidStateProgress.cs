using EDDatabase;

namespace EDOverwatch.Archival.Models;
internal class ThargoidStateProgress(StarSystemThargoidLevelProgress starSystemThargoidLevelProgress)
{
    public decimal Progress => starSystemThargoidLevelProgress.ProgressPercent ?? 0m;
    public DateTimeOffset Reported => starSystemThargoidLevelProgress.Updated;
    public DateTimeOffset Updated => starSystemThargoidLevelProgress.LastChecked;
}
