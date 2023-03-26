global using ActiveMQ.Artemis.Client;
global using ActiveMQ.Artemis.Client.Transactions;
global using EDDatabase;
global using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDOverwatch
{
    internal class Program
    {
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        static Task Main(string[] args)
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
                .AddSingleton<Overwatch>()
                .BuildServiceProvider();

            Overwatch overwatch = Services.GetRequiredService<Overwatch>();
            return overwatch.RunAsync();
        }
    }
}