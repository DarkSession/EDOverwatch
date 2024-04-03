using Newtonsoft.Json;
using System.Text;

namespace EDCApi
{
    public class FleetCarrier
    {
        [JsonProperty("name")]
        public FleetCarrierName? Name { get; set; }

        [JsonProperty("currentStarSystem")]
        public string? CurrentStarSystem { get; set; }

        [JsonProperty("cargo")]
        public List<FleetCarrierCargo>? Cargo { get; set; }
    }

    public class FleetCarrierName
    {
        [JsonProperty("callsign")]
        public string? Callsign { get; set; }

        [JsonProperty("vanityName")]
        public string? VanityName { get; set; }

        [JsonProperty("filteredVanityName")]
        public string? FilteredVanityName { get; set; }

        [JsonIgnore]
        public string? Name => HexToString(VanityName);

        private static string HexToString(string? hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return string.Empty;
            }
            int hexLength = hex.Length;
            byte[] bytes = new byte[hexLength / 2];
            for (int i = 0; i < hexLength; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return Encoding.UTF8.GetString(bytes);
        }
    }

    public class FleetCarrierCargo
    {
        [JsonProperty("commodity")]
        public string? Commodity { get; set; }

        [JsonProperty("qty")]
        public int Qty { get; set; }

        // [JsonProperty("value")]
        // public long Value { get; set; }

        [JsonProperty("originSystem")]
        public long? OriginSystem { get; set; }
    }
}
