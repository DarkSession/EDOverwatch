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
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Client watchdog exception");
                }
            }
        }

        private async Task WatchClient(Client client, CancellationTokenSource cancellationTokenSource)
        {
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
            CancellationTokenSource clientCancellationTokenSource = new();
            try
            {
                Client client = ServiceProvider.GetRequiredService<Client>();
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
