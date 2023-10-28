namespace EDOverwatch_Web.Models
{
    public class OverwatchThargoidLevel
    {
        public StarSystemThargoidLevelState Level { get; }
        public string Name { get; }
        [Obsolete]
        public bool IsInvisibleState { get; }

        public OverwatchThargoidLevel(StarSystemThargoidLevelState level)
        {
            Level = level;
            Name = Level.GetEnumMemberValue();
        }

        public OverwatchThargoidLevel(StarSystemThargoidLevel? starSystemThargoidLevel) :
            this(starSystemThargoidLevel?.State ?? StarSystemThargoidLevelState.None)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            IsInvisibleState = starSystemThargoidLevel?.IsInvisibleState ?? false;
#pragma warning restore CS0612 // Type or member is obsolete
        }
    }
}
