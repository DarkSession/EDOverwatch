using System.Runtime.Serialization;

namespace EDDatabase
{
    [Table("DcohFactionOperation")]
    public class DcohFactionOperation
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("FactionId")]
        public DcohFaction? Faction { get; set; }

        [Column]
        public DcohFactionOperationType Type { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [Column]
        public DcohFactionOperationStatus Status { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [ForeignKey("CreatedById")]
        public DcohDiscordUser? CreatedBy { get; set; }

        public DcohFactionOperation(int id, DcohFactionOperationType type, DcohFactionOperationStatus status, DateTimeOffset created)
        {
            Id = id;
            Type = type;
            Status = status;
            Created = created;
        }
    }

    public enum DcohFactionOperationType : byte
    {
        Unknown = 0,
        [EnumMember(Value = "AX Combat")]
        AXCombat,
        Rescue,
        Logistics,
    }

    public enum DcohFactionOperationStatus
    {
        Active = 1,
    }
}
