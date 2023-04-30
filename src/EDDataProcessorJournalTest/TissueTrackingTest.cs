using EDDataProcessor.CApiJournal;
using Microsoft.Extensions.DependencyInjection;

namespace EDDataProcessorJournalTest
{
    [TestClass]
    public class TissueTrackingTest : TestDbContext
    {
        [TestMethod]
        [DataRow("Journal1.log", 222)]
        public async Task TestCargoTracking(string journalName, int lines)
        {
            string journalFilePath = Path.Join("journals", journalName);
            string journal = await File.ReadAllTextAsync(journalFilePath);
            Commander commander = new(default, "Test", default, true, default, default, default, default, default, CommanderOAuthStatus.Active, string.Empty, string.Empty, string.Empty);
            DbContext.Commanders.Add(commander);

            if (!await DbContext.Stations.AnyAsync(s => s.MarketId == 129019263))
            {
                DbContext.Stations.Add(new(0, "Rescue Ship Bertschinger", 129019263, 0, 1, 1, 1, StationState.Normal, RescueShipType.Primary, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            }

            await DbContext.SaveChangesAsync();

            await using AsyncServiceScope serviceScope = Services.CreateAsyncScope();
            CApiJournalProcessor journalProcessor = (CApiJournalProcessor)ActivatorUtilities.CreateInstance(serviceScope.ServiceProvider, typeof(CApiJournalProcessor));

            JournalProcessResult result = await journalProcessor.ProcessCommanderJournal(journal, 0, commander, DbContext, null, null, CancellationToken.None);
            Assert.AreEqual(lines, result.CurrentLine);

            List<WarEffort> warEfforts = await DbContext.WarEfforts
                .Include(w => w.StarSystem)
                .Where(w => w.Commander == commander)
                .ToListAsync();
            Assert.AreEqual(3, warEfforts.Count);
            Assert.IsNotNull(warEfforts.FirstOrDefault(w =>
                w.Side == WarEffortSide.Humans &&
                w.Type == WarEffortType.TissueSampleCyclops &&
                w.StarSystem?.Name == "Arietis Sector UO-R b4-4" &&
                w.Date == new DateOnly(2023, 3, 25) &&
                w.Amount == 9));

            Assert.IsNotNull(warEfforts.FirstOrDefault(w =>
                w.Side == WarEffortSide.Humans &&
                w.Type == WarEffortType.TissueSampleBasilisk &&
                w.StarSystem?.Name == "Arietis Sector UO-R b4-4" &&
                w.Date == new DateOnly(2023, 3, 25) &&
                w.Amount == 9));
        }
    }
}
