using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [AllowAnonymous]
    [Route("discovery")]
    public class CommanderDiscovery : ControllerBase
    {
        [HttpGet]
        public ApiDiscovery Get()
        {
            ApiDiscovery apiDiscovery = new("DCoH Overwatch", "DCoH Overwatch tracks the progress of humanity's war against the Thargoids in Elite Dangerous.", "https://dcoh.watch");
            apiDiscovery.AddEvent("MissionAccepted")
                .AddFilter("Name", "^Mission_TW");
            apiDiscovery.AddEvent("MissionCompleted")
                .AddFilter("Name", "^Mission_TW");
            apiDiscovery.AddEvent("Died")
                .AddFilter("KillerShip", "scout_hq|scout_nq|scout_q|scout|thargonswarm|thargon");
            apiDiscovery.AddEvent("FactionKillBond")
                .AddFilter("AwardingFaction", @"^\$faction_PilotsFederation;$")
                .AddFilter("VictimFaction", @"^\$faction_Thargoid;$");
            apiDiscovery.AddEvent("CollectCargo")
                .AddFilter("Type", "UnknownArtifact2|ThargoidTissueSampleType1|ThargoidTissueSampleType2|ThargoidTissueSampleType3|ThargoidTissueSampleType4|ThargoidTissueSampleType5|ThargoidTissueSampleType6|ThargoidTissueSampleType9a|ThargoidTissueSampleType9b|ThargoidTissueSampleType9c|ThargoidTissueSampleType10a|ThargoidTissueSampleType10b|ThargoidTissueSampleType10c|ThargoidScoutTissueSample|USSCargoBlackBox|OccupiedCryoPod|DamagedEscapePod");
            apiDiscovery.AddEvent("Cargo");
            apiDiscovery.Endpoints.AddEventsEndpoint("api/v1/Commander/Events", 15, 20);

            return apiDiscovery;
        }
    }

    public class ApiDiscovery
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public Dictionary<string, ApiDiscoveryEvent> Events { get; } = new();
        public ApiDiscoveryEndpoints Endpoints { get; } = new();

        public ApiDiscovery(string name, string description, string url)
        {
            Name = name;
            Description = description;
            Url = url;
        }

        public ApiDiscoveryEvent AddEvent(string name)
        {
            ApiDiscoveryEvent apiDiscoveryEvent = new();
            Events[name] = apiDiscoveryEvent;
            return apiDiscoveryEvent;
        }
    }

    public class ApiDiscoveryEvent
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>? Filters { get; set; }

        public ApiDiscoveryEvent AddFilter(string fieldName, string regexFilter)
        {
            Filters ??= new();
            Filters[fieldName] = regexFilter;
            return this;
        }
    }

    public class ApiDiscoveryEndpoints
    {
        public ApiDiscoveryEndpoint? Events { get; set; }

        public void AddEventsEndpoint(string path, int minPeriod, int maxBatch)
        {
            Events = new(path, minPeriod, maxBatch);
        }
    }

    public class ApiDiscoveryEndpoint
    {
        public string Path { get; set; }
        public int MinPeriod { get; set; }
        public int MaxBatch { get; set; }

        public ApiDiscoveryEndpoint(string path, int minPeriod, int maxBatch)
        {
            Path = path;
            MinPeriod = minPeriod;
            MaxBatch = maxBatch;
        }
    }
}
