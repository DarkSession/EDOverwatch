namespace EDOverwatch
{
    internal static class AsyncLock
    {
        public static async Task<AsyncLockInstance> AquireLockInstance(SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken)
        {
            await semaphoreSlim.WaitAsync(cancellationToken);
            return new AsyncLockInstance(semaphoreSlim);
        }
    }

    internal class AsyncLockInstance : IDisposable
    {
        private SemaphoreSlim SemaphoreSlim { get; }

        public AsyncLockInstance(SemaphoreSlim semaphoreSlim)
        {
            SemaphoreSlim = semaphoreSlim;
        }

        public void Dispose()
        {
            SemaphoreSlim.Release();
        }
    }
}
