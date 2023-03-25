namespace EDDataProcessor.CApiJournal.Events.Trade
{
    internal class MarketSell : JournalEvent
    {
        public string Type { get; set; }

        public int Count { get; set; }

        public MarketSell(string type, int count)
        {
            Type = type;
            Count = count;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (journalParameters.Commander.Station?.IsRescueShip == RescueShipType.Primary ||
                journalParameters.Commander.Station?.IsRescueShip == RescueShipType.Secondary)
            {
                WarEffortType warEffortType = Type.ToLower() switch
                {
                    "thargoidtissuesampletype1" => WarEffortType.TissueSampleCyclops,
                    "thargoidtissuesampletype2" => WarEffortType.TissueSampleBasilisk,
                    "thargoidtissuesampletype3" => WarEffortType.TissueSampleMedusa,
                    "thargoidtissuesampletype4" => WarEffortType.TissueSampleHydra,
                    "thargoidtissuesampletype5" => WarEffortType.TissueSampleOrthrus,
                    "thargoidscouttissuesample" => WarEffortType.TissueSampleScout,
                    _ => default,
                };
                if (warEffortType != default)
                {
                    Commodity commodity = await Commodity.GetCommodity(Type, dbContext, cancellationToken);
                    List<CommanderCargoItem> commanderCargoItems = await dbContext.CommanderCargoItems
                       .Include(i => i.Commodity)
                       .Include(i => i.SourceStarSystem)
                       .Where(c => c.Commander == journalParameters.Commander && c.Commodity == commodity)
                       .ToListAsync(cancellationToken);

                    int remainingAmount = Count;
                    foreach (CommanderCargoItem commanderCargoItem in commanderCargoItems)
                    {
                        if (commanderCargoItem.Amount <= remainingAmount)
                        {
                            await AddOrUpdateWarEffort(journalParameters, commanderCargoItem.SourceStarSystem, warEffortType, commanderCargoItem.Amount, WarEffortSide.Humans, dbContext, cancellationToken);

                            remainingAmount -= commanderCargoItem.Amount;
                            dbContext.CommanderCargoItems.Remove(commanderCargoItem);
                        }
                        else
                        {
                            await AddOrUpdateWarEffort(journalParameters, commanderCargoItem.SourceStarSystem, warEffortType, remainingAmount, WarEffortSide.Humans, dbContext, cancellationToken);

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
    }
}
