global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace EDDatabase
{
    public class EdDbContext : DbContext
    {
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Commander> Commanders { get; set; }
        public DbSet<CommanderDeferredJournalEvent> CommanderDeferredJournalEvents { get; set; }
        public DbSet<CommanderMission> CommanderMissions { get; set; }

        public DbSet<DcohFaction> DcohFactions { get; set; }
        public DbSet<DcohFactionOperation> DcohFactionOperations { get; set; }

        public DbSet<Economy> Economies { get; set; }

        public DbSet<FactionAllegiance> FactionAllegiances { get; set; }
        public DbSet<FactionGovernment> FactionGovernments { get; set; }

        public DbSet<IdentityUserClaim<string>> IdentityUserClaims { get; set; }

        public DbSet<OAuthCode> OAuthCodes { get; set; }

        public DbSet<StarSystem> StarSystems { get; set; }
        public DbSet<StarSystemFssSignal> StarSystemFssSignals { get; set; }
        public DbSet<StarSystemThargoidLevel> StarSystemThargoidLevels { get; set; }
        public DbSet<StarSystemSecurity> StarSystemSecurities { get; set; }

        public DbSet<Station> Stations { get; set; }
        public DbSet<StationType> StationTypes { get; set; }

        public DbSet<ThargoidCycle> ThargoidCycles { get; set; }
        public DbSet<ThargoidMaelstrom> ThargoidMaelstroms { get; set; }

        public DbSet<WarEffort> WarEfforts { get; set; }

        private string ConnectionString { get; }

        public EdDbContext(IConfiguration configuration)
        {
            ConnectionString = configuration.GetValue<string>("ConnectionString") ?? string.Empty;
        }

        internal EdDbContext(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(ConnectionString,
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
            modelBuilder.Entity<StarSystem>()
                .HasMany(s => s.ThargoidLevelHistory)
                .WithOne(s => s.StarSystem);
        }

        public async Task<ThargoidCycle> GetThargoidCycle(DateTimeOffset dateTimeOffset, CancellationToken cancellationToken, int weekOffset = 0)
        {
            DateTimeOffset cycleTime;
            {
                int dayOffset = dateTimeOffset.DayOfWeek switch
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
                DateTimeOffset lastThursday = dateTimeOffset.AddDays(dayOffset);
                DateTimeOffset lastThursdayCycle = new(lastThursday.Year, lastThursday.Month, lastThursday.Day, 7, 0, 0, TimeSpan.Zero);
                cycleTime = lastThursdayCycle.AddDays(weekOffset * 7);
            }
            ThargoidCycle? thargoidCycle = await ThargoidCycles.FirstOrDefaultAsync(t => t.Start == cycleTime, cancellationToken);
            if (thargoidCycle == null)
            {
                thargoidCycle = new(0, cycleTime, cycleTime.AddDays(7));
                ThargoidCycles.Add(thargoidCycle);
                await SaveChangesAsync(cancellationToken);
            }
            return thargoidCycle;
        }
    }
}
