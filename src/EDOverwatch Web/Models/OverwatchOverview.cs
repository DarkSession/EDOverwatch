﻿namespace EDOverwatch_Web.Models
{
    public class OverwatchOverview
    {
        public OverwatchOverviewHuman? Humans { get; set; }
        public OverwatchOverviewThargoids? Thargoids { get; set; }
        public OverwatchOverviewContested? Contested { get; set; }
    }

    public class OverwatchOverviewHuman
    {
        public double ControllingPercentage { get; set; }
        public int SystemsControlling { get; set; }
        public int SystemsRecaptured { get; set; }
        public long? ThargoidKills { get; set; }
        public long? Rescues { get; set; }
        public long? RescueSupplies { get; set; }

        public OverwatchOverviewHuman(double controllingPercentage, int systemsControlling, int systemsRecaptured, long? thargoidKills = null, long? rescues = null, long? rescueSupplies = null)
        {
            ControllingPercentage = controllingPercentage;
            SystemsControlling = systemsControlling;
            SystemsRecaptured = systemsRecaptured;
            ThargoidKills = thargoidKills;
            Rescues = rescues;
            RescueSupplies = rescueSupplies;
        }
    }

    public class OverwatchOverviewThargoids
    {
        public double ControllingPercentage { get; set; }
        public int ActiveMaelstroms { get; set; }
        public int SystemsControlling { get; set; }
        public long? CommanderKills { get; set; }

        public OverwatchOverviewThargoids(double controllingPercentage, int activeMaelstroms, int systemsControlling, long? commanderKills = null)
        {
            ControllingPercentage = controllingPercentage;
            ActiveMaelstroms = activeMaelstroms;
            SystemsControlling = systemsControlling;
            CommanderKills = commanderKills;
        }
    }

    public class OverwatchOverviewContested
    {
        public int SystemsInInvasion { get; set; }
        public int SystemsWithAlerts { get; set; }
        public int SystemsBeingRecaptured { get; set; }

        public OverwatchOverviewContested(int systemsInInvasion, int systemsWithAlerts, int systemsBeingRecaptured)
        {
            SystemsInInvasion = systemsInInvasion;
            SystemsWithAlerts = systemsWithAlerts;
            SystemsBeingRecaptured = systemsBeingRecaptured;
        }
    }
}
