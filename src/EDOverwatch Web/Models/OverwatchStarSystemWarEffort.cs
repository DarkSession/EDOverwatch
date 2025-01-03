﻿using Newtonsoft.Json;

namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemWarEffort
    {
        public DateOnly Date { get; }

        public string Type { get; }
        public int TypeId { get; }
        [JsonIgnore]
        public WarEffortType WarEffortType { get; }

        public string Source { get; }
        public int SourceId { get; }
        [JsonIgnore]
        public WarEffortSource WarEffortSource { get; }

        public string TypeGroup { get; }
        public long Amount { get; }

        public OverwatchStarSystemWarEffort(DateOnly date, WarEffortType type, WarEffortSource source, long amount)
        {
            Date = date;

            Type = type.GetEnumMemberValue();
            TypeId = (int)type;
            WarEffortType = type;

            Source = source.GetEnumMemberValue();
            SourceId = (int)source;
            WarEffortSource = source;

            if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(type, out WarEffortTypeGroup group))
            {
                TypeGroup = group.GetEnumMemberValue();
            }
            else
            {
                TypeGroup = string.Empty;
            }
            Amount = amount;
        }
    }
}
