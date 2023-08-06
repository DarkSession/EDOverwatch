using EDDatabase;
using EDOverwatchAlertPrediction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDOverwatchAlertPredictionApp
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
                .AddDbContext<EdDbContext>(optionsBuilder =>
                {
                    string connectionString = Configuration.GetValue<string>("ConnectionString") ?? string.Empty;
                    optionsBuilder.UseMySql(connectionString,
                        new MariaDbServerVersion(new Version(10, 3, 25)),
                        options =>
                        {
                            options.EnableRetryOnFailure();
                            options.CommandTimeout(60 * 10 * 1000);
                        })
#if DEBUG
                        .EnableSensitiveDataLogging()
                        .LogTo(Console.WriteLine)
#endif
                        ;
                })
                .BuildServiceProvider();


            EdDbContext dbContext = Services.GetRequiredService<EdDbContext>();

            List<string> previousCycleAttackers = new()
            {
                "Hyades Sector GB-N b7-2",
                "Trianguli Sector BA-A d85",
                "Hyades Sector BV-O b6-4",
                "Trianguli Sector EQ-Y b0",
                "Trianguli Sector EQ-Y b4",
                "71 Tauri",
                "HIP 20679",
                "HIP 20480",
                "Arietis Sector JR-V b2-4",
                "86 Rho Tauri",
                "Ardhri",
                "Sambaho",
                "HIP 13179",
                "Cephei Sector AV-Y b3",
                "Mapon",
                "Col 285 Sector YT-F b12-2",
                "Col 285 Sector UN-H b11-4",
                "Col 285 Sector AF-P c6-1",
                "Mahlina",
                "Chanyaya",
                "Col 285 Sector IG-O c6-14",
                "Pegasi Sector IH-U b3-0",
                "Pegasi Sector QE-N a8-1",
                "Pegasi Sector NN-S b4-10",
                "Sholintet",
                "Pegasi Sector MN-S b4-7",
                "Col 285 Sector US-Z b14-2",
                "Col 285 Sector RN-T d3-78",
                "Col 285 Sector RH-B b14-2",
                "Col 285 Sector US-Z b14-7",
                "Col 285 Sector SH-B b14-4",
            };

            CancellationToken cancellationToken = CancellationToken.None;

            ThargoidCycle nextThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, 1);

            await EDOverwatchAlertPrediction.AlertPrediction.PredictionForCycle(dbContext, nextThargoidCycle, cancellationToken);
        }
    }
}