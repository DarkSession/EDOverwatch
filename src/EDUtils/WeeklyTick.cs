﻿namespace EDUtils
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
            if (dateTimeOffset.DayOfWeek == DayOfWeek.Thursday && dateTimeOffset.Hour < 7)
            {
                weekOffset -= 1;
            }
            DateTimeOffset lastThursdayCycle = new(lastThursday.Year, lastThursday.Month, lastThursday.Day, 7, 0, 0, TimeSpan.Zero);
            return lastThursdayCycle.AddDays(weekOffset * 7);
        }

        public static DateTimeOffset GetTickTime(DateOnly date, int weekOffset = 0)
        {
            var dayOffset = date.DayOfWeek switch
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
            DateOnly thursday = date.AddDays(dayOffset);
            DateTimeOffset lastThursdayCycle = new(thursday.Year, thursday.Month, thursday.Day, 7, 0, 0, TimeSpan.Zero);
            return lastThursdayCycle.AddDays(weekOffset * 7);
        }

        public static int GetNumberOfTicksSinceDate(DateTimeOffset tickDateTime)
        {
            var lastTick = GetLastTick();
            return (int)Math.Floor((lastTick - tickDateTime).Days / 7d);
        }
    }
}
