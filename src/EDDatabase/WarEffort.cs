using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace EDDatabase
{
    [Table("WarEffort")]
    [Index(nameof(Side))]
    public class WarEffort
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public WarEffortType Type { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [ForeignKey("StarSystemId")]
        public long? StarSystemId { get; set; }

        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        [ForeignKey("CommanderId")]
        public int? CommanderId { get; set; }

        [Column]
        public DateOnly Date { get; set; }

        [Column]
        public long Amount { get; set; }

        [Column]
        public WarEffortSide Side { get; set; }

        [Column]
        public WarEffortSource Source { get; set; }

        public WarEffort(int id, WarEffortType type, DateOnly date, long amount, WarEffortSide side, WarEffortSource source)
        {
            Id = id;
            Type = type;
            Date = date;
            Amount = amount;
            Side = side;
            Source = source;
        }
    }

    public enum WarEffortType : byte
    {
        [EnumMember(Value = "Kills")]
        KillGeneric = 1,

        [EnumMember(Value = "Resuces")]
        Rescue,

        [EnumMember(Value = "Supply Delivery")]
        SupplyDelivery,

        [EnumMember(Value = "Thargoid Scout Kill")]
        KillThargoidScout,

        [EnumMember(Value = "Thargoid Cyclops Kill")]
        KillThargoidCyclops,

        [EnumMember(Value = "Thargoid Basilisk Kill")]
        KillThargoidBasilisk,

        [EnumMember(Value = "Thargoid Medusa Kill")]
        KillThargoidMedusa,

        [EnumMember(Value = "Thargoid Hydra Kill")]
        KillThargoidHydra,

        [EnumMember(Value = "Thargoid Orthrus Kill")]
        KillThargoidOrthrus,

        [EnumMember(Value = "Mission completed")]
        MissionCompletionGeneric,

        [EnumMember(Value = "Delivery mission completed")]
        MissionCompletionDelivery,

        [EnumMember(Value = "Rescue mission completed")]
        MissionCompletionRescue,
    }

    public enum WarEffortSide : byte
    {
        Humans = 1,
        Thargoids,
    }

    public enum WarEffortSource : byte
    {
        Unknown = 0,
        IDA,
        Inara,
        OverwatchCAPI,
    }
}
