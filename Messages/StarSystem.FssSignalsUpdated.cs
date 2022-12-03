namespace Messages
{
    public class StarSystemFssSignalsUpdated
    {
        public long SystemAddress { get; set; }

        public StarSystemFssSignalsUpdated(long systemAddress)
        {
            SystemAddress = systemAddress;
        }
    }
}
