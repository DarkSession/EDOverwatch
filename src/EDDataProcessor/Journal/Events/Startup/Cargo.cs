namespace EDDataProcessor.Journal.Events.Startup
{
    internal class Cargo : JournalEvent
    {
        public string Vessel { get; set; }

        public int Count { get; set; }

        public List<CargoInventory>? Inventory { get; set; }

        public Cargo(string vessel)
        {
            Vessel = vessel;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (Vessel == "Ship" && (Inventory != null || Count == 0))
            {
                Inventory ??= new();
                List<CommanderCargoItem> commanderCargoItems = await dbContext.CommanderCargoItems
                   .Include(i => i.Commodity)
                   .Where(c => c.Commander == journalParameters.Commander && c.Commodity != null)
                   .ToListAsync(cancellationToken);

                foreach (var commanderCargoItemGroup in commanderCargoItems.GroupBy(c => c.Commodity!.Name))
                {
                    string commodityName = commanderCargoItemGroup.Key;
                    int inDbInventory = commanderCargoItemGroup.Sum(c => c.Amount);
                    int amountInInventory = Inventory
                        .Where(i => i.Name.ToLower() == commodityName)
                        .Sum(i => i.Count);
                    if (amountInInventory == 0)
                    {
                        if (commanderCargoItems.Any(c => c.Commodity!.Name == commodityName))
                        {
                            dbContext.CommanderCargoItems.RemoveRange(commanderCargoItems.Where(c => c.Commodity!.Name == commodityName));
                        }
                    }
                    else if (amountInInventory < inDbInventory)
                    {
                        int remainingAmount = inDbInventory;
                        foreach (CommanderCargoItem commanderCargoItem in commanderCargoItems.Where(c => c.Commodity!.Name == commodityName))
                        {
                            if (commanderCargoItem.Amount <= remainingAmount)
                            {
                                remainingAmount -= commanderCargoItem.Amount;
                                commanderCargoItem.Amount = 0;
                                dbContext.CommanderCargoItems.Remove(commanderCargoItem);
                            }
                            else
                            {
                                remainingAmount = 0;
                                commanderCargoItem.Amount -= remainingAmount;
                            }

                            if (remainingAmount <= 0)
                            {
                                break;
                            }
                        }
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    internal class CargoInventory
    {
        public string Name { get; set; }
        public int Count { get; set; }

        public CargoInventory(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }
}
