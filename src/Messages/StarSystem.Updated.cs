namespace Messages
{
    public class StarSystemUpdated
    {
        public long SystemAddress { get; set; }

        public StarSystemUpdated(long systemAddress)
        {
            SystemAddress = systemAddress;
        }
    }
}