namespace EDOverwatch_Web.Models
{
    public class OverwatchThargoidLevel
    {
        public StarSystemThargoidLevelState Level { get; }
        public string Name { get; }

        public OverwatchThargoidLevel(StarSystemThargoidLevelState level)
        {
            Level = level;
            Name = level.GetEnumMemberValue();
        }
    }
}
