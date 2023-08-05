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
            { WarEffortType.ThargoidProbeCollection, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.Recovery, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleScout, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleCyclops, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleBasilisk, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleMedusa, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleHydra, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleOrthrus, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleGlaive, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleTitan, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.TissueSampleTitanMaw, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.ProtectiveMembraneScrap, WarEffortTypeGroup.RecoveryAndProbing },
            { WarEffortType.KillThargoidHunter, WarEffortTypeGroup.Kills },
            { WarEffortType.KillThargoidRevenant, WarEffortTypeGroup.Kills },
            { WarEffortType.MissionCompletionThargoidControlledSettlementReboot, WarEffortTypeGroup.Mission },
        };
    }

    public enum WarEffortType : byte
    {
        [EnumMember(Value = "Kills")]
        KillGeneric = 1,

        [EnumMember(Value = "Rescues")]
        Rescue,

        [EnumMember(Value = "Supply delivery")]
        SupplyDelivery,

        [EnumMember(Value = "Thargoid Scout kill")]
        KillThargoidScout,

        [EnumMember(Value = "Thargoid Cyclops kill")]
        KillThargoidCyclops,

        [EnumMember(Value = "Thargoid Basilisk kill")]
        KillThargoidBasilisk,

        [EnumMember(Value = "Thargoid Medusa kill")]
        KillThargoidMedusa,

        [EnumMember(Value = "Thargoid Hydra kill")]
        KillThargoidHydra,

        [EnumMember(Value = "Thargoid Orthrus kill")]
        KillThargoidOrthrus,

        [EnumMember(Value = "Mission")]
        MissionCompletionGeneric,

        [EnumMember(Value = "Delivery mission")]
        MissionCompletionDelivery,

        [EnumMember(Value = "Rescue mission")]
        MissionCompletionRescue,

        [EnumMember(Value = "Thargoid kill mission")]
        MissionCompletionThargoidKill,

        [EnumMember(Value = "Passenger evacuation mission")]
        MissionCompletionPassengerEvacuation,

        [EnumMember(Value = "Thargoid probe collected")]
        ThargoidProbeCollection,

        [EnumMember(Value = "Recovery")]
        Recovery,

        [EnumMember(Value = "Settlement reboot mission")]
        MissionCompletionSettlementReboot,

        [EnumMember(Value = "Thargoid Scout tissue sample collection")]
        TissueSampleScout,

        [EnumMember(Value = "Thargoid Cyclops tissue sample collection")]
        TissueSampleCyclops,

        [EnumMember(Value = "Thargoid Basilisk tissue sample collection")]
        TissueSampleBasilisk,

        [EnumMember(Value = "Thargoid Medusa tissue sample collection")]
        TissueSampleMedusa,

        [EnumMember(Value = "Thargoid Hydra tissue sample collection")]
        TissueSampleHydra,

        [EnumMember(Value = "Thargoid Orthrus tissue sample collection")]
        TissueSampleOrthrus,

        [EnumMember(Value = "Thargoid Hunter kill")]
        KillThargoidHunter,

        [EnumMember(Value = "Thargoid Revenant kill")]
        KillThargoidRevenant,

        [EnumMember(Value = "Thargoid controlled settlement reboot mission")]
        MissionCompletionThargoidControlledSettlementReboot,

        [EnumMember(Value = "Thargoid Glaive tissue sample collection")]
        TissueSampleGlaive,

        [EnumMember(Value = "Thargoid Titan tissue sample collection")]
        TissueSampleTitan,

        [EnumMember(Value = "Thargoid Titan maw sample collection")]
        TissueSampleTitanMaw,

        [EnumMember(Value = "Protective membrane scrap collection")]
        ProtectiveMembraneScrap,
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
        [EnumMember(Value = "Recovery and probing")]
        RecoveryAndProbing,
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
