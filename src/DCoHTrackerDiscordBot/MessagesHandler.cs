using ActiveMQ.Artemis.Client;
using DCoHTrackerDiscordBot.Module;
using Discord.WebSocket;
using EDSystemProgress;
using EDUtils;
using Messages;

namespace DCoHTrackerDiscordBot
{
    internal class MessagesHandler
    {
        private EdDbContext DbContext { get; }
        private IConfiguration Configuration { get; }
        private ILogger Log { get; }
        private ActiveMQ.Artemis.Client.IConnection Connection { get; }
        private IAnonymousProducer AnonymousProducer { get; }

        public MessagesHandler(
            EdDbContext dbContext,
            IConfiguration configuration,
            ILogger<MessagesHandler> log,
            ActiveMQ.Artemis.Client.IConnection connection,
            IAnonymousProducer anonymousProducer)
        {
            DbContext = dbContext;
            Configuration = configuration;
            Log = log;
            Connection = connection;
            AnonymousProducer = anonymousProducer;
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
                List<(string systemName, string message)> updateRequests = new();
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
                                        .ThenInclude(t => t!.Maelstrom)
                                        .Include(s => s.ThargoidLevel.StateExpires)
                                        .FirstOrDefaultAsync(s =>
                                            EF.Functions.Like(s.Name, result.SystemName.Replace("%", string.Empty)) &&
                                            s.ThargoidLevel != null &&
                                            s.ThargoidLevel.State > StarSystemThargoidLevelState.None);
                                    if (starSystem == null)
                                    {
                                        Log.LogWarning("Star system {name} not found!", result.SystemName);
                                        List<string> allStarSystems = (await DbContext.StarSystems
                                            .AsNoTracking()
                                            .Where(s =>
                                                s.ThargoidLevel != null &&
                                                s.ThargoidLevel.State > StarSystemThargoidLevelState.None)
                                            .Select(s => s.Name)
                                            .ToListAsync()).Select(s => s.ToUpper()).ToList();

                                        List<string> similarStarSystemNames = allStarSystems
                                            .Where(a => StringUtil.ComputeSimilarity(a, result.SystemName) <= 1)
                                            .ToList();
                                        if (similarStarSystemNames.Any())
                                        {
                                            Log.LogWarning("Found {count} similar star system names", similarStarSystemNames.Count);
                                            if (similarStarSystemNames.Count == 1)
                                            {
                                                string starSystemName = similarStarSystemNames.First();
                                                starSystem = await DbContext.StarSystems
                                                                                        .Include(s => s.ThargoidLevel)
                                                                                        .ThenInclude(t => t!.Maelstrom)
                                                                                        .Include(s => s.ThargoidLevel.StateExpires)
                                                                                        .FirstOrDefaultAsync(s =>
                                                                                            EF.Functions.Like(s.Name, starSystemName) &&
                                                                                            s.ThargoidLevel != null &&
                                                                                            s.ThargoidLevel.State > StarSystemThargoidLevelState.None);
                                            }
                                        }
                                    }
                                    if (starSystem?.ThargoidLevel != null)
                                    {
                                        StarSystemThargoidLevelState thargoidState = result.SystemStatus switch
                                        {
                                            SystemStatus.AlertInProgressPopulated or SystemStatus.AlertInProgressUnpopulated or SystemStatus.AlertPrevented => StarSystemThargoidLevelState.Alert,
                                            SystemStatus.InvasionInProgress or SystemStatus.InvasionPrevented => StarSystemThargoidLevelState.Invasion,
                                            SystemStatus.ThargoidControlled or SystemStatus.ThargoidControlledRegainedUnpopulated  => StarSystemThargoidLevelState.Controlled,
                                            SystemStatus.Recovery or SystemStatus.RecoveryComplete => StarSystemThargoidLevelState.Recovery,
                                            SystemStatus.HumanControlled or SystemStatus.Unpopulated => StarSystemThargoidLevelState.None,
                                            _ => StarSystemThargoidLevelState.None,
                                        };
                                        short progress = (short)result.Progress;
                                        if (thargoidState != starSystem.ThargoidLevel.State)
                                        {
                                            (_, string updateRequestMessage) = await TrackingModule.SystemUpdateRequest(starSystem.Name, message.Author.Id, message.Channel.Id, DbContext, AnonymousProducer);
                                            updateRequests.Add((starSystem.Name, updateRequestMessage));
                                        }
                                        else if ((starSystem.ThargoidLevel.Progress ?? -1) < progress)
                                        {
                                            starSystem.ThargoidLevel.Progress = progress;
                                            StarSystemThargoidLevelProgress starSystemThargoidLevelProgress = new(0, DateTimeOffset.UtcNow, progress)
                                            {
                                                ThargoidLevel = starSystem.ThargoidLevel,
                                            };
                                            DbContext.StarSystemThargoidLevelProgress.Add(starSystemThargoidLevelProgress);
                                            starSystem.ThargoidLevel.CurrentProgress = starSystemThargoidLevelProgress;

                                            if (result.RemainingTime > TimeSpan.Zero && starSystem.ThargoidLevel.StateExpires == null)
                                            {
                                                DateTimeOffset remainingTimeEnd = DateTimeOffset.UtcNow.Add(result.RemainingTime);
                                                if (remainingTimeEnd.DayOfWeek == DayOfWeek.Wednesday || (remainingTimeEnd.DayOfWeek == DayOfWeek.Thursday && remainingTimeEnd.Hour < 7))
                                                {
                                                    remainingTimeEnd = new DateTimeOffset(remainingTimeEnd.Year, remainingTimeEnd.Month, remainingTimeEnd.Day, 0, 0, 0, TimeSpan.Zero);
                                                    ThargoidCycle thargoidCycle = await DbContext.GetThargoidCycle(remainingTimeEnd, CancellationToken.None);
                                                    starSystem.ThargoidLevel.StateExpires = thargoidCycle;
                                                }
                                            }
                                            if (progress >= 100)
                                            {
                                                await DbContext.DcohFactionOperations
                                                    .Where(d =>
                                                        d.StarSystem == starSystem &&
                                                        d.Status == DcohFactionOperationStatus.Active)
                                                    .ForEachAsync(d => d.Status = DcohFactionOperationStatus.Expired);
                                            }
                                            await DbContext.SaveChangesAsync();

                                            await using IProducer starSystemThargoidLevelChangedProducer = await Connection.CreateProducerAsync(StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing);

                                            StarSystemThargoidLevelChanged starSystemThargoidLevelChanged = new(starSystem.SystemAddress);
                                            await starSystemThargoidLevelChangedProducer.SendAsync(starSystemThargoidLevelChanged.Message);
                                        }
                                    }
                                    else
                                    {
                                        imageContent.Position = 0;
                                        await File.WriteAllBytesAsync($"Failed_{Guid.NewGuid()}.png", imageContent.ToArray());

                                        if (starSystem != null)
                                        {
                                            (_, string updateRequestMessage) = await TrackingModule.SystemUpdateRequest(starSystem.Name, message.Author.Id, message.Channel.Id, DbContext, AnonymousProducer);
                                            updateRequests.Add((starSystem.Name, updateRequestMessage));
                                        }
                                    }

                                    List<string> remainingTime = new();
                                    int weeks = (int)Math.Floor(result.RemainingTime.TotalDays / 7d);
                                    int days = (int)result.RemainingTime.TotalDays - (weeks * 7);

                                    if (days == 1)
                                    {
                                        remainingTime.Add("1 day");
                                    }
                                    else if (days > 0)
                                    {
                                        remainingTime.Add($"{days} days");
                                    }
                                    if (weeks == 1)
                                    {
                                        remainingTime.Add("1 week");
                                    }
                                    else if (weeks > 0)
                                    {
                                        remainingTime.Add($"{weeks} weeks");
                                    }

                                    string remainingTimeStr = string.Join(", ", remainingTime);
                                    if (string.IsNullOrEmpty(remainingTimeStr))
                                    {
                                        remainingTimeStr = "-";
                                    }

                                    embed.AddField("System Name", Format.Sanitize(starSystem?.Name ?? result.SystemName));
                                    if (starSystem?.ThargoidLevel?.Maelstrom != null)
                                    {
                                        embed.AddField("Maelstrom", starSystem.ThargoidLevel.Maelstrom.Name ?? "-");
                                    }
                                    embed.AddField("System State", result.SystemStatus.GetEnumMemberValue(), true);
                                    embed.AddField("Progress", result.Progress + "%", true);
                                    embed.AddField("Remaining Time", remainingTimeStr, true);

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
                    List<Embed> embeds = new()
                    {
                        embed.Build(),
                    };
                    if (updateRequests.Any())
                    {
                        EmbedBuilder updateEmebed = new EmbedBuilder()
                            .WithTitle("System Update Request");

                        string text;
                        if (updateRequests.Count == 1)
                        {
                            text = "One screenshot";
                        }
                        else
                        {
                            text = $"{updateRequests.Count} screenshots";
                        }
                        text += " submitted did conflict with the data currently in overwatch. An update request was submitted.";

                        updateEmebed.Description = text;

                        foreach ((string systemName, string updateRequestMessage) in updateRequests)
                        {
                            updateEmebed.AddField(Format.Sanitize(systemName), updateRequestMessage);
                        }

                        embeds.Add(updateEmebed.Build());
                    }
                    await message.ReplyAsync(embeds: embeds.ToArray());
                }
            }
        }
    }
}
