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

        [Column]
        public ulong CreatedBy { get; set; }

        public DcohFactionOperation(int id, DcohFactionOperationType type, DcohFactionOperationStatus status, DateTimeOffset created, ulong createdBy)
        {
            Id = id;
            Type = type;
            Status = status;
            Created = created;
            CreatedBy = createdBy;
        }
    }

    public enum DcohFactionOperationType : byte
    {
        AXCombat,
        Rescue,
    }

    public enum DcohFactionOperationStatus
    {
        Active = 1,
    }
}
