namespace EDDataProcessor.CApiJournal.Events.Trade
{
    internal class EjectCargo : JournalEvent
    {
        public string Type { get; set; }

        public int Count { get; set; }

        public EjectCargo(string type, int count)
        {
            Type = type;
            Count = count;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            Commodity commodity = await Commodity.GetCommodity(Type, dbContext, cancellationToken);
            List<CommanderCargoItem> commanderCargoItems = await dbContext.CommanderCargoItems
                .Where(c =>
                    c.Commander == journalParameters.Commander &&
                    c.Commodity == commodity)
                .ToListAsync(cancellationToken);
            int remainingAmount = Count;
            foreach (CommanderCargoItem commanderCargoItem in commanderCargoItems)
            {
                if (commanderCargoItem.Amount <= remainingAmount)
                {
                    remainingAmount -= commanderCargoItem.Amount;
                    commanderCargoItem.Amount = 0;
                    dbContext.CommanderCargoItems.Remove(commanderCargoItem);
                }
                else
                {
                    commanderCargoItem.Amount -= remainingAmount;
                    remainingAmount = 0;
                }

                if (remainingAmount <= 0)
                {
                    break;
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
