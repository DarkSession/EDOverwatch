using EDDatabase;
using EDDataProcessor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDOverwatchWeeklyReset
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
            log.LogInformation("Started weekly reset");

            CancellationToken cancellationToken = default;
            EdDbContext dbContext = Services.GetRequiredService<EdDbContext>();
            await WeeklyReset.ProcessWeeklyReset(dbContext, cancellationToken);

            log.LogInformation("Weekly reset completed");
        }
    }
}