namespace EDDataProcessor.Journal.Events
{
    public class PublicApiEventBase
    {
        [JsonProperty("event", Required = Required.Always)]
        public string Event { get; set; } = string.Empty;

        [JsonProperty("timestamp", Required = Required.Always)]
        public string Timestamp { get; set; } = string.Empty;

        [JsonProperty("cmdr", Required = Required.Always)]
        public string CMDR { get; set; } = string.Empty;

        [JsonProperty("SystemAddress", Required = Required.Always)]
        public long SystemAddress { get; set; }
    }
}
