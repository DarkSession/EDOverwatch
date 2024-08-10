namespace EDDataProcessor.CApiJournal.Events.Startup
{
    internal class LoadGame : JournalEvent
    {
        [JsonProperty("gameversion", Required = Required.Default)]
        public string? GameVersion { get; set; }

        [JsonProperty("build")]
        public string GameBuild { get; set; } = string.Empty;

        [JsonIgnore]
        public override bool BypassLiveStatusCheck => true;

        public override ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            journalParameters.Commander.IsInLiveVersion = !string.IsNullOrEmpty(GameVersion) && Version.TryParse(GameVersion, out Version? gameVersion) && gameVersion.Major >= 4 && GameBuild != "r304032/r0";
            return ValueTask.CompletedTask;
        }
    }
}
