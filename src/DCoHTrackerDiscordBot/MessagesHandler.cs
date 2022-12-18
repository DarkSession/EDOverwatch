using Discord;
using Discord.WebSocket;
using EDSystemProgress;
using EDUtils;

namespace DCoHTrackerDiscordBot
{
    internal class MessagesHandler
    {
        private EdDbContext DbContext { get; }
        private IConfiguration Configuration { get; }
        public MessagesHandler(EdDbContext dbContext, IConfiguration configuration)
        {
            DbContext = dbContext;
            Configuration = configuration;
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
                    .WithTitle("Thargoid War Progress");
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

                            }
                            imageContent.Position = 0;
                            ExtractSystemProgressResult result = await SystemProgressRecognition.ExtractSystemProgress(imageContent);
                            if (result.Success)
                            {
                                embed.AddField("System Name", Format.Sanitize(result.SystemName));
                                embed.AddField("System State", result.SystemStatus.GetEnumMemberValue(), true);
                                embed.AddField("Progress", result.Progress + "%", true);
                                embed.AddField("Remaining Time", result.RemainingTime.TotalDays + " days", true);
                                screenshots++;
                            }
                        }
                    }
                }
                if (screenshots > 0)
                {
                    if (screenshots == 1)
                    {
                        embed.Description = "Screenshot shows the following information about the Thargoid War Progress";
                    }
                    else
                    {
                        embed.Description = "Screenshots show the following information about the Thargoid War Progress";
                    }
                    await message.ReplyAsync(embed: embed.Build());
                }
            }
        }
    }
}
