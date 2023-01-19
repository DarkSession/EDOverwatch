namespace EDOverwatch_Web.Models
{
    public class OverwatchThargoidLevel
    {
        public StarSystemThargoidLevelState Level { get; }
        public string Name { get; }
        public bool IsInvisibleState { get; }

        public OverwatchThargoidLevel(StarSystemThargoidLevelState level)
        {
            Level = level;
            Name = Level.GetEnumMemberValue();
        }

        public OverwatchThargoidLevel(StarSystemThargoidLevel? starSystemThargoidLevel) :
            this(starSystemThargoidLevel?.State ?? StarSystemThargoidLevelState.None)
        {
            IsInvisibleState = starSystemThargoidLevel?.IsInvisibleState ?? false;
        }
    }
}
