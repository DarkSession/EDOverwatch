namespace EDOverwatch_Web.Models
{
    public class OverwatchOverviewThargoids
    {
        public double ControllingPercentage { get; set; }
        public int ActiveMaelstroms { get; set; }
        public int SystemsControlling { get; set; }
        public long CommanderKills { get; set; }
        public long RefugeePopulation { get; set; }

        public OverwatchOverviewThargoids(double controllingPercentage, int activeMaelstroms, int systemsControlling, long commanderKills, long refugeePopulation)
        {
            ControllingPercentage = controllingPercentage;
            ActiveMaelstroms = activeMaelstroms;
            SystemsControlling = systemsControlling;
            CommanderKills = commanderKills;
            RefugeePopulation = refugeePopulation;
        }
    }
}
