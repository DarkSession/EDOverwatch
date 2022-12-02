global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Configuration;

namespace EDDatabase
{
    public class EdDbContext : DbContext
    {
        public DbSet<FactionAllegiance> FactionAllegiances { get; set; }
        public DbSet<StarSystem> StarSystems { get; set; }
        public DbSet<StarSystemFssSignal> StarSystemFssSignals { get; set; }
        public DbSet<StarSystemThargoidLevel> StarSystemThargoidLevels { get; set; }
        public DbSet<ThargoidCycle> ThargoidCycles { get; set; }
        public DbSet<ThargoidMaelstrom> ThargoidMaelstroms { get; set; }

        private IConfiguration Configuration { get; }

        public EdDbContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
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
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StarSystemThargoidLevel>()
                .HasOne(s => s.StarSystem)
                .WithMany(s => s.ThargoidLevelHistory);

            modelBuilder.Entity<StarSystemThargoidLevel>()
                .HasOne(s => s.StarSystem)
                .WithOne(s => s.ThargoidLevel);

            modelBuilder.Entity<StarSystem>()
                .HasMany(s => s.ThargoidLevelHistory)
                .WithOne(s => s.StarSystem);

            modelBuilder.Entity<StarSystem>()
                .HasOne(s => s.ThargoidLevel)
                .WithOne(s => s.StarSystem);
        }

        public async Task<ThargoidCycle> GetThargoidCycle(int weekOffset = 0)
        {
            DateTimeOffset cycleTime;
            {
                int dayOffset = DateTimeOffset.UtcNow.DayOfWeek switch
                {
                    DayOfWeek.Sunday => -3,
                    DayOfWeek.Monday => -4,
                    DayOfWeek.Tuesday => -5,
                    DayOfWeek.Wednesday => -6,
                    DayOfWeek.Thursday => 0,
                    DayOfWeek.Friday => -1,
                    DayOfWeek.Saturday => -2,
                    _ => 0,
                };
                DateTimeOffset lastThursday = DateTimeOffset.UtcNow.AddDays(dayOffset);
                DateTimeOffset lastThursdayCycle = new(lastThursday.Year, lastThursday.Month, lastThursday.Day, 7, 0, 0, TimeSpan.Zero);
                cycleTime = lastThursdayCycle.AddDays(weekOffset * 7);
            }
            ThargoidCycle? thargoidCycle = await ThargoidCycles.FirstOrDefaultAsync(t => t.Start == cycleTime);
            if (thargoidCycle == null)
            {
                thargoidCycle = new(0, cycleTime, cycleTime.AddDays(7));
                ThargoidCycles.Add(thargoidCycle);
                await SaveChangesAsync();
            }
            return thargoidCycle;
        }
    }
}
