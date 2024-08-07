﻿namespace EDDatabase
{
    [Table("CommanderFleetCarrierCargoItem")]
    public class CommanderFleetCarrierCargoItem
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
        public int StackNumber { get; set; }

        [Column]
        public DateTimeOffset CreatedUpdated { get; set; }

        [Column]
        public int Amount { get; set; }

        public CommanderFleetCarrierCargoItem(int id, int amount, int stackNumber, DateTimeOffset createdUpdated)
        {
            Id = id;
            Amount = amount;
            StackNumber = stackNumber;
            CreatedUpdated = createdUpdated;
        }
    }
}
