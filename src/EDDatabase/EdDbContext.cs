global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations.Schema;
using EDUtils;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http.Headers;

namespace EDDatabase
{
    public class EdDbContext : DbContext
    {
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Commander> Commanders { get; set; }
        public DbSet<CommanderDeferredJournalEvent> CommanderDeferredJournalEvents { get; set; }
        public DbSet<CommanderMission> CommanderMissions { get; set; }

        public DbSet<DcohDiscordUser> DcohDiscordUsers { get; set; }
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
        public DbSet<StarSystemThargoidLevelProgress> StarSystemThargoidLevelProgress { get; set; }
        public DbSet<StarSystemSecurity> StarSystemSecurities { get; set; }
        public DbSet<StarSystemUpdateQueueItem> StarSystemUpdateQueueItems { get; set; }

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

            modelBuilder.Entity<StarSystemThargoidLevel>()
                .HasMany(s => s.ProgressHistory)
                .WithOne(s => s.ThargoidLevel);
        }

        public async Task<ThargoidCycle> GetThargoidCycle(DateTimeOffset dateTimeOffset, CancellationToken cancellationToken, int weekOffset = 0)
        {
            DateTimeOffset cycleTime = WeeklyTick.GetTickTime(dateTimeOffset, weekOffset);
            ThargoidCycle? thargoidCycle = await ThargoidCycles.FirstOrDefaultAsync(t => t.Start == cycleTime, cancellationToken);
            if (thargoidCycle == null)
            {
                thargoidCycle = new(0, cycleTime, cycleTime.AddDays(7));
                ThargoidCycles.Add(thargoidCycle);
                await SaveChangesAsync(cancellationToken);
            }
            return thargoidCycle;
        }

        public Task<bool> ThargoidCycleExists(DateOnly date, CancellationToken cancellationToken)
        {
            DateTimeOffset cycleTime = WeeklyTick.GetTickTime(date);
            return ThargoidCycles.AnyAsync(t => t.Start == cycleTime, cancellationToken);
        }
    }
}
