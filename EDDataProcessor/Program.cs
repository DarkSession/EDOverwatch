global using ActiveMQ.Artemis.Client;
global using EDDatabase;
global using Messages;
global using Microsoft.EntityFrameworkCore;
global using Newtonsoft.Json;
using EDDataProcessor.EDDN;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDDataProcessor
{
    internal class Program
    {
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        static async Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: true)
#if DEBUG
                .AddJsonFile("appsettings.dev.json", optional: true)
#endif
                .AddUserSecrets<Program>()
                .Build();

            Services = new ServiceCollection()
                .AddSingleton(Configuration)
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                })
                .AddSingleton<EDDNProcessor>()
                .AddDbContext<EdDbContext>()
                .BuildServiceProvider();

            EdDbContext dbContext = Services.GetRequiredService<EdDbContext>();
            bool canConnect = await dbContext.Database.CanConnectAsync();

            await dbContext.FactionAllegiances.FirstOrDefaultAsync();

            EDDNProcessor eddnProcessor = Services.GetRequiredService<EDDNProcessor>();
            await eddnProcessor.ProcessMessages();
        }
    }
}