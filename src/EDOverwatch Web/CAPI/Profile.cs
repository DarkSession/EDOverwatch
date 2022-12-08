using Newtonsoft.Json;

namespace EDOverwatch_Web.CAPI
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
