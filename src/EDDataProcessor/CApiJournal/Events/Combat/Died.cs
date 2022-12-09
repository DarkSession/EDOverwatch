namespace EDDataProcessor.CApiJournal.Events.Combat
{
    internal class Died : JournalEvent
    {
        [JsonProperty(Required = Required.Default)]
        public string? KillerShip { get; set; }

        public override async ValueTask ProcessEvent(Commander commander, EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(KillerShip))
            {
                bool isThargoidKill = KillerShip switch
                {
                    "scout_q" => true,
                    "thargonswarm" => true,
                    "thargon" => true,
                    _ => false
                };
                if (isThargoidKill)
                {
                    await AddOrUpdateWarEffort(commander, WarEffortType.KillGeneric, 1, WarEffortSide.Thargoids, dbContext, cancellationToken);
                }
                else
                {
                    Console.WriteLine("Died. Ship: " + KillerShip);
                }
            }
        }
    }
}
