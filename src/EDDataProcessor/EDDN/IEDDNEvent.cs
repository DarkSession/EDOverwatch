namespace EDDataProcessor.EDDN
{
    internal interface IEDDNEvent
    {
        public ValueTask ProcessEvent(EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken);
    }
}
