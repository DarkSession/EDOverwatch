using DCoHTrackerDiscordBot.Modals;
using Discord.Interactions;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DCoHTrackerDiscordBot.Module
{
    public partial class SquadronModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IConfiguration Configuration { get; }
        private EdDbContext DbContext { get; }
        private static Regex SquadronNameRegex { get; } = SquadronNameRegexGenerator();
        private static Regex SquadronIdRegex { get; } = SquadronIdRegexGenerator();

        public SquadronModule(EdDbContext dbContext, IConfiguration configuration)
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

        [SlashCommand("squadron", "As a representative create a new or join an existing squadron on this Discord.")]
        public async Task Squadron([Summary("Squadron", "Squadron Id (4 letter) of your squadron")] string squadronId)
        {
            if (!await CheckElevatedGuild())
            {
                return;
            }
            if (string.IsNullOrEmpty(squadronId) || !SquadronNameRegex.IsMatch(squadronId))
            {
                await RespondAsync("The squadron Id provided contains characters which are not allowed.", ephemeral: true);
                return;
            }
            squadronId = squadronId.Trim();
            DcohDiscordUser? user = await DbContext.DcohDiscordUsers
                .Include(d => d.Faction)
                .FirstOrDefaultAsync(d => d.DiscordId == Context.User.Id);
            if (user?.Faction != null)
            {
                await RespondAsync($"You are already a registered representative of **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})**.", ephemeral: true);
                return;
            }
            DcohFaction? faction = null;
            if (squadronId.Length == 4)
            {
                faction = await DbContext.DcohFactions.FirstOrDefaultAsync(d => d.Short == squadronId);
            }
            else
            {
                squadronId = squadronId.Replace("%", string.Empty);
                if (squadronId.Length > 2)
                {
                    faction = await DbContext.DcohFactions.FirstOrDefaultAsync(d => EF.Functions.Like(d.Name, $"%{squadronId}%"));
                }
            }
            if (faction == null)
            {
                ComponentBuilder builder = new ComponentBuilder()
                    .WithButton("Yes, register squadron", "SquadronRegForm", ButtonStyle.Primary);

                await RespondAsync("It seems we have not yet registered your squadron. Would you like to register it?",
                    ephemeral: true,
                    components: builder.Build());
                return;
            }

            if (user == null)
            {
                user = new(0, Context.User.Id, DateTimeOffset.Now)
                {
                    Faction = faction,
                };
                DbContext.DcohDiscordUsers.Add(user);
            }
            else
            {
                user.Faction = faction;
            }
            await DbContext.SaveChangesAsync();

            AllowedMentions mentions = new()
            {
                AllowedTypes = AllowedMentionTypes.Users,
            };
            await RespondAsync($"{Context.User.Mention} registed themselves as a representative of **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})**. Updates provided by them will be assigned to this squadron.", allowedMentions: mentions);
        }

        [ComponentInteraction("SquadronRegForm")]
        public async Task SquadronRegForm()
        {
            if (!await CheckElevatedGuild())
            {
                return;
            }
            DcohDiscordUser? user = await DbContext.DcohDiscordUsers
                .Include(d => d.Faction)
                .FirstOrDefaultAsync(d => d.DiscordId == Context.User.Id);
            if (user?.Faction != null)
            {
                await RespondAsync($"You are already a registered representative of **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})**.", ephemeral: true);
                return;
            }
            await Context.Interaction.RespondWithModalAsync<SquadronRegistration>("SquadronRegistration");
        }

        [ModalInteraction("SquadronRegistration")]
        public async Task SquadronRegistration(SquadronRegistration squadronRegistration)
        {
            if (!await CheckElevatedGuild())
            {
                return;
            }
            DcohDiscordUser? user = await DbContext.DcohDiscordUsers
                            .Include(d => d.Faction)
                            .FirstOrDefaultAsync(d => d.DiscordId == Context.User.Id);
            if (user?.Faction != null)
            {
                await RespondAsync($"You are already a registered representative of **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})**.", ephemeral: true);
                return;
            }

            string? squadronId = squadronRegistration.SquadronId?.Trim().ToUpper();
            string? squadronName = squadronRegistration.SquadronName?.Trim();

            if (string.IsNullOrEmpty(squadronId) || !SquadronIdRegex.IsMatch(squadronId))
            {
                await RespondAsync("Squadron Id needs to be 4 letters long.", ephemeral: true);
                return;
            }
            else if (string.IsNullOrEmpty(squadronName) || !SquadronNameRegex.IsMatch(squadronName))
            {
                await RespondAsync("Squadron name needs to be between 4 and 64 characters long, only contain letters, numbers, spaces and hyphen.", ephemeral: true);
                return;
            }
            else if (await DbContext.DcohFactions.AnyAsync(d => d.Short == squadronId))
            {
                await RespondAsync("There is already a squadron with this Squadron Id registered.", ephemeral: true);
                return;
            }
            else if (await DbContext.DcohFactions.AnyAsync(d => EF.Functions.Like(d.Name, $"%{squadronName}%")))
            {
                await RespondAsync("There is already a squadron with this name registered.", ephemeral: true);
                return;
            }

            squadronName = new CultureInfo("en").TextInfo.ToTitleCase(squadronName);

            DcohFaction faction = new(0, squadronName, squadronId, DateTimeOffset.Now);
            DbContext.Add(faction);
            await DbContext.SaveChangesAsync();

            if (user == null)
            {
                user = new(0, Context.User.Id, DateTimeOffset.Now)
                {
                    Faction = faction,
                };
                DbContext.DcohDiscordUsers.Add(user);
            }
            else
            {
                user.Faction = faction;
                user.FactionJoined = DateTimeOffset.Now;
            }
            faction.CreatedBy = user;
            await DbContext.SaveChangesAsync();

            AllowedMentions mentions = new()
            {
                AllowedTypes = AllowedMentionTypes.Users,
            };
            await RespondAsync($"{Context.User.Mention} registered the squadron **{Format.Sanitize(user.Faction.Name)} ({Format.Sanitize(user.Faction.Short)})**.", allowedMentions: mentions);
        }

        [GeneratedRegex("^[0-9a-z \\-]{4,64}$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-CH")]
        private static partial Regex SquadronNameRegexGenerator();

        [GeneratedRegex("^[A-Z0-9]{4}$", RegexOptions.Compiled, "en-CH")]
        private static partial Regex SquadronIdRegexGenerator();
    }
}
