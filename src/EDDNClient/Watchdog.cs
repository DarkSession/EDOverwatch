namespace EDDNClient
{
    internal class Watchdog
    {
        private ILogger Log { get; }
        private IServiceProvider ServiceProvider { get; }

        public Watchdog(ILogger<Watchdog> log, IServiceProvider serviceProvider)
        {
            Log = log;
            ServiceProvider = serviceProvider;
        }

        public async Task StartAsync()
        {
            Log.LogDebug("StartAsync");
            while (true)
            {
                try
                {
                    await StartClient();
                    Log.LogWarning("Client stopped...");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Client watchdog exception");
                }
            }
        }

        private async Task WatchClient(Client client, CancellationTokenSource cancellationTokenSource)
        {
            Log.LogInformation("WatchClient started");
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                TimeSpan timeSinceLastMessage = (DateTimeOffset.UtcNow - client.LastMessageReceived);
                if (timeSinceLastMessage.TotalMinutes >= 5)
                {
                    Log.LogWarning("Client Watchdog: EDDN client did not process or receive a message for more than 5 minutes.");
                    cancellationTokenSource.Cancel();
                    break;
                }
                await Task.Delay(TimeSpan.FromMinutes(5).Subtract(timeSinceLastMessage));
            }
        }

        private async Task StartClient()
        {
            Log.LogInformation("Starting new client instance");
            CancellationTokenSource clientCancellationTokenSource = new();
            try
            {
                using IServiceScope scope = ServiceProvider.CreateScope();
                Client client = scope.ServiceProvider.GetRequiredService<Client>();
                Task watchClientTask = WatchClient(client, clientCancellationTokenSource);
                Task processTask = client.ProcessAsync(clientCancellationTokenSource.Token);
                await Task.WhenAny(watchClientTask, processTask);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Client exception");
                clientCancellationTokenSource.Cancel();
            }
        }
    }
}
