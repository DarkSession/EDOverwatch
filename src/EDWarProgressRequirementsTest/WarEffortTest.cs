using EDDatabase;

namespace EDWarProgressRequirementsTest
{
    [TestClass]
    public class WarEffortTest
    {
        [TestMethod]
        [DataRow(23.5d, true, StarSystemThargoidLevelState.Controlled, 2452)]
        public void TestMethod1(double distance, bool populated, StarSystemThargoidLevelState state, int expectedResult)
        {
            int? result = EDWarProgressRequirements.WarEfforts.GetRequirements((decimal)distance, populated, state);
            Assert.AreEqual(expectedResult, result);
        }
    }
}