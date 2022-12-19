using EDSystemProgress;
using Microsoft.Extensions.Logging;

namespace EDSystemProgressTest
{
    [TestClass]
    public class ProgressTests
    {
        [TestMethod]
        [DataRow("image1.png", "LAHUA", SystemStatus.InvasionInProgress, 26d, 18)]
        [DataRow("image2.png", "AWARA", SystemStatus.InvasionInProgress, 72d, 17)]
        [DataRow("image3.png", "EBISU", SystemStatus.InvasionInProgress, 90d, 24)]
        [DataRow("image4.png", "EBISU", SystemStatus.InvasionPrevented, 100d, 3)]
        [DataRow("image5.png", "63 ERIDANI", SystemStatus.InvasionInProgress, 56d, 4)]
        [DataRow("image6.png", "VUKURBEH", SystemStatus.AlertPrevented, 100d, 4)]
        [DataRow("image7.png", "VUKURBEH", SystemStatus.AlertInProgress, 12d, 6)]
        [DataRow("image8.png", "HIP 20485", SystemStatus.AlertInProgress, 90d, 1)]
        [DataRow("image9.png", "HIP 20485", SystemStatus.AlertInProgress, 52d, 3)]
        [DataRow("image10.png", "ARIETIS SECTOR AQ-P B5-0", SystemStatus.ThargoidControlled, 0d, 3)] // 2 might not be right
        [DataRow("image11.png", "HIP 23716", SystemStatus.Recovery, 12d, 24)]
        public async Task Test(string fileName, string systemName, SystemStatus systemStatus, double progress, int remainingDays)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
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