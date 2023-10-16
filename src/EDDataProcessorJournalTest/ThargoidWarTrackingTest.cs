using EDDataProcessor.CApiJournal;
using Microsoft.Extensions.DependencyInjection;

namespace EDDataProcessorJournalTest
{
    [TestClass]
    public class ThargoidWarTrackingTest : TestDbContext
    {
        [TestMethod]
        [DataRow("{ \"timestamp\":\"2023-05-13T13:35:33Z\", \"event\":\"FSDJump\", \"Taxi\":false, \"Multicrew\":false, \"StarSystem\":\"Baudani\", \"SystemAddress\":2868903552385, \"StarPos\":[-80.71875,-125.18750,-99.21875], \"SystemAllegiance\":\"Independent\", \"SystemEconomy\":\"$economy_Service;\", \"SystemEconomy_Localised\":\"Service\", \"SystemSecondEconomy\":\"$economy_Agri;\", \"SystemSecondEconomy_Localised\":\"Agriculture\", \"SystemGovernment\":\"$government_Democracy;\", \"SystemGovernment_Localised\":\"Democracy\", \"SystemSecurity\":\"$SYSTEM_SECURITY_low;\", \"SystemSecurity_Localised\":\"Low Security\", \"Population\":90750, \"Body\":\"Baudani\", \"BodyID\":0, \"BodyType\":\"Star\", \"ThargoidWar\":{ \"CurrentState\":\"Thargoid_Probing\", \"NextStateSuccess\":\"\", \"NextStateFailure\":\"Thargoid_Harvest\", \"SuccessStateReached\":false, \"WarProgress\":0.631353, \"RemainingPorts\":0 }, \"JumpDist\":17.999, \"FuelUsed\":7.643958, \"FuelLevel\":39.175404, \"Factions\":[ { \"Name\":\"Workers of HIP 6442 Resistance\", \"FactionState\":\"None\", \"Government\":\"Democracy\", \"Influence\":0.087087, \"Allegiance\":\"Federation\", \"Happiness\":\"$Faction_HappinessBand3;\", \"Happiness_Localised\":\"Content\", \"MyReputation\":92.739998 }, { \"Name\":\"Baudani Services\", \"FactionState\":\"None\", \"Government\":\"Corporate\", \"Influence\":0.167167, \"Allegiance\":\"Federation\", \"Happiness\":\"$Faction_HappinessBand3;\", \"Happiness_Localised\":\"Content\", \"MyReputation\":100.000000 }, { \"Name\":\"Parakana Partners\", \"FactionState\":\"None\", \"Government\":\"Corporate\", \"Influence\":0.084084, \"Allegiance\":\"Independent\", \"Happiness\":\"$Faction_HappinessBand3;\", \"Happiness_Localised\":\"Content\", \"MyReputation\":100.000000 }, { \"Name\":\"Baudani Shared\", \"FactionState\":\"None\", \"Government\":\"Cooperative\", \"Influence\":0.042042, \"Allegiance\":\"Independent\", \"Happiness\":\"$Faction_HappinessBand3;\", \"Happiness_Localised\":\"Content\", \"MyReputation\":100.000000 }, { \"Name\":\"Monarchy of Baudani\", \"FactionState\":\"None\", \"Government\":\"Feudal\", \"Influence\":0.065065, \"Allegiance\":\"Independent\", \"Happiness\":\"$Faction_HappinessBand3;\", \"Happiness_Localised\":\"Content\", \"MyReputation\":94.059998 }, { \"Name\":\"Baudani Dragons\", \"FactionState\":\"None\", \"Government\":\"Anarchy\", \"Influence\":0.060060, \"Allegiance\":\"Independent\", \"Happiness\":\"$Faction_HappinessBand3;\", \"Happiness_Localised\":\"Content\", \"MyReputation\":-44.875999 }, { \"Name\":\"Knights of Isarian\", \"FactionState\":\"None\", \"Government\":\"Democracy\", \"Influence\":0.494494, \"Allegiance\":\"Independent\", \"Happiness\":\"$Faction_HappinessBand3;\", \"Happiness_Localised\":\"Content\", \"SquadronFaction\":true, \"MyReputation\":100.000000, \"ActiveStates\":[ { \"State\":\"Boom\" } ] } ], \"SystemFaction\":{ \"Name\":\"Knights of Isarian\" } }\r\n", 2868903552385, StarSystemThargoidLevelState.Alert, 63)]
        public async Task Test(string journalLine, long systemAddress, StarSystemThargoidLevelState expectedSystemState, int expectedProgress)
        {
            Commander commander = new(default, "Test", default, true, default, default, default, default, default, CommanderOAuthStatus.Active, string.Empty, string.Empty, string.Empty, CommanderFleetHasFleetCarrier.Unknown, CommanderPermissions.Default);
            DbContext.Commanders.Add(commander);

            await DbContext.SaveChangesAsync();

            await using AsyncServiceScope serviceScope = Services.CreateAsyncScope();
            JournalProcessor journalProcessor = (JournalProcessor)ActivatorUtilities.CreateInstance(serviceScope.ServiceProvider, typeof(JournalProcessor));

            JournalProcessResult result = await journalProcessor.ProcessCommanderJournal(journalLine, 0, commander, DbContext, null, null, CancellationToken.None);

            StarSystem starSystem = await DbContext.StarSystems
                .Include(s => s.ThargoidLevel)
                .SingleAsync(s => s.SystemAddress == systemAddress);

            Assert.IsNotNull(starSystem.ThargoidLevel);
            Assert.AreEqual(expectedSystemState, starSystem.ThargoidLevel.State);
            Assert.AreEqual(expectedProgress, (short)starSystem.ThargoidLevel.Progress!);
        }
    }
}
