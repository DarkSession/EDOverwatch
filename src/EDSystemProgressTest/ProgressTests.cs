using EDSystemProgress;
using Microsoft.Extensions.Logging;

namespace EDSystemProgressTest
{
    [TestClass]
    public class ProgressTests
    {
        // Missing states: AlertPrevented, ThargoidControlledRegainedPopulated
        [TestMethod]
        [DataRow("test01.png", "PATOLLU", SystemStatus.RecoveryComplete, 100d, 1)]
        [DataRow("test02.png", "GARONGXIANS", SystemStatus.InvasionInProgress, 66d, 15)]
        [DataRow("test03.png", "JEMENTI", SystemStatus.InvasionInProgress, 2d, 8)]
        [DataRow("test04.png", "VUKURBEH", SystemStatus.RecoveryComplete, 100d, 1)]
        [DataRow("test05.png", "DAO TZU", SystemStatus.InvasionPrevented, 100d, 1)]
        [DataRow("test06.png", "COL 285 SECTOR JW-M C7-18", SystemStatus.ThargoidControlledRegainedUnpopulated, 100d, 1)]
        [DataRow("test07.png", "HIP 20899", SystemStatus.InvasionInProgress, 70d, 22)]
        [DataRow("test08.png", "HIP 20605", SystemStatus.ThargoidControlled, 44d, 1)]
        [DataRow("test09.png", "AOWICHA", SystemStatus.InvasionInProgress, 26d, 15)]
        [DataRow("test10.png", "PUTAS", SystemStatus.AlertInProgressPopulated, 12d, 1)]
        [DataRow("test11.png", "IMEUT", SystemStatus.AlertInProgressPopulated, 0d, 1)]
        [DataRow("test12.png", "HIP 20563", SystemStatus.ThargoidControlled, 0d, 1)]
        [DataRow("test13.png", "HIP 20916", SystemStatus.ThargoidControlled, 0d, 1)]
        [DataRow("test14.png", "HIP 22496", SystemStatus.InvasionInProgress, 0d, 8)]
        [DataRow("test15.png", "COL 285 SECTOR AF-E B13-5", SystemStatus.Recovery, 26d, 22)]
        [DataRow("test16.png", "HR 1358", SystemStatus.AlertInProgressUnpopulated, 0d, 1)]
        [DataRow("test17.png", "HIP 19781", SystemStatus.HumanControlled, 0d, 0)]
        [DataRow("test18.png", "HR 457", SystemStatus.Unpopulated, 0d, 0)]
        [DataRow("test19.png", "VUKURBEH", SystemStatus.RecoveryComplete, 100d, 1)]
        [DataRow("test20.png", "TRIANGULI SECTOR EQ-Y B1", SystemStatus.AlertInProgressUnpopulated, 0d, 1)]
        [DataRow("test21.png", "HIP 20056", SystemStatus.AlertInProgressPopulated, 0d, 1)]
        [DataRow("test22.png", "H PUPPIS", SystemStatus.AlertInProgressPopulated, 6d, 1)]
        [DataRow("test23.png", "AOWICHA", SystemStatus.InvasionInProgress, 30d, 11)]
        public async Task Test(string fileName, string systemName, SystemStatus systemStatus, double progress, int remainingDays)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            ILogger log = loggerFactory.CreateLogger<ProgressTests>();

            await using MemoryStream imageContent = new();
            {
                await using FileStream fileStream = File.OpenRead($"./test/{fileName}");
                await fileStream.CopyToAsync(imageContent);
                imageContent.Position = 0;
            }
            ExtractSystemProgressResult result = await SystemProgressRecognition.ExtractSystemProgress(imageContent, log);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(systemName, result.SystemName);
            Assert.AreEqual(systemStatus, result.SystemStatus);
            Assert.AreEqual((decimal)progress, result.Progress);
            Assert.AreEqual(remainingDays, (int)result.RemainingTime.TotalDays);
        }
    }
}