namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemSpecialFactionOperation
    {
        public string Tag { get; }
        public string Name { get; }
        public OverwatchStarSystemSpecialFactionOperation(string tag, string name)
        {
            Tag = tag;
            Name = name;
        }
    }
}
