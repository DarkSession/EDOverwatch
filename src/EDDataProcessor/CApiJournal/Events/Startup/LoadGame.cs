namespace EDDataProcessor.CApiJournal.Events.Startup
{
    internal class LoadGame : JournalEvent
    {
        [JsonProperty("gameversion", Required = Required.Default)]
        public string? GameVersion { get; set; }

        [JsonIgnore]
        public override bool BypassLiveStatusCheck => true;

        public override ValueTask ProcessEvent(Commander commander, EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            commander.IsInLiveVersion = !string.IsNullOrEmpty(GameVersion) && Version.TryParse(GameVersion, out Version? gameVersion) && gameVersion.Major >= 4;
            return ValueTask.CompletedTask;
        }
    }
}
