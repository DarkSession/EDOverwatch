namespace Messages
{
    public class CommanderCApi
    {
        public long FDevCustomerId { get; set; }
        public CommanderCApi(long fDevCustomerId)
        {
            FDevCustomerId = fDevCustomerId;
        }
    }
}
