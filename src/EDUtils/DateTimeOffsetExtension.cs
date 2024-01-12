namespace EDUtils
{
    public static class DateTimeOffsetExtension
    {
        public static DateTimeOffset WithoutMiliseconds(this DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.AddMilliseconds(dateTimeOffset.Millisecond * -1).AddMicroseconds(dateTimeOffset.Microsecond * -1);
        }
    }
}
