namespace Messages
{
    public class StationUpdated
    {
        public long MarketId { get; set; }
        public long SystemAddress { get; set; }

        public StationUpdated(long marketId, long systemAddress)
        {
            MarketId = marketId;
            SystemAddress = systemAddress;
        }
    }
}
