using System.Runtime.Serialization;

namespace EDDatabase
{
    [Table("StarSystemThargoidLevel")]
    [Index(nameof(State))]
    public class StarSystemThargoidLevel
    {
        [Column]
        public int Id { get; set; }

        public StarSystem? StarSystem { get; set; }

        [Column]
        public StarSystemThargoidLevelState State { get; set; }

        [ForeignKey("CycleStartId")]
        public ThargoidCycle? CycleStart { get; set; }

        [ForeignKey("CycleStartId")]
        public int? CycleStartId { get; set; }

        [ForeignKey("CycleEndId")]
        public ThargoidCycle? CycleEnd { get; set; }

        [ForeignKey("CycleEndId")]
        public int? CycleEndId { get; set; }

        [ForeignKey("StateExpiresId")]
        public ThargoidCycle? StateExpires { get; set; }

        [ForeignKey("ManualUpdateCycleId")]
        public ThargoidCycle? ManualUpdateCycle { get; set; }

        [ForeignKey("ManualUpdateCycleId")]
        public int? ManualUpdateCycleId { get; set; }

        [ForeignKey("MaelstromId")]
        public ThargoidMaelstrom? Maelstrom { get; set; }

        [ForeignKey("MaelstromId")]
        public int? MaelstromId { get; set; }

        [Column]
        public short? Progress { get; set; }

        [ForeignKey("CurrentProgressId")]
        public StarSystemThargoidLevelProgress? CurrentProgress { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [Column]
        public bool IsInvisibleState { get; set; }

        [Column]
        public bool IsCounterstrike {  get; set; }

        public IEnumerable<StarSystemThargoidLevelProgress>? ProgressHistory { get; set; }

        public StarSystemThargoidLevel(int id, StarSystemThargoidLevelState state, short? progress, DateTimeOffset created, bool isInvisibleState, bool isCounterstrike)
        {
            Id = id;
            State = state;
            Progress = progress;
            Created = created;
            IsInvisibleState = isInvisibleState;
            IsCounterstrike = isCounterstrike;
        }

        public static StarSystemThargoidLevelState GetNextThargoidState(StarSystemThargoidLevelState currentState, bool populated, bool completed)
        {
            if (completed)
            {
                return currentState switch
                {
                    StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.None,
                    StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Recovery,
                    StarSystemThargoidLevelState.Controlled when populated => StarSystemThargoidLevelState.Recovery,
                    StarSystemThargoidLevelState.Recovery => StarSystemThargoidLevelState.None,
                    _ => StarSystemThargoidLevelState.None,
                };
            }

            return currentState switch
            {
                StarSystemThargoidLevelState.Alert when populated => StarSystemThargoidLevelState.Invasion,
                StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.Controlled,
                StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Controlled,
                StarSystemThargoidLevelState.Controlled => StarSystemThargoidLevelState.Controlled,
                _ => StarSystemThargoidLevelState.None,
            };
        }
    }

    public enum StarSystemThargoidLevelState : byte
    {
        [EnumMember(Value = "Clear")]
        None = 0,
        Alert = 20,
        Invasion = 30,
        Controlled = 40,
        Titan = 50,
        // Recapture = 60,
        Recovery = 70,
    }
}
