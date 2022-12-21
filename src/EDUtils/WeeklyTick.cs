namespace EDUtils
{
    public static class WeeklyTick
    {
        public static DateTimeOffset GetLastTick() => GetTickTime(DateTimeOffset.UtcNow);

        public static DateTimeOffset GetTickTime(DateTimeOffset dateTimeOffset, int weekOffset = 0)
        {
            int dayOffset = dateTimeOffset.DayOfWeek switch
            {
                DayOfWeek.Sunday => -3,
                DayOfWeek.Monday => -4,
                DayOfWeek.Tuesday => -5,
                DayOfWeek.Wednesday => -6,
                DayOfWeek.Thursday => 0,
                DayOfWeek.Friday => -1,
                DayOfWeek.Saturday => -2,
                _ => 0,
            };
            DateTimeOffset lastThursday = dateTimeOffset.AddDays(dayOffset);
            DateTimeOffset lastThursdayCycle = new(lastThursday.Year, lastThursday.Month, lastThursday.Day, 7, 0, 0, TimeSpan.Zero);
            return lastThursdayCycle.AddDays(weekOffset * 7);
        }
    }
}
