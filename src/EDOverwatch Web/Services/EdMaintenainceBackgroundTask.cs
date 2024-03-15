namespace EDOverwatch_Web.Services
{
    public class EdMaintenainceBackgroundTask : BackgroundService
    {
        private EdMaintenance EdMaintenance { get; }
        private ILogger Log { get; }

        public EdMaintenainceBackgroundTask(EdMaintenance edMaintenance, ILogger<EdMaintenainceBackgroundTask> log)
        {
            EdMaintenance = edMaintenance;
            Log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(5));

            while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await EdMaintenance.Update(cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, "Exception while executing update");
                }
            }
        }
    }
}
