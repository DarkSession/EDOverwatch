namespace EDDatabase
{
    [Table("CommanderCargo")]
    public class CommanderCargoItem
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        [ForeignKey("CommodityId")]
        public Commodity? Commodity { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? SourceStarSystem { get; set; }

        [Column]
        public int Amount { get; set; }

        public CommanderCargoItem(int id, int amount)
        {
            Id = id;
            Amount = amount;
        }
    }
}
