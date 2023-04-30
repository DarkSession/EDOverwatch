namespace EDDataProcessor.Journal.Events.Travel
{
    internal class Location : FSDJump
    {
        public Location(long? population, string starSystem, long systemAddress, List<double> starPos, string? systemGovernment, string? systemAllegiance, string? systemEconomy, string? systemSecurity) :
            base(population, starSystem, systemAddress, starPos, systemGovernment, systemAllegiance, systemEconomy, systemSecurity)
        {
        }
    }
}
