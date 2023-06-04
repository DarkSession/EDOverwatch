using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDDatabase
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
        public int Amount { get; set; }

        public CommanderFleetCarrierCargoItem(int id, int amount)
        {
            Id = id;
            Amount = amount;
        }
    }
}
