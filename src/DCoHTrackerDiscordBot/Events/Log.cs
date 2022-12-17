namespace DCoHTrackerDiscordBot.Events
{
    internal static class Log
    {
        public static Task LogAsync(LogMessage log)
        {
            LogLevel logLevel = log.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.Information,
            };
#pragma warning disable CA2254 // Template should be a static expression
            Program.Log!.Log(logLevel, log.Exception, message: log.Message);
#pragma warning restore CA2254 // Template should be a static expression
            return Task.CompletedTask;
        }
    }
}
