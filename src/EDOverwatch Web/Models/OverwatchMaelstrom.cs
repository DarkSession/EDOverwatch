using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstrom
    {
        public string Name { get; }

        public string SystemName { get; }

        public long SystemAddress { get; }
        [Obsolete("No longer exists in-game")]

        public short IngameNumber { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        [Description("Shows 'Active' as long the Titan is active or systems remain.")]
        public OverwatchMaelstromState RemainingState { get; }

        public OverwatchMaelstrom(ThargoidMaelstrom thargoidMaelstrom)
        {
            Name = thargoidMaelstrom.Name;
            SystemName = thargoidMaelstrom.StarSystem?.Name ?? string.Empty;
            SystemAddress = thargoidMaelstrom.StarSystem?.SystemAddress ?? 0;
#pragma warning disable CS0618 // Type or member is obsolete
            IngameNumber = thargoidMaelstrom.IngameNumber;
#pragma warning restore CS0618 // Type or member is obsolete
            RemainingState = thargoidMaelstrom.State switch
            {
                ThargoidMaelstromState.Active => OverwatchMaelstromState.Active,
                _ => OverwatchMaelstromState.Completed,
            };
        }
    }

    public enum OverwatchMaelstromState
    {
        Active,
        Completed,
    }
}
