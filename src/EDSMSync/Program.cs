using EDDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web;

namespace EDSMSync
{
    internal class Program
    {
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
            #if DEBUG
                .AddJsonFile("appsettings.dev.json", optional: true)
            #endif
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                .Build();

            Services = new ServiceCollection()
                .AddSingleton(Configuration)
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddConfiguration(Configuration.GetSection("Logging"));
                })
                .AddDbContext<EdDbContext>()
                .BuildServiceProvider();

            ILogger log = Services.GetRequiredService<ILogger<Program>>();

            List<string> maelstromSystems = new()
            {
                "Col 285 Sector BA-P c6-18",
                "HIP 30377",
                "HIP 20567",
                "HIP 8887",
                "Cephei Sector BV-Y b4",
                "Pegasi Sector IH-U b3-3",
                "Hyades Sector FB-N b7-6",
                "Col 285 Sector IG-O c6-5",
            };
            using HttpClient client = new();
            foreach (string systemName in maelstromSystems)
            {
                using HttpResponseMessage response = await client.GetAsync($"https://www.edsm.net/api-v1/sphere-systems?systemName={HttpUtility.UrlEncode(systemName)}&radius=40&showId=1&showCoordinates=1&showInformation=1");
                response.EnsureSuccessStatusCode();
                string systemJson = await response.Content.ReadAsStringAsync();

                List<SystemEntry>? systems = JsonConvert.DeserializeObject<List<SystemEntry>>(systemJson);
                if (systems != null)
                {
                    await using AsyncServiceScope serviceScope = Services.CreateAsyncScope();
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

                    foreach (SystemEntry system in systems)
                    {
                        if (system.SystemAddress != null &&
                            !await dbContext.StarSystems.AnyAsync(s => s.SystemAddress == system.SystemAddress))
                        {
                            StarSystem starSystem = new(0, (long)system.SystemAddress, system.Name, system.Coords.X, system.Coords.Y, system.Coords.Z, system.Information?.Population ?? 0, system.Information?.Population ?? 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                            starSystem.UpdateWarRelevantSystem();
                            dbContext.StarSystems.Add(starSystem);

                            log.LogInformation("Added {systemName} {systemAddress}", starSystem.Name, starSystem.SystemAddress);
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }

    internal class SystemEntry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id64")]
        public long? SystemAddress { get; set; }

        [JsonProperty("coords")]
        public SystemEntryCoordinates Coords { get; set; }

        [JsonProperty("information")]
        public SystemEntryInformation? Information { get; set; }
    }

    internal class SystemEntryCoordinates
    {
        [JsonProperty("x")]
        public decimal X { get; set; }

        [JsonProperty("y")]
        public decimal Y { get; set; }

        [JsonProperty("z")]
        public decimal Z { get; set; }
    }

    internal class SystemEntryInformation
    {
        [JsonProperty("population")]
        public long Population { get; set; }
    }
}