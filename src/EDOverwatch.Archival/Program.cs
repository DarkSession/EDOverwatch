using EDDatabase;
using EDOverwatch.Archival.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EDOverwatch.Archival;

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
                var connectionString = Configuration.GetValue<string>("ConnectionString") ?? string.Empty;
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
                optionsBuilder.UseProjectables();
            })
            .BuildServiceProvider();


        await using var serviceScope = Services.CreateAsyncScope();

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        var dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

        var starSystems = await dbContext.StarSystems
            .AsNoTracking()
            .AsSplitQuery()
            .Where(s => s.ThargoidLevelHistory!.Any(t => t.State != StarSystemThargoidLevelState.None))
            .Include(s => s.ThargoidLevelHistory!)
            .ThenInclude(s => s.ProgressHistory)
            .Include(s => s.ThargoidLevelHistory!)
            .ThenInclude(s => s.CycleStart)
            .Include(s => s.ThargoidLevelHistory!)
            .ThenInclude(s => s.CycleEnd)
            .Include(s => s.ThargoidLevelHistory!)
            .ThenInclude(s => s.StateExpires)
            .Include(s => s.ThargoidLevelHistory!)
            .ThenInclude(s => s.Maelstrom)
            .ToListAsync();

        {
            Directory.CreateDirectory("Contributions By Cycle");

            var contributions = await dbContext.WarEfforts
                .AsNoTracking()
                .AsSplitQuery()
                .Where(c => c.CycleId != null && c.Side == WarEffortSide.Humans)
                .Include(c => c.StarSystem)
                .GroupBy(c => new
                {
                    c.Date,
                    c.CycleId,
                    c.StarSystemId,
                    c.Source,
                    c.Type,
                })
                .Select(c => new
                {
                    c.Key.Date,
                    c.Key.CycleId,
                    c.Key.StarSystemId,
                    c.Key.Source,
                    c.Key.Type,
                    Amount = c.Sum(c => c.Amount),
                })
                .ToListAsync();

            var cycles = await dbContext.ThargoidCycles
                .AsNoTracking()
                .ToListAsync();

            var cycle = Cycle.CycleZero;
            while (cycle <= Cycle.CycleMax)
            {
                var cycleNumber = (int)(cycle - Cycle.CycleZero).TotalDays / 7;
                var thargoidCycle = cycles.Single(c => c.Start == cycle);

                var cycleContributions = contributions
                    .Where(c => c.CycleId == thargoidCycle.Id)
                    .ToList();

                var systemIds = cycleContributions
                    .Select(c => c.StarSystemId)
                    .Distinct()
                    .ToList();

                var systems = await dbContext.StarSystems
                    .AsNoTracking()
                    .Where(s => systemIds.Contains(s.Id))
                    .ToListAsync();

                var result = new List<SystemContributions>();

                foreach (var system in systems)
                {
                    var systemContributions = cycleContributions
                        .Where(c => c.StarSystemId == system.Id)
                        .ToList();

                    result.Add(new SystemContributions() {
                        Address = system.SystemAddress,
                        Name = system.Name,
                        Contributions = systemContributions
                            .Select(c => new Contribution() {
                                Date = c.Date,
                                Source = c.Source.ToString(),
                                Type = c.Type.ToString(),
                                Amount = c.Amount })
                            .ToList()
                    });
                }

                var data = JsonSerializer.Serialize(result, jsonSerializerOptions);
                await File.WriteAllTextAsync($"Contributions By Cycle/{cycleNumber} - {cycle:yyyy-MM-dd}.json", data);

                cycle = cycle.AddDays(7);
            }
        }

        {
            Directory.CreateDirectory("By System");

            foreach (var starSystem in starSystems)
            {
                var systemExport = new SingleSystem(starSystem, null);
                var data = JsonSerializer.Serialize(systemExport, jsonSerializerOptions);
                await File.WriteAllTextAsync($"By System/{systemExport.Name}.json", data);
            }

            Directory.CreateDirectory("By Cycle");
            var cycle = Cycle.CycleZero;
            while (cycle <= Cycle.CycleMax)
            {
                var cycleNumber = (int)(cycle - Cycle.CycleZero).TotalDays / 7;
                var cycleExport = starSystems
                    .Select(s => new SingleSystem(s, cycleNumber))
                    .ToList();
                cycleExport.RemoveAll(c => c.States.Count == 0);

                var data = JsonSerializer.Serialize(cycleExport, jsonSerializerOptions);
                await File.WriteAllTextAsync($"By Cycle/{cycleNumber} - {cycle:yyyy-MM-dd}.json", data);
                cycle = cycle.AddDays(7);
            }
        }

        {
            var titans = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Hearts)
                .ToListAsync();

            Directory.CreateDirectory("Titan Hearts");

            foreach (var titan in titans)
            {
                var titanExport = new TitanHearts(titan);
                var data = JsonSerializer.Serialize(titanExport, jsonSerializerOptions);
                await File.WriteAllTextAsync($"Titan Hearts/{titanExport.Name}.json", data);
            }
        }
    }
}
