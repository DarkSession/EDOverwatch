using Newtonsoft.Json;

namespace EDCApi
{
    public class Profile
    {
        [JsonProperty("commander")]
        public ProfileCommander? Commander { get; set; }
    }

    public class ProfileCommander
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        public ProfileCommander(string name)
        {
            Name = name;
        }
    }
}
