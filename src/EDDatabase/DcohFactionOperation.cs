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

        [Column(TypeName = "varchar(512)")]
        public string? MeetingPoint { get; set; }

        public DcohFactionOperation(int id, DcohFactionOperationType type, DcohFactionOperationStatus status, DateTimeOffset created, string? meetingPoint)
        {
            Id = id;
            Type = type;
            Status = status;
            Created = created;
            MeetingPoint = meetingPoint;
        }
    }

    public enum DcohFactionOperationType : byte
    {
        Unknown = 0,
        [EnumMember(Value = "AX Combat")]
        AXCombat,
        Rescue,
        Logistics,
        [EnumMember(Value = "General Operations")]
        General,
    }

    public enum DcohFactionOperationStatus
    {
        Inactive = 0,
        Active,
        Expired,
    }
}
