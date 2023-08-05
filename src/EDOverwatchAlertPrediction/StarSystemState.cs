using EDDatabase;

namespace EDOverwatchAlertPrediction
{
    public class StarSystemState
    {
        public StarSystemThargoidLevelState State { get; }

        public DateTimeOffset StateStartDate { get; }

        public DateTimeOffset? StateEndDate { get;}

        public bool IsActive(DateTimeOffset date)
        {
            return StateStartDate >= date && (StateEndDate is not DateTimeOffset endDate || endDate < date);
        }

        public StarSystemState(EDDatabase.StarSystemThargoidLevel starSystemThargoidLevel)
        {
            State = starSystemThargoidLevel.State;
            StateStartDate = starSystemThargoidLevel.CycleStart!.Start;
            StateEndDate = starSystemThargoidLevel.CycleEnd?.End;
        }
    }
}
