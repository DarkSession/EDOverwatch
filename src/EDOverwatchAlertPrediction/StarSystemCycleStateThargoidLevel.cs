﻿using EDDatabase;

namespace EDOverwatchAlertPrediction
{
    public class StarSystemCycleStateThargoidLevel
    {
        public StarSystemThargoidLevelState State { get; }

        public int StartCycle { get; }

        public int? EndCycle { get; set; }

        public int? Expires { get; }

        public bool Completed { get; }

        public StarSystemCycleStateThargoidLevel(StarSystemThargoidLevel starSystemThargoidLevel)
        {
            State = starSystemThargoidLevel.State;
            StartCycle = (int)(starSystemThargoidLevel.CycleStart!.Start - AlertPrediction.CycleZero).TotalDays / 7;
            if (starSystemThargoidLevel.CycleEnd?.End is DateTimeOffset cycleEndDate)
            {
                EndCycle = (int)(cycleEndDate - AlertPrediction.CycleZero).TotalDays / 7;
            }
            if (starSystemThargoidLevel.StateExpires?.End is DateTimeOffset stateExpiresDate)
            {
                Expires = (int)(stateExpiresDate - AlertPrediction.CycleZero).TotalDays / 7;
            }
            Completed = starSystemThargoidLevel.Progress is short progress && progress >= 100;
        }

        public StarSystemCycleStateThargoidLevel(StarSystemThargoidLevelState state, int startCycle, int? endCycle, int? expires, bool completed)
        {
            State = state;
            StartCycle = startCycle;
            EndCycle = endCycle;
            Expires = expires;
            Completed = completed;
        }
    }
}