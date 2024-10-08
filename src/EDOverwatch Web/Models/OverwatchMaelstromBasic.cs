﻿namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromBasic : OverwatchMaelstromProgress
    {
        public int SystemsInAlert { get; }
        public int SystemsInInvasion { get; }
        public int SystemsThargoidControlled { get; }
        public int SystemsInRecovery { get; }
        public decimal DefenseRate { get; }
        public TitanDamageResistance DamageResistance { get; }

        public OverwatchMaelstromBasic(
            ThargoidMaelstrom thargoidMaelstrom,
            int systemsInAlert,
            int systemsInInvasion,
            int systemsThargoidControlled,
            int systemsInRecovery,
            int populatedSystemsInvaded,
            int populatedAlertsDefended,
            int populatedInvasionsDefended) :
            base(thargoidMaelstrom)
        {
            SystemsInAlert = systemsInAlert;
            SystemsInInvasion = systemsInInvasion;
            SystemsThargoidControlled = systemsThargoidControlled;
            SystemsInRecovery = systemsInRecovery;
            var invasionsAlertsTotal = (populatedSystemsInvaded + populatedAlertsDefended);
            if (invasionsAlertsTotal > 0)
            {
                DefenseRate = Math.Round((decimal)(populatedInvasionsDefended + populatedAlertsDefended) / (decimal)invasionsAlertsTotal, 4);
            }
            DamageResistance = TitanDamageResistance.GetDamageResistance(systemsThargoidControlled, thargoidMaelstrom.HeartsRemaining);
        }
    }
}
