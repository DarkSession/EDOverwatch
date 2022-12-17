using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using System.Reflection;

namespace DCoHTrackerDiscordBot
{
    public class InteractionHandler
    {
        private DiscordSocketClient Client { get; }
        private InteractionService Handler { get; }
        private IServiceProvider Services { get; }
        private IConfiguration Configuration { get; }
        private ILogger Logger { get; }

        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services, IConfiguration config, ILogger<InteractionHandler> log)
        {
            Client = client;
            Handler = handler;
            Services = services;
            Configuration = config;
            Logger = log;
        }

        public async Task InitializeAsync()
        {
            // Process when the client is ready, so we can register our commands.
            Client.Ready += ReadyAsync;
            Handler.Log += LogAsync;
            Handler.InteractionExecuted += InteractionExecuted;

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await Handler.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

            // Process the InteractionCreated payloads to execute Interactions commands
            Client.InteractionCreated += HandleInteraction;
        }

        private Task LogAsync(LogMessage log) => Events.Log.LogAsync(log);

        private async Task InteractionExecuted(ICommandInfo commandInfo, IInteractionContext interactionContext, IResult result)
        {
            await HandleInteractionResult(interactionContext.Interaction, result);
        }

        private async Task ReadyAsync()
        {
            // Context & Slash commands can be automatically registered, but this process needs to happen after the client enters the READY state.
            // Since Global Commands take around 1 hour to register, we should use a test guild to instantly update and test our commands.
            if (Program.IsDebug)
            {
                await Handler.RegisterCommandsToGuildAsync(Configuration.GetValue<ulong>("Discord:TestGuild"), true);
            }
            else
            {
                await Handler.RegisterCommandsGloballyAsync(true);
            }
        }

        private static async ValueTask HandleInteractionResult(IDiscordInteraction interaction, IResult result)
        {
            if (!result.IsSuccess)
            {
                string errorMessage = result.Error switch
                {
                    InteractionCommandError.UnmetPrecondition => "You do not have permission for this interaction.",
                    InteractionCommandError.BadArgs => "You did not provide the required parameters for this interaction.",
                    InteractionCommandError.ParseFailed => "You provided invalid parameters for this interaction.",
                    _ => "Sorry, I was unable to process your request.",
                };
                await interaction.RespondAsync(errorMessage, ephemeral: true);
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                SocketInteractionContext context = new(Client, interaction);

                // Execute the incoming command.
                IResult result = await Handler.ExecuteCommandAsync(context, Services);

                await HandleInteractionResult(interaction, result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Interaction exception");
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                {
                    if (!interaction.HasResponded)
                    {
                        await interaction.RespondAsync("Sorry, I was unable to process your request.", ephemeral: true);
                    }
                    else
                    {
                        RestInteractionMessage msg = await interaction.GetOriginalResponseAsync();
                        await msg.DeleteAsync();
                    }
                }
            }
        }
    }
}
