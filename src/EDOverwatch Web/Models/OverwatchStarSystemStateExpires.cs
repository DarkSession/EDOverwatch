namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemStateExpires
    {
        public DateTimeOffset StateExpires { get; }
        public DateTimeOffset CurrentCycleEnds { get; }
        public short? RemainingCycles { get; }

        public OverwatchStarSystemStateExpires(DateTimeOffset stateExpires, DateTimeOffset currentCycleEnds, short? remainingCycles)
        {
            StateExpires = stateExpires;
            CurrentCycleEnds = currentCycleEnds;
            RemainingCycles = remainingCycles;
        }
    }
}
