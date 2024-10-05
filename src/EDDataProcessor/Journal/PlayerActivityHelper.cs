namespace EDDataProcessor.Journal
{
    internal static class PlayerActivityHelper
    {
        public static async ValueTask<bool> RegisterPlayerActivity(string playerId, DateTimeOffset activityTime, StarSystem starSystem, EdDbContext dbContext)
        {
            if ((DateTimeOffset.UtcNow - activityTime).TotalHours >= 24 || starSystem.ThargoidLevel is null || starSystem.ThargoidLevel.State == StarSystemThargoidLevelState.None)
            {
                return false;
            }

            var hash = EDUtils.Activity.PlayerActivityHash(playerId, activityTime);
            if (await dbContext.PlayerActivities.AnyAsync(d => d.Hash == hash))
            {
                return false;
            }

            var dateHour = activityTime.Year * 1000000 + activityTime.Month * 10000 + activityTime.Day * 100 + activityTime.Hour;

            dbContext.PlayerActivities.Add(new PlayerActivity()
            {
                Id = 0,
                StarSystemId = starSystem.Id,
                StarSystem = null,
                DateHour = dateHour,
                Hash = hash,
            });

            return true;
        }
    }
}
