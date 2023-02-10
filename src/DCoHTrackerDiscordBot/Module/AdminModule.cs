using Discord.Interactions;
using EDUtils;
using static DCoHTrackerDiscordBot.Module.SquadronModule;
using static DCoHTrackerDiscordBot.Module.TrackingModule;

namespace DCoHTrackerDiscordBot.Module
{
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IConfiguration Configuration { get; }
        private EdDbContext DbContext { get; }

        public AdminModule(EdDbContext dbContext, IConfiguration configuration)
        {
            DbContext = dbContext;
            Configuration = configuration;
        }

        private async ValueTask<bool> CheckElevatedGuild()
        {
            if (!(Configuration.GetSection("Discord:ElevatedServers").Get<List<ulong>>()?.Contains(Context.Guild.Id) ?? false))
            {
                await RespondAsync("This server is not authorized to execute commands.", ephemeral: true);
                return false;
            }
            return true;
        }

        [SlashCommand("admin-update", "Update the focus of a squadron's activities")]
        public async Task Update(
            [Summary("Squadron", "Squadron Id (4 letter) of the squadron"), Autocomplete(typeof(SquadronIdAutocompleteHandler))] string squadronId,
            [Summary("Operation", "Operation Type")] OperationType operation,
            [Summary("System", "System Name"), Autocomplete(typeof(WarStarSystemAutocompleteHandler))] string starSystemName)
        {
            if (!await CheckElevatedGuild())
            {
                return;
            }
            starSystemName = starSystemName.Replace("%", string.Empty).Trim();
            if (string.IsNullOrEmpty(starSystemName) || starSystemName.Length < 3 || starSystemName.Length > 64)
            {
                await RespondAsync("The system name is invalid.", ephemeral: true);
                return;
            }

            squadronId = squadronId.Replace("%", string.Empty);
            DcohFaction? faction = await DbContext.DcohFactions.FirstOrDefaultAsync(f => EF.Functions.Like(f.Short, squadronId));
            if (faction == null)
            {
                await RespondAsync("Squadron not found.", ephemeral: true);
                return;
            }
            await DeferAsync();

            StarSystem? starSystem = await DbContext.StarSystems
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .FirstOrDefaultAsync(s =>
                    s.ThargoidLevel != null &&
                    s.ThargoidLevel.Maelstrom != null &&
                    s.ThargoidLevel.State > StarSystemThargoidLevelState.None &&
                    EF.Functions.Like(s.Name, starSystemName));
            if (starSystem == null)
            {
                await FollowupAsync("The system could not be found.", ephemeral: true);
                return;
            }

            DcohFactionOperationType type = TrackingModule.OperationTypeToDcohFactionOperationType(operation);
            if (await DbContext.DcohFactionOperations.AnyAsync(d =>
                        d.StarSystem == starSystem &&
                        d.Status == DcohFactionOperationStatus.Active &&
                        d.Faction == faction &&
                        d.Type == type))
            {
                await FollowupAsync($"This operation already exists in {starSystem.Name} this squadron.", ephemeral: true);
                return;
            }

            DcohFactionOperation factionOperation = new(0, type, DcohFactionOperationStatus.Active, DateTimeOffset.Now, null)
            {
                CreatedBy = null,
                StarSystem = starSystem,
                Faction = faction,
            };
            DbContext.DcohFactionOperations.Add(factionOperation);
            await DbContext.SaveChangesAsync();
            await FollowupAsync($"Registered {type.GetEnumMemberValue()} operations by **{Format.Sanitize(faction.Name)} ({Format.Sanitize(faction.Short)})** in **{starSystem.Name}**.");
        }

        [SlashCommand("admin-remove", "Remove a registered activity of a squadron")]
        public async Task Remove(
            [Summary("Squadron", "Squadron Id (4 letter) of the squadron"), Autocomplete(typeof(SquadronIdAutocompleteHandler))] string squadronId,
            [Summary("Activity", "Activity Type")] OperationType operation,
            [Summary("System", "System Name"), Autocomplete(typeof(WarStarSystemAutocompleteHandler))] string starSystemName)
        {
            if (!await CheckElevatedGuild())
            {
                return;
            }
            starSystemName = starSystemName.Replace("%", string.Empty).Trim();
            if (string.IsNullOrEmpty(starSystemName) || starSystemName.Length < 3 || starSystemName.Length > 64)
            {
                await RespondAsync("The system name is invalid.", ephemeral: true);
                return;
            }

            squadronId = squadronId.Replace("%", string.Empty);
            DcohFaction? faction = await DbContext.DcohFactions.FirstOrDefaultAsync(f => EF.Functions.Like(f.Short, squadronId));
            if (faction == null)
            {
                await RespondAsync("Squadron not found.", ephemeral: true);
                return;
            }

            await DeferAsync();

            StarSystem? starSystem = await DbContext.StarSystems
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .FirstOrDefaultAsync(s =>
                    s.ThargoidLevel != null &&
                    s.ThargoidLevel.Maelstrom != null &&
                    s.ThargoidLevel.State > StarSystemThargoidLevelState.None &&
                    EF.Functions.Like(s.Name, starSystemName));
            if (starSystem == null)
            {
                await FollowupAsync("The system could not be found.", ephemeral: true);
                return;
            }

            DcohFactionOperationType type = OperationTypeToDcohFactionOperationType(operation);
            DcohFactionOperation? dcohFactionOperation = await DbContext.DcohFactionOperations.FirstOrDefaultAsync(d =>
                        d.StarSystem == starSystem &&
                        d.Status == DcohFactionOperationStatus.Active &&
                        d.Faction == faction &&
                        d.Type == type);
            if (dcohFactionOperation == null)
            {
                await FollowupAsync($"No {type.GetEnumMemberValue()} activity was found in {starSystem.Name} for this squadron.", ephemeral: true);
                return;
            }

            dcohFactionOperation.Status = DcohFactionOperationStatus.Inactive;
            await DbContext.SaveChangesAsync();

            await FollowupAsync($"Removed {type.GetEnumMemberValue()} activity by **{Format.Sanitize(faction.Name)} ({Format.Sanitize(faction.Short)})** in **{starSystem.Name}**.");
        }
    }
}
