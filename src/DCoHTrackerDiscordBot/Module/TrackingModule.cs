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
            [Summary("Titan", "Titan"), Autocomplete(typeof(TitanAutocompleteHandler))] string titanName,
#pragma warning restore IDE0060 // Remove unused parameter
            [Summary("System", "System Name"), Autocomplete(typeof(WarStarSystemAutocompleteHandler))] string starSystemName,
            [Summary("MeetingPoint", "Meeting Point (optional)"), Autocomplete(typeof(MeetingPointAutocompleteHandler))] string? meetingPoint = null)
        {
            if (!await CheckElevatedGuild())
            {
                return;
            }
            starSystemName = starSystemName.Replace("%", string.Empty).Trim();
            if (string.IsNullOrEmpty(starSystemName) || starSystemName.Length < 2 || starSystemName.Length > 64)
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

            if (!string.IsNullOrWhiteSpace(meetingPoint) &&
                !await DbContext.Stations.AnyAsync(s => s.StarSystem == starSystem && s.Name == meetingPoint && s.Type!.Name != StationType.FleetCarrierStationType) &&
                !await DbContext.StarSystemFssSignals.AnyAsync(s => s.StarSystem == starSystem && s.Name == meetingPoint))
            {
                meetingPoint = null;
            }

            DcohFactionOperationType type = OperationTypeToDcohFactionOperationType(operation);
            DcohFactionOperation? existingOperation = await DbContext.DcohFactionOperations.FirstOrDefaultAsync(d =>
                        d.StarSystem == starSystem &&
                        d.Status == DcohFactionOperationStatus.Active &&
                        d.Faction == user.Faction &&
                        d.Type == type);
            if (existingOperation != null)
            {
                if (existingOperation.MeetingPoint != meetingPoint)
                {
                    existingOperation.MeetingPoint = meetingPoint;
                    await DbContext.SaveChangesAsync();
                    await FollowupAsync($"Updated meeting point for your squadrons **{type.GetEnumMemberValue()}** activity in **{starSystem.Name}**.", ephemeral: true);
                    return;
                }
                await FollowupAsync($"**{type.GetEnumMemberValue()}** activity already exists in **{starSystem.Name}** for your squadron.", ephemeral: true);
                return;
            }

            if (await DbContext.DcohFactionOperations.AnyAsync(d =>
                d.StarSystem == starSystem &&
                d.Status == DcohFactionOperationStatus.Active &&
                d.Type == DcohFactionOperationType.General &&
                d.Faction == user.Faction))
            {
                await FollowupAsync($"Your squadron already registered general operations in **{starSystem.Name}**.", ephemeral: true);
                return;
            }
            if (operation == OperationType.General &&
                await DbContext.DcohFactionOperations.AnyAsync(d =>
                d.StarSystem == starSystem &&
                d.Status == DcohFactionOperationStatus.Active &&
                d.Faction == user.Faction))
            {
                await FollowupAsync($"Your squadron already registered specialized operations in **{starSystem.Name}**. No general operations can be registered.", ephemeral: true);
                return;
            }
            if (await DbContext.DcohFactionOperations.CountAsync(d =>
                  d.StarSystem == starSystem &&
                  d.Status == DcohFactionOperationStatus.Active &&
                  d.Faction == user.Faction) >= 2)
            {
                await FollowupAsync($"Your squadron can only register up to 2 specialized operations in **{starSystem.Name}**.", ephemeral: true);
                return;
            }

            DcohFactionOperation factionOperation = new(0, type, DcohFactionOperationStatus.Active, DateTimeOffset.UtcNow, meetingPoint)
            {
                CreatedBy = user,
                StarSystem = starSystem,
                Faction = user.Faction,
            };
            DbContext.DcohFactionOperations.Add(factionOperation);
            await DbContext.SaveChangesAsync();
            await FollowupAsync($"Registered **{type.GetEnumMemberValue()}** activities by **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})** in **{starSystem.Name}**.");
        }

        [SlashCommand("remove", "Remove a registered activity")]
        public async Task Remove(
            [Summary("Activity", "Activity Type")] OperationType operation,
#pragma warning disable IDE0060 // Remove unused parameter
            [Summary("Titan", "Titan"), Autocomplete(typeof(TitanAutocompleteHandler))] string titanName,
#pragma warning restore IDE0060 // Remove unused parameter
            [Summary("System", "System Name"), Autocomplete(typeof(WarStarSystemAutocompleteHandler))] string starSystemName)
        {
            if (!await CheckElevatedGuild())
            {
                return;
            }
            starSystemName = starSystemName.Replace("%", string.Empty).Trim();
            if (string.IsNullOrEmpty(starSystemName) || starSystemName.Length < 2 || starSystemName.Length > 64)
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

            await FollowupAsync($"Removed {type.GetEnumMemberValue()} activity by **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})** in **{starSystem.Name}**.");
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

            StarSystemUpdateQueueItem starSystemUpdateQueueItem = new(default, discordUserId, discordChannelId, StarSystemUpdateQueueItemStatus.PendingAutomaticReview, StarSystemUpdateQueueItemResult.Pending, default, DateTimeOffset.UtcNow, null)
            {
                StarSystem = starSystem,
            };
            dbContext.StarSystemUpdateQueueItems.Add(starSystemUpdateQueueItem);
            string text;
            if (await dbContext.StarSystemUpdateQueueItems.AnyAsync(s =>
                            s.StarSystem == starSystem &&
                            s.Status == StarSystemUpdateQueueItemStatus.Completed &&
                            s.Completed >= DateTimeOffset.UtcNow.AddDays(-1) &&
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

        public static Dictionary<string, List<string>> SystemsByTitan { get; private set; } = new();
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
            SystemsByTitan = systems;
            Systems = starSystems.Select(s => s.Name).ToList();
        }

        public class WarStarSystemAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                string? titan = autocompleteInteraction.Data.Options.FirstOrDefault(o => o.Name == "maelstrom" || o.Name == "titan")?.Value as string;
                List<string>? systemList = null;
                if (!string.IsNullOrEmpty(titan))
                {
                    SystemsByTitan.TryGetValue(titan, out systemList);
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

        public class MeetingPointAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                List<string> result = new();
                string? systemName = autocompleteInteraction.Data.Options.FirstOrDefault(o => o.Name == "system")?.Value as string;
                if (!string.IsNullOrWhiteSpace(systemName) && systemName.Length > 2 && autocompleteInteraction.Data.Current.Value is string value && value.Length > 0)
                {
                    value = value.Replace("%", string.Empty).Trim();
                    if (!string.IsNullOrEmpty(value) && !value.Contains('$'))
                    {
                        await using AsyncServiceScope scope = services.CreateAsyncScope();
                        EdDbContext dbContext = scope.ServiceProvider.GetRequiredService<EdDbContext>();
                        StarSystem? starSystem = await dbContext.StarSystems
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Name == systemName);
                        if (starSystem != null)
                        {
                            result.AddRange(await dbContext.Stations
                                .AsNoTracking()
                                .Where(s => s.StarSystem == starSystem && EF.Functions.Like(s.Name, $"{value}%") && s.Type!.Name != StationType.FleetCarrierStationType)
                                .Take(10)
                                .Select(s => s.Name)
                                .ToListAsync());

                            result.AddRange(await dbContext.StarSystemFssSignals
                                .AsNoTracking()
                                .Where(s => s.StarSystem == starSystem && EF.Functions.Like(s.Name, $"{value}%") && s.Type != StarSystemFssSignalType.FleetCarrier)
                                .Take(10)
                                .Select(s => s.Name)
                                .ToListAsync());
                        }
                    }
                }

                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(result.Distinct().Select(s => new AutocompleteResult(s, s)).Take(25));
            }
        }

        public static List<string> Titans { get; set; } = new();
        public static async Task UpdateTitans(EdDbContext dbContext)
        {
            Titans = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Select(s => s.Name)
                .OrderBy(n => n)
                .ToListAsync();
        }

        public class TitanAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                IEnumerable<string> titans = Titans;
                if (autocompleteInteraction.Data.Current.Value is string value)
                {
                    value = value.Trim().ToLower();
                    if (value.Length > 0)
                    {
                        titans = titans.Where(s => s.ToLower().StartsWith(value));
                    }
                }
                // max - 25 suggestions at a time (API limit)
                return Task.FromResult(AutocompletionResult.FromSuccess(titans.Select(s => new AutocompleteResult(s, s)).Take(25)));
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
