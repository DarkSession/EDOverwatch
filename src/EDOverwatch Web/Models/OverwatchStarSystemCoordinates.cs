namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemCoordinates
    {
        public decimal X { get; }
        public decimal Y { get; }
        public decimal Z { get; }
        public OverwatchStarSystemCoordinates(decimal x, decimal y, decimal z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
