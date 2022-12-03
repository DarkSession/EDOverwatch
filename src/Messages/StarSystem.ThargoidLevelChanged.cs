namespace Messages
{
    public class StarSystemThargoidLevelChanged
    {
        public long SystemAddress { get; set; }

        public StarSystemThargoidLevelChanged(long systemAddress)
        {
            SystemAddress = systemAddress;
        }
    }
}
