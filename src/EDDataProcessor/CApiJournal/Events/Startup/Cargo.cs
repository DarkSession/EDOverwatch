﻿namespace EDDataProcessor.CApiJournal.Events.Startup
{
    internal class Cargo : JournalEvent
    {
        public string Vessel { get; set; }

        public List<CargoInventory> Inventory { get; set; }

        public Cargo(string vessel, List<CargoInventory> inventory)
        {
            Vessel = vessel;
            Inventory = inventory;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (Vessel == "Ship")
            {
                List<CommanderCargoItem> commanderCargoItems = await dbContext.CommanderCargoItems
                   .Include(i => i.Commodity)
                   .Where(c => c.Commander == journalParameters.Commander)
                   .ToListAsync(cancellationToken);

                foreach (var commanderCargoItemGroup in commanderCargoItems.GroupBy(c => c.Commodity))
                {
                    string commodityName = commanderCargoItemGroup.Key!.Name;
                    int inDbInventory = commanderCargoItemGroup.Sum(c => c.Amount);
                    int amountInInventory = Inventory.Where(i => i.Name == commodityName).Sum(i => i.Count);
                    if (amountInInventory == 0)
                    {
                        dbContext.CommanderCargoItems.RemoveRange(commanderCargoItems.Where(c => c.Commodity!.Name == commodityName));
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
