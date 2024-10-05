global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations.Schema;
using EDUtils;
using Microsoft.AspNetCore.Identity;

namespace EDDatabase
{
    public class EdDbContext : DbContext
    {
        public DbSet<AlertPrediction> AlertPredictions { get; set; }
        public DbSet<AlertPredictionAttacker> AlertPredictionAttackers { get; set; }
        public DbSet<AlertPredictionCycleAttacker> AlertPredictionCycleAttackers { get; set; }

        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Commander> Commanders { get; set; }
        public DbSet<CommanderApiKey> CommanderApiKeys { get; set; }
        public DbSet<CommanderApiKeyClaim> CommanderApiKeyClaims { get; set; }
        public DbSet<CommanderCargoItem> CommanderCargoItems { get; set; }
        public DbSet<CommanderDeferredJournalEvent> CommanderDeferredJournalEvents { get; set; }
        public DbSet<CommanderFleetCarrierCargoItem> CommanderFleetCarrierCargoItems { get; set; }
        public DbSet<CommanderJournalProcessedEvent> CommanderJournalProcessedEvents { get; set; }
        public DbSet<CommanderMission> CommanderMissions { get; set; }
        public DbSet<Commodity> Commodities { get; set; }

        public DbSet<DcohDiscordUser> DcohDiscordUsers { get; set; }
        public DbSet<DcohFaction> DcohFactions { get; set; }
        public DbSet<DcohFactionOperation> DcohFactionOperations { get; set; }

        public DbSet<Economy> Economies { get; set; }

        public DbSet<FactionAllegiance> FactionAllegiances { get; set; }
        public DbSet<FactionGovernment> FactionGovernments { get; set; }

        public DbSet<IdentityUserClaim<string>> IdentityUserClaims { get; set; }

        public DbSet<MinorFaction> MinorFactions { get; set; }

        public DbSet<OAuthCode> OAuthCodes { get; set; }

        public DbSet<StarSystem> StarSystems { get; set; }
        public DbSet<StarSystemBody> StarSystemBodies { get; set; }
        public DbSet<StarSystemFssSignal> StarSystemFssSignals { get; set; }
        public DbSet<StarSystemMinorFactionPresence> StarSystemMinorFactionPresences { get; set; }
        public DbSet<StarSystemThargoidLevel> StarSystemThargoidLevels { get; set; }
        public DbSet<StarSystemThargoidLevelProgress> StarSystemThargoidLevelProgress { get; set; }
        public DbSet<StarSystemSecurity> StarSystemSecurities { get; set; }
        public DbSet<StarSystemUpdateQueueItem> StarSystemUpdateQueueItems { get; set; }

        public DbSet<Station> Stations { get; set; }
        public DbSet<StationType> StationTypes { get; set; }

        public DbSet<ThargoidCycle> ThargoidCycles { get; set; }
        public DbSet<ThargoidMaelstrom> ThargoidMaelstroms { get; set; }
        public DbSet<ThargoidMaelstromHeart> ThargoidMaelstromHearts { get; set; }
        public DbSet<ThargoidMaelstromHistoricalSummary> ThargoidMaelstromHistoricalSummaries { get; set; }

        public DbSet<WarEffort> WarEfforts { get; set; }

        public DbSet<PlayerActivity> PlayerActivities { get; set; }

        public EdDbContext(DbContextOptions<EdDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StarSystem>()
                .HasMany(s => s.ThargoidLevelHistory)
                .WithOne(s => s.StarSystem);

            modelBuilder.Entity<StarSystemThargoidLevel>()
                .HasMany(s => s.ProgressHistory)
                .WithOne(s => s.ThargoidLevel);

            modelBuilder.Entity<AlertPrediction>()
                .Navigation(a => a.StarSystem)
                .AutoInclude();

            modelBuilder.Entity<AlertPrediction>()
                .Navigation(a => a.Maelstrom)
                .AutoInclude();

            modelBuilder.Entity<AlertPrediction>()
                .Navigation(a => a.Cycle)
                .AutoInclude();

            modelBuilder.Entity<AlertPredictionAttacker>()
                .Navigation(a => a.StarSystem)
                .AutoInclude();

            modelBuilder.Entity<AlertPredictionCycleAttacker>()
                .Navigation(a => a.AttackerStarSystem)
                .AutoInclude();

            modelBuilder.Entity<StarSystemThargoidLevel>()
                .Navigation(s => s.CycleStart)
                .AutoInclude();

            modelBuilder.Entity<StarSystemThargoidLevel>()
                .Navigation(s => s.CycleEnd)
                .AutoInclude();

            modelBuilder.Entity<StarSystemThargoidLevel>()
                .Navigation(s => s.StateExpires)
                .AutoInclude();

            modelBuilder.Entity<StarSystemThargoidLevel>()
                .Navigation(s => s.CurrentProgress)
                .AutoInclude();

            modelBuilder.Entity<StarSystemThargoidLevel>()
                .Navigation(s => s.Maelstrom)
                .AutoInclude();

            modelBuilder.Entity<ThargoidMaelstrom>()
                .Property(t => t.State)
                .HasDefaultValue(ThargoidMaelstromState.Active);

            modelBuilder.Entity<ThargoidMaelstrom>()
               .Navigation(t => t.StarSystem)
               .AutoInclude();

            modelBuilder.Entity<ThargoidMaelstrom>()
               .Navigation(t => t.DefeatCycle)
               .AutoInclude();

            modelBuilder.Entity<PlayerActivity>()
                .HasOne(p => p.StarSystem)
                .WithMany(s => s.PlayerActivities)
                .HasForeignKey(p => p.StarSystemId);
        }

        public Task<ThargoidCycle> GetThargoidCycle(CancellationToken cancellationToken) => GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken);

        public Task<ThargoidCycle> GetThargoidCycle(DateOnly date, CancellationToken cancellationToken, int weekOffset = 0)
        {
            DateTimeOffset cycleTime = WeeklyTick.GetTickTime(date);
            return GetThargoidCycle(cycleTime, cancellationToken, weekOffset);
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
