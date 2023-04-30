namespace EDDataProcessor.Journal.Events.Station
{
    internal class SearchAndRescue : JournalEvent
    {
        public string Name { get; set; }
        public int Count { get; set; }

        public SearchAndRescue(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<CommanderCargoItem> commanderCargoItems = await dbContext.CommanderCargoItems
                   .Include(i => i.Commodity)
                   .Include(i => i.SourceStarSystem)
                   .Where(c => c.Commander == journalParameters.Commander && c.Commodity!.Name == Name.ToLower())
                   .ToListAsync(cancellationToken);

            int remainingAmount = Count;
            foreach (CommanderCargoItem commanderCargoItem in commanderCargoItems)
            {
                if (commanderCargoItem.Amount <= remainingAmount)
                {
                    await AddOrUpdateWarEffort(journalParameters, commanderCargoItem.SourceStarSystem, WarEffortType.Recovery, commanderCargoItem.Amount, WarEffortSide.Humans, dbContext, cancellationToken);

                    remainingAmount -= commanderCargoItem.Amount;
                    dbContext.CommanderCargoItems.Remove(commanderCargoItem);
                }
                else
                {
                    await AddOrUpdateWarEffort(journalParameters, commanderCargoItem.SourceStarSystem, WarEffortType.Recovery, remainingAmount, WarEffortSide.Humans, dbContext, cancellationToken);

                    remainingAmount = 0;
                    commanderCargoItem.Amount -= remainingAmount;
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
