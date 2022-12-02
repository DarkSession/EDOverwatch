namespace EDDataProcessor.EDDN
{
    public class Header
    {
        [JsonProperty("uploaderID", Required = Required.Always)]
        public string UploaderID { get; set; } = string.Empty;

        [JsonProperty("softwareName", Required = Required.Always)]
        public string SoftwareName { get; set; } = string.Empty;

        [JsonProperty("softwareVersion", Required = Required.Always)]
        public string SoftwareVersion { get; set; } = string.Empty;

        [JsonProperty("gameversion", Required = Required.Default)]
        public string GameVersion { get; set; } = string.Empty;

        [JsonIgnore]
        public bool IsLive => !string.IsNullOrEmpty(GameVersion) && Version.TryParse(GameVersion, out Version? gameVersion) && gameVersion.Major >= 4;

        /// <summary>Timestamp upon receipt at the gateway. If present, this property will be overwritten by the gateway; submitters are not intended to populate this property.</summary>
        [JsonProperty("gatewayTimestamp", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset GatewayTimestamp { get; set; }
    }
}
