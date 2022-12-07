using Newtonsoft.Json;

namespace EDOverwatch_Web.CAPI
{
    internal class Profile
    {
        [JsonProperty("commander")]
        public ProfileCommander? Commander { get; set; }
    }

    internal class ProfileCommander
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        public ProfileCommander(string name)
        {
            Name = name;
        }
    }
}
