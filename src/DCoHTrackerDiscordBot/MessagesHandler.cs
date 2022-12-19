using Discord.WebSocket;
using EDSystemProgress;
using EDUtils;

namespace DCoHTrackerDiscordBot
{
    internal class MessagesHandler
    {
        private EdDbContext DbContext { get; }
        private IConfiguration Configuration { get; }
        private ILogger Log { get; }
        public MessagesHandler(EdDbContext dbContext, IConfiguration configuration, ILogger<MessagesHandler> log)
        {
            DbContext = dbContext;
            Configuration = configuration;
            Log = log;
        }

        public async ValueTask ProcessMessage(SocketUserMessage message)
        {
            if (!(Configuration.GetSection("Discord:ProgressScreenshotsChannels").Get<List<ulong>>()?.Contains(message.Channel.Id) ?? false))
            {
                return;
            }
            if (message.Attachments.Any())
            {
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Thargoid War Progress")
                    .WithFooter("This feature is still in a beta stage and not perfect.");
                int screenshots = 0;
                foreach (Attachment attachment in message.Attachments.Take(5))
                {
                    if (Path.GetExtension(attachment.Filename) == ".png" && attachment.Size < 1024 * 1024)
                    {
                        using HttpClient httpClient = new();
                        using HttpResponseMessage httpResponse = await httpClient.GetAsync(attachment.Url);
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            await using MemoryStream imageContent = new();
                            await httpResponse.Content.CopyToAsync(imageContent);
                            if (imageContent.Length == attachment.Size)
                            {
                                imageContent.Position = 0;
                                ExtractSystemProgressResult result = await SystemProgressRecognition.ExtractSystemProgress(imageContent, Log);
                                if (result.Success)
                                {
                                    embed.AddField("System Name", Format.Sanitize(result.SystemName));
                                    embed.AddField("System State", result.SystemStatus.GetEnumMemberValue(), true);
                                    embed.AddField("Progress", result.Progress + "%", true);
                                    embed.AddField("Remaining Time", result.RemainingTime.TotalDays + " days", true);
                                    screenshots++;

                                    StarSystem? starSystem = await DbContext.StarSystems
                                        .Include(s => s.ThargoidLevel)
                                        .FirstOrDefaultAsync(s =>
                                            EF.Functions.Like(s.Name, result.SystemName.Replace("%", string.Empty)) &&
                                            s.ThargoidLevel != null &&
                                            s.ThargoidLevel.State > StarSystemThargoidLevelState.None);
                                    if (starSystem?.ThargoidLevel != null)
                                    {
                                        StarSystemThargoidLevelState thargoidState = result.SystemStatus switch
                                        {
                                            SystemStatus.AlertInProgress or SystemStatus.AlertPrevented => StarSystemThargoidLevelState.Alert,
                                            SystemStatus.InvasionInProgress or SystemStatus.InvasionPrevented => StarSystemThargoidLevelState.Invasion,
                                            SystemStatus.ThargoidControlled => StarSystemThargoidLevelState.Controlled,
                                            SystemStatus.Recovery => StarSystemThargoidLevelState.Recovery,
                                            _ => StarSystemThargoidLevelState.None,
                                        };
                                        short progress = (short)result.Progress;
                                        if (thargoidState == starSystem.ThargoidLevel.State && (starSystem.ThargoidLevel.Progress ?? 0) < progress)
                                        {
                                            starSystem.ThargoidLevel.Progress = progress;
                                            await DbContext.SaveChangesAsync();
                                        }
                                    }
                                    else
                                    {
                                        imageContent.Position = 0;
                                        await File.WriteAllBytesAsync($"Failed_{Guid.NewGuid()}.png", imageContent.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
                if (screenshots > 0)
                {
                    if (screenshots == 1)
                    {
                        embed.Description = "Screenshot shows the following information about the Thargoid war progress.";
                    }
                    else
                    {
                        embed.Description = "Screenshots show the following information about the Thargoid war progress.";
                    }
                    await message.ReplyAsync(embed: embed.Build());
                }
            }
        }
    }
}
