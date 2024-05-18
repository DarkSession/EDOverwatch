using Discord.WebSocket;

namespace DCoHTrackerDiscordBot
{
    internal class MessagesHandler
    {
        private IConfiguration Configuration { get; }

        public MessagesHandler(
            IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async ValueTask ProcessMessage(SocketUserMessage message)
        {
            if (!(Configuration.GetSection("Discord:ProgressScreenshotsChannels").Get<List<ulong>>()?.Contains(message.Channel.Id) ?? false))
            {
                return;
            }
            if (message.Attachments.Count != 0)
            {
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Thargoid War Progress");
                int screenshots = 0;
                foreach (Attachment attachment in message.Attachments.Take(5))
                {
                    if (Path.GetExtension(attachment.Filename) == ".png" && attachment.Size < 1024 * 1024)
                    {
                        screenshots++;
                    }
                }
                if (screenshots > 0)
                {
                    embed.Description = "This feature is no longer supported. Please submit your updates to Overwatch and other community tools by visiting the system while running EDMC or similar.";
                    List<Embed> embeds =
                    [
                        embed.Build(),
                    ];
                    await message.ReplyAsync(embeds: [.. embeds]);
                }
            }
        }
    }
}
