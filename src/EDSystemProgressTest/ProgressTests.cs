using EDSystemProgress;
using Microsoft.Extensions.Logging;

namespace EDSystemProgressTest
{
    [TestClass]
    public class ProgressTests
    {
        // Missing states:  ThargoidControlledRegainedPopulated
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
        [DataRow("test24.png", "CEPHEI SECTOR YZ-Y B4", SystemStatus.InvasionInProgress, 42d, 16)]
        [DataRow("test25.png", "HUILE", SystemStatus.InvasionInProgress, 82d, 9)]
        [DataRow("test26.png", "HUILE", SystemStatus.InvasionInProgress, 82d, 9)]
        [DataRow("test27.png", "SENOCIDI", SystemStatus.AlertPreventedPopulated, 100d, 1)]
        [DataRow("test28.png", "EBISU", SystemStatus.Recovery, 2d, 20)]
        [DataRow("test29.png", "HIP 21380", SystemStatus.RecoveryComplete, 100d, 6)]
        [DataRow("test30.png", "SENOCIDI", SystemStatus.HumanControlled, 0d, 0)]
        [DataRow("test31.png", "65 KAPPA TAURI", SystemStatus.HumanControlled, 0d, 0)]
        [DataRow("test32.png", "67 TAURI", SystemStatus.HumanControlled, 0d, 0)]
        [DataRow("test33.png", "BIDIAE", SystemStatus.HumanControlled, 0d, 0)]
        [DataRow("test34.png", "HIP 110", SystemStatus.HumanControlled, 0d, 0)]
        [DataRow("test35.png", "HIP 21991", SystemStatus.AlertInProgressPopulated, 46d, 3)]
        [DataRow("test36.png", "IMEUT", SystemStatus.InvasionInProgress, 26d, 12)]
        [DataRow("test37.png", "HIP 20899", SystemStatus.InvasionInProgress, 86d, 19)]
        [DataRow("test38.png", "TRIANGULI SECTOR BA-A D84", SystemStatus.ThargoidControlled, 2d, 1)]
        [DataRow("test39.png", "COL 285 SECTOR AF-E B13-0", SystemStatus.AlertInProgressUnpopulated, 54d, 3)]
        [DataRow("test41.png", "COL 285 SECTOR AF-E B13-0", SystemStatus.AlertInProgressUnpopulated, 24d, 3)]
        [DataRow("test42.png", "COL 285 SECTOR AF-E B13-0", SystemStatus.AlertPreventedUnpopulated, 100d, 1)]
        [DataRow("test43.png", "GLIESE 3050", SystemStatus.ThargoidControlledRegainedUnpopulated, 100d, 1)]
        [DataRow("test44.png", "BI DHORORA", SystemStatus.AlertPreventedPopulated, 100d, 1)]
        [DataRow("test45.png", "ARIETIS SECTOR YU-P B5-0", SystemStatus.ThargoidControlled, 44d, 5)]
        [DataRow("test46.png", "VESTET", SystemStatus.AlertInProgressPopulated, 46d, 3)]
        [DataRow("test47.png", "HIP 18702", SystemStatus.AlertInProgressPopulated, 98d, 3)]
        [DataRow("test48.png", "VESTET", SystemStatus.AlertInProgressPopulated, 64d, 3)]
        [DataRow("test49.png", "VESTET", SystemStatus.AlertInProgressPopulated, 94d, 2)]
        [DataRow("test50.png", "HIP 23816", SystemStatus.ThargoidControlled, 0d, 1)]
        [DataRow("test51.png", "YUKAIT", SystemStatus.ThargoidControlledRegainedPopulated, 100d, 3)]
        [DataRow("test52.png", "AUAKER", SystemStatus.ThargoidControlledRegainedPopulated, 100d, 1)]
        [DataRow("test53.png", "BAUDANI", SystemStatus.AlertInProgressPopulated, 60d, 4)]
        [DataRow("test54.png", "COL 285 SECTOR SH-B B14-4", SystemStatus.ThargoidControlled, 0d, 2)]
        [DataRow("test55.png", "KURUMA", SystemStatus.Recovery, 58d, 15)]
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