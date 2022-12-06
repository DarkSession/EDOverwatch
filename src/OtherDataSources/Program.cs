global using EDDatabase;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OtherDataSources.IDA;
using OtherDataSources.Inara;

namespace OtherDataSources
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
                .AddSingleton<InaraClient>()
                .AddSingleton<IdaClient>()
            .BuildServiceProvider();

            ILogger log = Services.GetRequiredService<ILogger<Program>>();

            while (true)
            {
                try
                {
                    await using AsyncServiceScope serviceScope = Services.CreateAsyncScope();
                    IdaClient idaClient = serviceScope.ServiceProvider.GetRequiredService<IdaClient>();
                    await idaClient.UpdateData();
                }
                catch (Exception e)
                {
                    log.LogError(e, "IdaClient exception");
                }
                try
                {
                    await using AsyncServiceScope serviceScope = Services.CreateAsyncScope();
                    UpdateFromInara? updateFromInara = ActivatorUtilities.CreateInstance(serviceScope.ServiceProvider, typeof(UpdateFromInara)) as UpdateFromInara;
                    if (updateFromInara != null)
                    {
                        await updateFromInara.Update();
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, "Inara update exception");
                }
                await Task.Delay(TimeSpan.FromMinutes(15));
            }
        }
    }
}