using EDDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EDDBSync
{
    internal class Program
    {
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        static async Task Main(string[] args)
        {
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

            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(Models.System.Url);
            response.EnsureSuccessStatusCode();
            string systemJson = await response.Content.ReadAsStringAsync();

            List<Models.System>? systems = JsonConvert.DeserializeObject<List<Models.System>>(systemJson);
            if (systems != null)
            {
                log.LogInformation("Downloaded {systemCount} systems", systems.Count);
                foreach (Models.System eddbStarSystem in systems)
                {
                    await using AsyncServiceScope serviceScope = Services.CreateAsyncScope();
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                    if (!await dbContext.StarSystems.AnyAsync(s => s.SystemAddress == eddbStarSystem.SystemAddress))
                    {
                        StarSystem starSystem = new(0, eddbStarSystem.SystemAddress, eddbStarSystem.Name, eddbStarSystem.X, eddbStarSystem.Y, eddbStarSystem.Z, eddbStarSystem.Population ?? 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                        starSystem.WarRelevantSystem = starSystem.IsWarRelevantSystem;
                        if (!string.IsNullOrEmpty(eddbStarSystem.Allegiance))
                        {
                            starSystem.Allegiance = await FactionAllegiance.GetByName(eddbStarSystem.Allegiance, dbContext);
                        }
                        dbContext.StarSystems.Add(starSystem);
                        await dbContext.SaveChangesAsync();
                    }
                }
                log.LogInformation("Processed {systemCount} systems", systems.Count);
            }
        }
    }
}