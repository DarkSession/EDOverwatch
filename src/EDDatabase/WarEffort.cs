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

        [ForeignKey("CycleId")]
        public ThargoidCycle? Cycle { get; set; }

        [ForeignKey("CycleId")]
        public int? CycleId { get; set; }

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

        public static Dictionary<WarEffortType, WarEffortTypeGroup> WarEffortGroups => new()
        {
            { WarEffortType.KillGeneric, WarEffortTypeGroup.Kills },
            { WarEffortType.KillThargoidScout, WarEffortTypeGroup.Kills },
            { WarEffortType.KillThargoidCyclops, WarEffortTypeGroup.Kills },
            { WarEffortType.KillThargoidBasilisk, WarEffortTypeGroup.Kills},
            { WarEffortType.KillThargoidMedusa, WarEffortTypeGroup.Kills },
            { WarEffortType.KillThargoidHydra, WarEffortTypeGroup.Kills },
            { WarEffortType.KillThargoidOrthrus, WarEffortTypeGroup.Kills },
            { WarEffortType.Rescue, WarEffortTypeGroup.Rescue },
            { WarEffortType.SupplyDelivery, WarEffortTypeGroup.Supply },
            { WarEffortType.MissionCompletionGeneric, WarEffortTypeGroup.Mission },
            { WarEffortType.MissionCompletionThargoidKill, WarEffortTypeGroup.Mission },
            { WarEffortType.MissionCompletionPassengerEvacuation, WarEffortTypeGroup.Mission },
            { WarEffortType.MissionCompletionDelivery, WarEffortTypeGroup.Mission },
            { WarEffortType.MissionCompletionRescue, WarEffortTypeGroup.Mission },
            { WarEffortType.MissionCompletionSettlementReboot, WarEffortTypeGroup.Mission },
            { WarEffortType.ThargoidProbeCollection, WarEffortTypeGroup.Recovery },
            { WarEffortType.Recovery, WarEffortTypeGroup.Recovery },
        };
    }

    public enum WarEffortType : byte
    {
        [EnumMember(Value = "Kills")]
        KillGeneric = 1,

        [EnumMember(Value = "Rescues")]
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

        [EnumMember(Value = "Thargoid kill mission completed")]
        MissionCompletionThargoidKill,

        [EnumMember(Value = "Passenger evacuation mission completed")]
        MissionCompletionPassengerEvacuation,

        [EnumMember(Value = "Thargoid probe collected")]
        ThargoidProbeCollection,

        [EnumMember(Value = "Recovery")]
        Recovery,

        [EnumMember(Value = "Settlement reboot mission completed")]
        MissionCompletionSettlementReboot,
    }

    public enum WarEffortTypeGroup : byte
    {
        Kills,
        [EnumMember(Value = "Rescues")]
        Rescue,
        [EnumMember(Value = "Supplies")]
        Supply,
        [EnumMember(Value = "Missions")]
        Mission,
        [EnumMember(Value = "Recovery")]
        Recovery,
    }

    public enum WarEffortSide : byte
    {
        Humans = 1,
        Thargoids,
    }

    public enum WarEffortSource : byte
    {
        Unknown = 0,
        [EnumMember(Value = "Operation IDA")]
        IDA,
        Inara,
        [EnumMember(Value = "ED: Overwatch")]
        OverwatchCAPI,
    }
}
