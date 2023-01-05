using ActiveMQ.Artemis.Client;
using Discord.Interactions;
using EDUtils;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Serialization;

namespace DCoHTrackerDiscordBot.Module
{
    public class TrackingModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IConfiguration Configuration { get; }
        private EdDbContext DbContext { get; }
        private IAnonymousProducer AnonymousProducer { get; }

        public TrackingModule(
            EdDbContext dbContext,
            IConfiguration configuration,
            IAnonymousProducer anonymousProducer)
        {
            DbContext = dbContext;
            Configuration = configuration;
            AnonymousProducer = anonymousProducer;
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

        [SlashCommand("update", "Update the focus of your sqadrons activities")]
        public async Task Update(
            [Summary("Operation", "Operation Type")] OperationType operation,
#pragma warning disable IDE0060 // Remove unused parameter
            [Summary("Maelstrom", "Maelstrom"), Autocomplete(typeof(MaelstromAutocompleteHandler))] string maelstromName,
#pragma warning restore IDE0060 // Remove unused parameter
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

            DcohDiscordUser? user = await DbContext.DcohDiscordUsers
                .Include(d => d.Faction)
                .FirstOrDefaultAsync(d => d.DiscordId == Context.User.Id);
            if (user?.Faction == null)
            {
                await RespondAsync("You are currently not registered to a squadron. Please run the `/squadron` command.", ephemeral: true);
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
            if (await DbContext.DcohFactionOperations.AnyAsync(d =>
                        d.StarSystem == starSystem &&
                        d.Status == DcohFactionOperationStatus.Active &&
                        d.Faction == user.Faction &&
                        d.Type == type))
            {
                await FollowupAsync($"This operation already exists in {starSystem.Name} for your squadron.", ephemeral: true);
                return;
            }

            DcohFactionOperation factionOperation = new(0, type, DcohFactionOperationStatus.Active, DateTimeOffset.Now)
            {
                CreatedBy = user,
                StarSystem = starSystem,
                Faction = user.Faction,
            };
            DbContext.DcohFactionOperations.Add(factionOperation);
            await DbContext.SaveChangesAsync();
            await FollowupAsync($"Registered {type.GetEnumMemberValue()} activities by **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})** in **{starSystem.Name}**.");
        }

        [SlashCommand("remove", "Remove a registered operation")]
        public async Task Remove(
            [Summary("Operation", "Operation Type")] OperationType operation,
#pragma warning disable IDE0060 // Remove unused parameter
            [Summary("Maelstrom", "Maelstrom"), Autocomplete(typeof(MaelstromAutocompleteHandler))] string maelstromName,
#pragma warning restore IDE0060 // Remove unused parameter
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

            DcohDiscordUser? user = await DbContext.DcohDiscordUsers
                .Include(d => d.Faction)
                .FirstOrDefaultAsync(d => d.DiscordId == Context.User.Id);
            if (user?.Faction == null)
            {
                await RespondAsync("You are currently not registered to a squadron. Please run the `/squadron` command.", ephemeral: true);
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
                        d.Faction == user.Faction &&
                        d.Type == type);
            if (dcohFactionOperation == null)
            {
                await FollowupAsync($"No {type.GetEnumMemberValue()} activity was found in {starSystem.Name} for your squadron.", ephemeral: true);
                return;
            }
            dcohFactionOperation.Status = DcohFactionOperationStatus.Inactive;
            await DbContext.SaveChangesAsync();

            await FollowupAsync($"Removed {type.GetEnumMemberValue()} activities by **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})** in **{starSystem.Name}**.");
        }

        [SlashCommand("view", "View operation by type")]
        public async Task View(
            [Summary("Operation", "Operation Type")] OperationType? operation = null,
            [Summary("Maelstrom", "Maelstrom"), Autocomplete(typeof(MaelstromAutocompleteHandler))] string? maelstromName = null)
        {
            await DeferAsync(true);

            IQueryable<DcohFactionOperation> dcohFactionsQuery = DbContext.DcohFactionOperations
                .AsNoTracking()
                .Include(d => d.Faction)
                .Include(d => d.StarSystem)
                .ThenInclude(s => s!.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .Where(d =>
                    d.Status == DcohFactionOperationStatus.Active &&
                    d.StarSystem != null &&
                    d.StarSystem.ThargoidLevel != null &&
                    d.StarSystem.ThargoidLevel.Maelstrom != null);

            string operationTypeString = string.Empty;
            string maelstromNameString = string.Empty;
            if (operation is OperationType operationType)
            {
                DcohFactionOperationType type = OperationTypeToDcohFactionOperationType(operationType);
                dcohFactionsQuery = dcohFactionsQuery.Where(d => d.Type == type);
                operationTypeString = operationType.GetEnumMemberValue();
            }
            if (!string.IsNullOrEmpty(maelstromName))
            {
                maelstromName = maelstromName.Replace("%", string.Empty).Trim();
                ThargoidMaelstrom? maelstrom = await DbContext.ThargoidMaelstroms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => EF.Functions.Like(m.Name, maelstromName));
                if (maelstrom == null)
                {
                    await FollowupAsync($"Could not find maelstrom {Format.Sanitize(maelstromName)}", ephemeral: true);
                    return;
                }
                dcohFactionsQuery = dcohFactionsQuery.Where(d => d.StarSystem!.ThargoidLevel!.Maelstrom == maelstrom);
                maelstromNameString = maelstrom.Name;
            }
            if (string.IsNullOrEmpty(operationTypeString) && string.IsNullOrEmpty(maelstromNameString))
            {
                await FollowupAsync($"You need to either provide the maelstrom or the operation.", ephemeral: true);
                return;
            }

            List<DcohFactionOperation> factionOperations = await dcohFactionsQuery
                .OrderBy(s => s.StarSystem!.ThargoidLevel!.Maelstrom)
                .ThenBy(s => s.StarSystem!.Name)
                .ToListAsync();
            EmbedBuilder embedMain = new EmbedBuilder()
                           .WithTitle("Operations / Activities");
            if (factionOperations.Any())
            {
                string description = $"{operationTypeString} activities".Trim();
                description += (!string.IsNullOrEmpty(maelstromNameString) ? " around " + maelstromNameString : "") + ":";
                embedMain.Description = description;
                if (string.IsNullOrEmpty(maelstromNameString))
                {
                    embedMain.AddField("Maelstrom", string.Join("\r\n", factionOperations.Select(c => c.StarSystem?.ThargoidLevel?.Maelstrom?.Name ?? "-")), true);
                }
                embedMain.AddField("System", string.Join("\r\n", factionOperations.Select(c => c.StarSystem?.Name ?? "-")), true);
                embedMain.AddField("Squadron", string.Join("\r\n", factionOperations.Select(c => c.Faction?.Name ?? "-")), true);
                if (string.IsNullOrEmpty(operationTypeString))
                {
                    embedMain.AddField("Operation", string.Join("\r\n", factionOperations.Select(c => c.Type.GetEnumMemberValue() ?? "-")), true);
                }
            }
            else
            {
                embedMain.Description = "There are no known active activities that meet the criterias.";
            }

            await FollowupAsync(embed: embedMain.Build(), ephemeral: true);
        }

        [SlashCommand("system-update", "Request an automatic or manual review of system data")]
        public async Task SystemUpdateRequest(
            [Summary("System", "System Name"), Autocomplete(typeof(AnyStarSystemAutocompleteHandler))] string starSystemName)
        {
            await DeferAsync(true);

            (_, string message) = await SystemUpdateRequest(starSystemName, Context.User.Id, Context.Channel.Id, DbContext, AnonymousProducer);

            await FollowupAsync(message, ephemeral: true);
        }

        public static async Task<(bool success, string userMessage)> SystemUpdateRequest(
            string starSystemName,
            ulong discordUserId,
            ulong discordChannelId,
            EdDbContext dbContext,
            IAnonymousProducer anonymousProducer)
        {
            if (await dbContext.StarSystemUpdateQueueItems.CountAsync(s => s.DiscordUserId == discordUserId && s.Status != StarSystemUpdateQueueItemStatus.Completed) > 5)
            {
                return (false, "There are still 5 system updates requested by you pending. You need to wait until one of those is completed before you can submit another request.");
            }

            StarSystem? starSystem = await dbContext.StarSystems
                .Where(s => EF.Functions.Like(s.Name, starSystemName.Replace("%", string.Empty)))
                .FirstOrDefaultAsync();
            if (starSystem == null)
            {
                return (false, $"Could not find system **{Format.Sanitize(starSystemName)}**.");
            }
            else if (await dbContext.StarSystemUpdateQueueItems.AnyAsync(s => s.StarSystem == starSystem && s.Status != StarSystemUpdateQueueItemStatus.Completed))
            {
                return (false, $"System **{Format.Sanitize(starSystem.Name)}** has already been requested and is still pending.");
            }

            StarSystemUpdateQueueItem starSystemUpdateQueueItem = new(default, discordUserId, discordChannelId, StarSystemUpdateQueueItemStatus.PendingAutomaticReview, StarSystemUpdateQueueItemResult.Pending, default, DateTimeOffset.Now, null)
            {
                StarSystem = starSystem,
            };
            dbContext.StarSystemUpdateQueueItems.Add(starSystemUpdateQueueItem);
            string text;
            if (await dbContext.StarSystemUpdateQueueItems.AnyAsync(s =>
                            s.StarSystem == starSystem &&
                            s.Status == StarSystemUpdateQueueItemStatus.Completed &&
                            s.Completed >= DateTimeOffset.Now.AddDays(-1) &&
                            s.Result != StarSystemUpdateQueueItemResult.Pending))
            {
                starSystemUpdateQueueItem.Status = StarSystemUpdateQueueItemStatus.PendingManualReview;
                text = $"System **{Format.Sanitize(starSystem.Name)}** queued for manual review.";
            }
            else
            {
                text = $"System **{Format.Sanitize(starSystem.Name)}** queued for automatic review.";
            }
            await dbContext.SaveChangesAsync();

            StarSystemUpdateQueueItemUpdated starSystemUpdateQueueItemUpdated = new(starSystemUpdateQueueItem.Id);
            await anonymousProducer.SendAsync(StarSystemUpdateQueueItemUpdated.QueueName, StarSystemUpdateQueueItemUpdated.Routing, starSystemUpdateQueueItemUpdated.Message);
            return (true, text);
        }

        public static DcohFactionOperationType OperationTypeToDcohFactionOperationType(OperationType operation)
        {
            return operation switch
            {
                OperationType.AXCombat => DcohFactionOperationType.AXCombat,
                OperationType.Rescue => DcohFactionOperationType.Rescue,
                OperationType.Logistics => DcohFactionOperationType.Logistics,
                OperationType.General => DcohFactionOperationType.General,
                _ => DcohFactionOperationType.Unknown,
            };
        }

        public static Dictionary<string, List<string>> SystemsByMaelstrom { get; private set; } = new();
        public static List<string> Systems { get; private set; } = new();
        public static async Task UpdateSystems(EdDbContext dbContext)
        {
            Dictionary<string, List<string>> systems = new();
            List<StarSystem> starSystems = await dbContext.StarSystems
                .AsNoTracking()
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .Where(s => s.ThargoidLevel != null &&
                    s.ThargoidLevel.Maelstrom != null &&
                    s.ThargoidLevel.State > StarSystemThargoidLevelState.None)
                .ToListAsync();
            foreach (StarSystem starSystem in starSystems)
            {
                if (starSystem.ThargoidLevel?.Maelstrom is null)
                {
                    continue;
                }
                if (systems.TryGetValue(starSystem.ThargoidLevel.Maelstrom.Name, out List<string>? systemList))
                {
                    systemList.Add(starSystem.Name);
                }
                else
                {
                    systems[starSystem.ThargoidLevel.Maelstrom.Name] = new() { starSystem.Name };
                }
            }
            SystemsByMaelstrom = systems;
            Systems = starSystems.Select(s => s.Name).ToList();
        }

        public class WarStarSystemAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                string? maelstrom = autocompleteInteraction.Data.Options.FirstOrDefault(o => o.Name == "maelstrom")?.Value as string;
                List<string>? systemList = null;
                if (!string.IsNullOrEmpty(maelstrom))
                {
                    SystemsByMaelstrom.TryGetValue(maelstrom, out systemList);
                }
                systemList ??= Systems;
                IEnumerable<string> systems = systemList.OrderBy(s => s);
                if (autocompleteInteraction.Data.Current.Value is string value)
                {
                    value = value.Trim().ToLower();
                    if (value.Length > 0)
                    {
                        systems = systems
                            .Where(s => s.ToLower().StartsWith(value));
                    }
                }
                // max - 25 suggestions at a time (API limit)
                return Task.FromResult(AutocompletionResult.FromSuccess(systems.Select(s => new AutocompleteResult(s, s)).Take(25)));
            }
        }

        public class AnyStarSystemAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                EdDbContext dbContext = services.GetRequiredService<EdDbContext>();
                List<string>? systemList = null;
                if (autocompleteInteraction.Data.Current.Value is string value)
                {
                    value = value.Trim().Replace("%", string.Empty).ToLower();
                    if (value.Length >= 2)
                    {
                        systemList = await dbContext.StarSystems
                            .AsNoTracking()
                            .Where(s => EF.Functions.Like(s.Name, $"{value}%"))
                            .Take(10)
                            .Select(s => s.Name)
                            .OrderBy(n => n)
                            .ToListAsync();
                    }
                }
                systemList ??= new();
                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(systemList.Select(s => new AutocompleteResult(s, s)).Take(25));
            }
        }

        public static List<string> Maelstroms { get; set; } = new();
        public static async Task UpdateMaelstroms(EdDbContext dbContext)
        {
            Maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Select(s => s.Name)
                .OrderBy(n => n)
                .ToListAsync();
        }

        public class MaelstromAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                IEnumerable<string> maelstroms = Maelstroms;
                if (autocompleteInteraction.Data.Current.Value is string value)
                {
                    value = value.Trim().ToLower();
                    if (value.Length > 0)
                    {
                        maelstroms = maelstroms.Where(s => s.ToLower().StartsWith(value));
                    }
                }
                // max - 25 suggestions at a time (API limit)
                return Task.FromResult(AutocompletionResult.FromSuccess(maelstroms.Select(s => new AutocompleteResult(s, s)).Take(25)));
            }
        }
    }

    public enum OperationType
    {
        [EnumMember(Value = "AX Combat")]
        [ChoiceDisplay("AX Combat")]
        AXCombat,
        Rescue,
        Logistics,
        [EnumMember(Value = "General Purpose")]
        [ChoiceDisplay("General Purpose")]
        General,
    }
}
