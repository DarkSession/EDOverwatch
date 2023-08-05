
using EDOverwatchAlertPrediction;

namespace EDOverwatchAlertPredictionTest
{
    [TestClass]
    public class StarSystemTests
    {
        [TestMethod]
        public void TestAttackCostsUnpopulated()
        {
            StarSystem starSystem = new(new(0, 0, "Test", 0, 0, 0, 0, 0, 0, true, true, default, default)
            {
                ThargoidLevelHistory = new(),
            });
            Assert.AreEqual(1, starSystem.AttackCost(new DateTimeOffset(2023, 3, 9, 7, 0, 0, TimeSpan.Zero)));
            Assert.AreEqual(4, starSystem.AttackCost(new DateTimeOffset(2023, 3, 16, 7, 0, 0, TimeSpan.Zero)));
            Assert.AreEqual(4, starSystem.AttackCost(new DateTimeOffset(2023, 8, 3, 7, 0, 0, TimeSpan.Zero)));
        }

        [TestMethod]
        public void TestAttackCostsPopulated()
        {
            StarSystem starSystem = new(new(0, 0, "Test", 0, 0, 0, 1, 1, 1, true, true, default, default)
            {
                ThargoidLevelHistory = new(),
            });
            Assert.AreEqual(4, starSystem.AttackCost(new DateTimeOffset(2023, 3, 9, 7, 0, 0, TimeSpan.Zero)));
            Assert.AreEqual(4, starSystem.AttackCost(new DateTimeOffset(2023, 8, 3, 7, 0, 0, TimeSpan.Zero)));
        }
    }
}