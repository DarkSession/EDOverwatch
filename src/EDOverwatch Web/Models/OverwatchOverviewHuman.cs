namespace EDOverwatch_Web.Models
{
    public class OverwatchOverviewHuman
    {
        public double ControllingPercentage { get; set; }
        public int SystemsControlling { get; set; }
        public int SystemsRecaptured { get; set; }
        public long? ThargoidKills { get; set; }
        public long? Rescues { get; set; }
        public long? RescueSupplies { get; set; }
        public long? Missions { get; set; }
        public long? ItemsRecovered { get; set; }
        public long? TissuesSampled { get; set; }

        public OverwatchOverviewHuman(double controllingPercentage, int systemsControlling, int systemsRecaptured, long? thargoidKills, long? rescues, long? rescueSupplies, long? missions, long? itemsRecovered, long? tissuesSampled)
        {
            ControllingPercentage = controllingPercentage;
            SystemsControlling = systemsControlling;
            SystemsRecaptured = systemsRecaptured;
            ThargoidKills = thargoidKills;
            Rescues = rescues;
            RescueSupplies = rescueSupplies;
            Missions = missions;
            ItemsRecovered = itemsRecovered;
            TissuesSampled = tissuesSampled;
        }
    }
}
