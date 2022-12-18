using EDSystemProgress;

namespace EDSystemProgressTest
{
    [TestClass]
    public class ProgressTests
    {
        [TestMethod]
        [DataRow("image1.png", "LAHUA", SystemStatus.InvasionInProgress, 28d, 18)]
        [DataRow("image2.png", "AWARA", SystemStatus.InvasionInProgress, 74d, 17)]
        [DataRow("image3.png", "EBISU", SystemStatus.InvasionInProgress, 92d, 24)]
        [DataRow("image4.png", "EBISU", SystemStatus.InvasionPrevented, 100d, 3)]
        [DataRow("image5.png", "63 ERIDANI", SystemStatus.InvasionInProgress, 58d, 4)]
        [DataRow("image6.png", "VUKURBEH", SystemStatus.AlertPrevented, 100d, 4)]
        [DataRow("image7.png", "VUKURBEH", SystemStatus.AlertInProgress, 14d, 6)]
        [DataRow("image8.png", "HIP 20485", SystemStatus.AlertInProgress, 92d, 1)]
        [DataRow("image9.png", "HIP 20485", SystemStatus.AlertInProgress, 54d, 3)]
        [DataRow("image10.png", "ARIETIS SECTOR AQ-P B5-0", SystemStatus.ThargoidControlled, 2d, 3)] // 2 might not be right
        [DataRow("image11.png", "HIP 23716", SystemStatus.Recovery, 14d, 24)]
        public async Task Test(string fileName, string systemName, SystemStatus systemStatus, double progress, int remainingDays)
        {
            await using MemoryStream imageContent = new();
            {
                await using FileStream fileStream = File.OpenRead($"./test/{fileName}");
                await fileStream.CopyToAsync(imageContent);
                imageContent.Position = 0;
            }
            ExtractSystemProgressResult result = await SystemProgressRecognition.ExtractSystemProgress(imageContent);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(systemName, result.SystemName);
            Assert.AreEqual(systemStatus, result.SystemStatus);
            Assert.AreEqual((decimal)progress, result.Progress);
            Assert.AreEqual(remainingDays, (int)result.RemainingTime.TotalDays);
        }
    }
}