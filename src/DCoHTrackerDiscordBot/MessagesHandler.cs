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
                                            StarSystemThargoidLevelProgress starSystemThargoidLevelProgress = new(0, DateTimeOffset.UtcNow, progress)
                                            {
                                                ThargoidLevel = starSystem.ThargoidLevel,
                                            };
                                            DbContext.StarSystemThargoidLevelProgress.Add(starSystemThargoidLevelProgress);
                                            starSystem.ThargoidLevel.CurrentProgress = starSystemThargoidLevelProgress;
                                            await DbContext.SaveChangesAsync();
                                        }
                                    }
                                    else
                                    {
                                        imageContent.Position = 0;
                                        await File.WriteAllBytesAsync($"Failed_{Guid.NewGuid()}.png", imageContent.ToArray());
                                    }

                                    List<string> remainingTime = new();
                                    int weeks = (int)Math.Floor(result.RemainingTime.TotalDays / 7d);
                                    int days = (int)result.RemainingTime.TotalDays - (weeks * 7);

                                    if (days > 0)
                                    {
                                        if (days == 1)
                                        {
                                            remainingTime.Add("1 day");
                                        }
                                        else
                                        {
                                            remainingTime.Add($"{days} days");
                                        }
                                    }
                                    if (weeks > 0)
                                    {
                                        if (weeks == 1)
                                        {
                                            remainingTime.Add("1 week");
                                        }
                                        else
                                        {
                                            remainingTime.Add($"{weeks} weeks");
                                        }
                                    }

                                    embed.AddField("System Name", Format.Sanitize(result.SystemName));
                                    embed.AddField("System State", result.SystemStatus.GetEnumMemberValue(), true);
                                    embed.AddField("Progress", result.Progress + "%", true);
                                    embed.AddField("Remaining Time", string.Join(", ", remainingTime), true);
                                    screenshots++;
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
