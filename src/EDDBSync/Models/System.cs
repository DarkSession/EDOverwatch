using Newtonsoft.Json;

namespace EDDBSync.Models
{
    public class System
    {
        public static string Url { get; } = "https://eddb.io/archive/v6/systems_populated.json";

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("x")]
        public decimal X { get; set; }

        [JsonProperty("y")]
        public decimal Y { get; set; }

        [JsonProperty("z")]
        public decimal Z { get; set; }

        [JsonProperty("ed_system_address")]
        public long SystemAddress { get; set; }

        [JsonProperty("population")]
        public long? Population { get; set; }

        [JsonProperty("allegiance")]
        public string? Allegiance { get; set; }

        public System(int id, string name, decimal x, decimal y, decimal z, long systemAddress, long? population, string? allegiance)
        {
            Id = id;
            Name = name;
            X = x;
            Y = y;
            Z = z;
            SystemAddress = systemAddress;
            Population = population;
            Allegiance = allegiance;
        }
    }
}
