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
        [DataRow("image7.png", "VUKURBEH", SystemStatus.AlertInProgressPopulated, 12d, 6)]
        [DataRow("image8.png", "HIP 20485", SystemStatus.AlertInProgressPopulated, 90d, 1)]
        [DataRow("image9.png", "HIP 20485", SystemStatus.AlertInProgressPopulated, 52d, 3)]
        [DataRow("image10.png", "ARIETIS SECTOR AQ-P B5-0", SystemStatus.ThargoidControlled, 0d, 3)] // 2 might not be right
        [DataRow("image11.png", "HIP 23716", SystemStatus.Recovery, 12d, 24)]
        [DataRow("image12.png", "OBAMUMBO", SystemStatus.InvasionInProgress, 32d, 9)]
        [DataRow("image13.png", "29 E ORIONIS", SystemStatus.InvasionPrevented, 100d, 1)]
        [DataRow("image14.png", "HIP 20527", SystemStatus.AlertInProgressPopulated, 30d, 1)]
        [DataRow("image15.png", "OBAMUMBO", SystemStatus.InvasionInProgress, 40d, 8)]
        [DataRow("image16.png", "HIP 20527", SystemStatus.AlertInProgressPopulated, 32d, 1)]
        [DataRow("image17.png", "MURUIDOOGES", SystemStatus.InvasionInProgress, 58d, 28)]
        [DataRow("image18.png", "MURUIDOOGES", SystemStatus.InvasionInProgress, 58d, 28)]
        [DataRow("image19.png", "HIP 20916", SystemStatus.InvasionInProgress, 4d, 7)]
        [DataRow("image20.png", "HIP 20890", SystemStatus.InvasionInProgress, 2d, 21)]
        [DataRow("image21.png", "NU GUANG", SystemStatus.InvasionInProgress, 2d, 27)]
        [DataRow("image22.png", "OBAMUMBO", SystemStatus.InvasionInProgress, 10d, 6)]
        [DataRow("image23.png", "HIP 23716", SystemStatus.Recovery, 26d, 20)]
        [DataRow("image24.png", "MUCHIHIKS", SystemStatus.InvasionInProgress, 10d, 0)]
        [DataRow("image25.png", "MURUIDOOGES", SystemStatus.RecoveryComplete, 100d, 3)]
        [DataRow("image26.png", "78 THETA-2 TAURI", SystemStatus.AlertInProgressUnpopulated, 0d, 2)]
        [DataRow("image27.png", "HIP 8525", SystemStatus.InvasionInProgress, 70d, 9)]
        [DataRow("image28.png", "MAPON", SystemStatus.InvasionInProgress, 36d, 7)]
        [DataRow("image29.png", "GLIESE 9035", SystemStatus.InvasionInProgress, 20d, 21)]
        [DataRow("image30.png", "HR 2204", SystemStatus.InvasionInProgress, 18d, 7)]
        [DataRow("image31.png", "AKBAKARA", SystemStatus.InvasionInProgress, 26d, 7)]
        [DataRow("image32.png", "IMEUT", SystemStatus.Recovery, 22d, 21)]
        [DataRow("image33.png", "GAEZATORIX", SystemStatus.InvasionInProgress, 6d, 1)]
        [DataRow("image34.png", "RAIDAL", SystemStatus.InvasionInProgress, 0d, 1)]
        [DataRow("image35.png", "OBASSI OSAW", SystemStatus.InvasionInProgress, 32d, 1)]
        [DataRow("image36.png", "HIP 20419", SystemStatus.InvasionInProgress, 0d, 1)]
        [DataRow("image37.png", "70 TAURI", SystemStatus.AlertInProgressPopulated, 16d, 1)]
        [DataRow("image38.png", "HIP 7277", SystemStatus.InvasionInProgress, 0d, 1)]
        [DataRow("image39.png", "PATOLLU", SystemStatus.AlertInProgressPopulated, 2d, 1)]
        [DataRow("image40.png", "CHERNOBO", SystemStatus.InvasionInProgress, 2d, 1)]
        [DataRow("image41.png", "HIP 21380", SystemStatus.AlertInProgressPopulated, 10d, 1)]
        [DataRow("image42.png", "COL 285 SECTOR WY-F B12-3", SystemStatus.InvasionInProgress, 0d, 1)]
        [DataRow("image43.png", "IMEUT", SystemStatus.Recovery, 24d, 20)]
        [DataRow("image44.png", "LIU HUANG", SystemStatus.Recovery, 0d, 27)]
        [DataRow("image45.png", "HUILE", SystemStatus.AlertInProgressPopulated, 4d, 6)]
        [DataRow("image46.png", "HIP 7338", SystemStatus.InvasionInProgress, 12d, 11)]
        [DataRow("image47.png", "HIP 19198", SystemStatus.InvasionInProgress, 94d, 4)]
        [DataRow("image48.png", "HIP 7338", SystemStatus.InvasionInProgress, 22d, 9)]
        [DataRow("image49.png", "GLIESE 9035", SystemStatus.InvasionInProgress, 70d, 16)]
        [DataRow("image50.png", "HYADES SECTOR MY-I BS-0", SystemStatus.Unpopulated, 0d, 0)]
        [DataRow("image51.png", "LUNGUNI", SystemStatus.HumanControlled, 0d, 0)]
        [DataRow("image52.png", "CHINAS", SystemStatus.InvasionInProgress, 64d, 10)]
        [DataRow("image53.png", "LUGGERATES", SystemStatus.InvasionInProgress, 58d, 17)]
        [DataRow("image54.png", "HIP 7338", SystemStatus.InvasionPrevented, 100d, 1)]
        [DataRow("image55.png", "HIP 7338", SystemStatus.InvasionPrevented, 100d, 1)]
        [DataRow("image56.png", "HYADES SECTOR EQ-O B6-3", SystemStatus.ThargoidControlled, 8d, 5)]
        [DataRow("image57.png", "HYADES SECTOR EQ-O B6-3", SystemStatus.ThargoidControlled, 40d, 4)]
        [DataRow("image58.png", "HYADES SECTOR EQ-O B6-3", SystemStatus.ThargoidControlled, 42d, 4)]
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