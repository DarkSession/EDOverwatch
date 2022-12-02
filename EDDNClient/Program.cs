﻿global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

namespace EDDNClient
{
    internal class Program
    {
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        static Task Main(string[] args)
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
                .AddTransient<Client>()
                .AddSingleton<Watchdog>()
                .BuildServiceProvider();

            Watchdog watchdog = Services.GetRequiredService<Watchdog>();
            return watchdog.StartAsync();
        }
    }
}