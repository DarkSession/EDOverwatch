using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Tesseract;

namespace EDSystemProgress
{
    public static partial class SystemProgressRecognition
    {
        private static List<ColorRange> InvasionProgressColors { get; } = new()
        {
            // Bar
            new ColorRange(25, 45, 25, 45, 25, 45),
            new ColorRange(30, 40, 10, 25, 20, 25),
            new ColorRange(35, 50, 35, 50, 35, 50),
            new ColorRange(45, 50, 20, 25, 30, 40),
            new ColorRange(45, 55, 45, 55, 45, 55),
            new ColorRange(50, 100, 0, 50, 75, 130),
            new ColorRange(70, 190, 0, 95, 98, 255),
            new ColorRange(80, 110, 5, 15, 135, 190),
            new ColorRange(100, 125, 5, 15, 200, 230),
            // Arrow
            new ColorRange(45, 65, 45, 65, 45, 65),
            new ColorRange(55, 75, 55, 75, 55, 75),
            new ColorRange(120, 140, 120, 140, 120, 140),
            new ColorRange(140, 160, 140, 160, 140, 160),
            new ColorRange(230, 255, 230, 255, 230, 255),
        };

        private static List<ColorRange> InvasionRemainingColors { get; } = new()
        {
            new ColorRange(5, 15, 20, 50, 0, 10),
            new ColorRange(10, 20, 18, 30, 0, 5),
            new ColorRange(15, 30, 24, 50, 0, 15),
            new ColorRange(15, 25, 40, 60, 0, 15),
            new ColorRange(20, 25, 20, 25, 0, 0),
            new ColorRange(50, 70, 60, 100, 0, 35),
            new ColorRange(20, 60, 40, 100, 0, 35),
        };

        private static List<ColorRange> AlertProgressColors { get; } = new()
        {
            new ColorRange(140, 160, 140, 160, 140, 160),
            new ColorRange(150, 170, 150, 180, 150, 170),
            new ColorRange(160, 180, 160, 180, 160, 180),
            new ColorRange(170, 190, 170, 190, 170, 190),
            new ColorRange(180, 200, 180, 200, 180, 200),
            new ColorRange(190, 210, 190, 210, 190, 210),
            new ColorRange(200, 220, 200, 220, 200, 220),
            new ColorRange(210, 230, 210, 230, 210, 230),
            new ColorRange(220, 240, 220, 240, 220, 240),
            new ColorRange(230, 255, 230, 255, 230, 255),
        };

        private static List<ColorRange> AlertRemainingColors { get; } = new()
        {
            new ColorRange(120, 180, 45, 102, 0, 47),
            new ColorRange(100, 130, 30, 50, 0, 20),
        };

        public static async Task<ExtractSystemProgressResult> ExtractSystemProgress(MemoryStream imageContent, ILogger log)
        {
            Guid fileId = Guid.NewGuid();
            log.LogDebug("Starting analysis of file ({fileSize}). Id: {Id}", imageContent.Length, fileId);

            await using MemoryStream invertedImage = new();
            {
                using Image image = await Image.LoadAsync(imageContent);
                image.Mutate(x => x.Invert());
                await image.SaveAsPngAsync(invertedImage);
#if DEBUG
                await image.SaveAsPngAsync($"test_result{fileId}_i.png");
#endif
            }
            byte[] file = invertedImage.ToArray();

            using TesseractEngine engine = new("tessdata", "eng", EngineMode.Default, "config");
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz01234567890:.- ");

            log.LogInformation("TesseractEngine version {version}", engine.Version);

            using Pix img = Pix.LoadFromMemory(file);
            using Page page = engine.Process(img);

            using ResultIterator iter = page.GetIterator();

            iter.Begin();

            int paragraphNumber = 0;
            int paragraphLineNumber = 0;
            ImageProcessingStep processingStep = ImageProcessingStep.WaitForTitle;

            string systemName = string.Empty;
            string remainingTime = string.Empty;
            SystemStatus systemStatus = SystemStatus.Unknown;
            decimal progress = 0;
            decimal remaining = 0;

            int progressBarUpperY = 0;
            int progressBarLowerY = 0;
            int leftSideBorder = 0;

            do
            {
                do
                {
                    do
                    {
                        if (iter.IsAtBeginningOf(PageIteratorLevel.Block))
                        {
                            log.LogDebug("Start of new block");
                        }
                        string text = iter.GetText(PageIteratorLevel.TextLine).Trim();

                        log.LogDebug("Text: {text}", text);

                        switch (processingStep)
                        {
                            case ImageProcessingStep.WaitForTitle:
                                {
                                    if (text.Contains("THARGOID WAR INFORMATION"))
                                    {
                                        processingStep = ImageProcessingStep.SystemName;

                                        if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect bounds))
                                        {
                                            leftSideBorder = bounds.X1;
                                        }
                                    }
                                    break;
                                }
                            case ImageProcessingStep.SystemName:
                                {
                                    if (text.Contains("THARGOID WAR") || text.Contains("DISTANCE"))
                                    {
                                        systemName = "INVALID";
                                    }
                                    else
                                    {
                                        systemName = text;
                                    }
                                    processingStep = ImageProcessingStep.NextStep;
#if DEBUG
                                    do
                                    {
                                        do
                                        {
                                            using ChoiceIterator choiceIter = iter.GetChoiceIterator();
                                            float symbolConfidence = iter.GetConfidence(PageIteratorLevel.Symbol) / 100;
                                            if (choiceIter != null)
                                            {
                                                log.LogDebug("<symbol text=\"{0}\" confidence=\"{1:P}\">", iter.GetText(PageIteratorLevel.Symbol), symbolConfidence);
                                                log.LogDebug("<choices>");
                                                do
                                                {
                                                    float choiceConfidence = choiceIter.GetConfidence() / 100;
                                                    log.LogDebug("<choice text=\"{0}\" confidence\"{1:P}\"/>", choiceIter.GetText(), choiceConfidence);

                                                } while (choiceIter.Next());
                                                log.LogDebug("</choices>");
                                                log.LogDebug("</symbol>");
                                            }
                                            else
                                            {
                                                log.LogDebug("<symbol text=\"{0}\" confidence=\"{1:P}\"/>", iter.GetText(PageIteratorLevel.Symbol), symbolConfidence);
                                            }
                                        } while (iter.Next(PageIteratorLevel.Word, PageIteratorLevel.Symbol));
                                    } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
#endif
                                    break;
                                }
                            case ImageProcessingStep.NextStep:
                                {
                                    if (paragraphNumber >= 1)
                                    {
                                        if (text.Contains("Thargoids are attempting"))
                                        {
                                            systemStatus = SystemStatus.InvasionInProgress;
                                            processingStep = ImageProcessingStep.AboveProgressBar;
                                        }
                                        else if (text.Contains("have established"))
                                        {
                                            systemStatus = SystemStatus.ThargoidControlled;
                                            processingStep = ImageProcessingStep.AboveProgressBar;
                                        }
                                        else if (text.Contains("Thargoid vessels") || text.Contains("are present in"))
                                        {
                                            systemStatus = SystemStatus.AlertInProgressPopulated;
                                            processingStep = ImageProcessingStep.AboveProgressBar;
                                        }
                                        else if (text.Contains("system is currently populated"))
                                        {
                                            systemStatus = SystemStatus.HumanControlled;
                                            processingStep = ImageProcessingStep.Completed;
                                        }
                                        else if (text.Contains("no human population present"))
                                        {
                                            systemStatus = SystemStatus.Unpopulated;
                                            processingStep = ImageProcessingStep.Completed;
                                        }
                                        else if (text.Contains("RECOVERY COMPLETE IN"))
                                        {
                                            systemStatus = SystemStatus.Recovery;
                                            remainingTime = text;
                                            processingStep = ImageProcessingStep.AboveProgressBar;
                                        }
                                        else if (text.Contains("RECOVERY WORK COMPLETE"))
                                        {
                                            systemStatus = SystemStatus.RecoveryComplete;
                                            processingStep = ImageProcessingStep.AboveProgressBar;
                                        }
                                    }
                                    break;
                                }
                            case ImageProcessingStep.AboveProgressBar:
                                {
                                    bool match = false;
                                    switch (systemStatus)
                                    {
                                        case SystemStatus.InvasionInProgress:
                                        case SystemStatus.InvasionPrevented:
                                            {
                                                if (text.Contains("DEFENSIVE WINDOW") || (text.Contains("DEFENSIVE") && text.Contains("ENDS")))
                                                {
                                                    systemStatus = SystemStatus.InvasionInProgress;
                                                    match = true;
                                                }
                                                else if (text.Contains("PREVENTED"))
                                                {
                                                    systemStatus = SystemStatus.InvasionPrevented;
                                                }
                                                else if (systemStatus == SystemStatus.InvasionPrevented && text.Contains("BEGINS"))
                                                {
                                                    remainingTime = text;
                                                    match = true;
                                                }
                                                break;
                                            }
                                        case SystemStatus.RecoveryComplete:
                                            {
                                                match = text.Contains("ACTIVE IN");
                                                if (match)
                                                {
                                                    remainingTime = text;
                                                }
                                                break;
                                            }
                                        case SystemStatus.ThargoidControlledRegainedPopulated:
                                        case SystemStatus.ThargoidControlledRegainedUnpopulated:
                                        case SystemStatus.ThargoidControlled:
                                            {
                                                match = text.Contains("COUNTER-ATTACK");
                                                if (match)
                                                {
                                                    remainingTime = text;
                                                }
                                                else if (text.Contains("WILL RETREAT"))
                                                {
                                                    match = true;
                                                    systemStatus = SystemStatus.ThargoidControlledRegainedUnpopulated;
                                                    remainingTime = text;
                                                }
                                                break;
                                            }
                                        case SystemStatus.AlertInProgressPopulated:
                                        case SystemStatus.AlertInProgressUnpopulated:
                                            {
                                                if (text.Contains("HUMAN CONTROL MAINTAINED"))
                                                {
                                                    systemStatus = SystemStatus.AlertPrevented;
                                                    break;
                                                }
                                                if (text.Contains("DEFENSIVE WINDOW") || (text.Contains("DEFENSIVE") && text.Contains("ENDS")))
                                                {
                                                    remainingTime = text;
                                                    match = true;
                                                }
                                                break;
                                            }
                                        case SystemStatus.AlertPrevented:
                                            {
                                                match = text.Contains("WITHDRAWAL");
                                                remainingTime = text;
                                                break;
                                            }
                                        case SystemStatus.Recovery:
                                            {
                                                match = text.Contains("INACTIVE PORTS") || text.Contains("PORTS REMAINING");
                                                break;
                                            }
                                    }
                                    if (match)
                                    {
                                        if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect bounds))
                                        {
                                            progressBarUpperY = bounds.Y2;
                                        }
                                        processingStep = ImageProcessingStep.BelowProgressBar;
                                    }
                                    break;
                                }
                            case ImageProcessingStep.BelowProgressBar:
                                {
                                    bool match = false;
                                    switch (systemStatus)
                                    {
                                        case SystemStatus.InvasionInProgress:
                                            {
                                                match = text.Contains("DELIVER SUPPLIES") ||
                                                        text.Contains("ELVER SUPPLES") ||
                                                        text.Contains("JLENVETCNE") ||
                                                        text.Contains("JENVENNE");
                                                break;
                                            }
                                        case SystemStatus.AlertInProgressPopulated:
                                        case SystemStatus.AlertInProgressUnpopulated:
                                            {
                                                match = text.Contains("DELIVER SUPPLIES") ||
                                                        text.Contains("ELVER SUPPLES") ||
                                                        text.Contains("JLENVETCNE") ||
                                                        text.Contains("JENVENNE");
                                                if (match)
                                                {
                                                    systemStatus = SystemStatus.AlertInProgressPopulated;
                                                }
                                                else if (text.Contains("DESTROY"))
                                                {
                                                    match = true;
                                                    systemStatus = SystemStatus.AlertInProgressUnpopulated;
                                                }
                                                break;
                                            }
                                        case SystemStatus.InvasionPrevented:
                                            {
                                                match = text.Contains("POST-THARGOID RECOVER") || text.Contains("POST THARGOID RECOVER");
                                                break;
                                            }
                                        case SystemStatus.RecoveryComplete:
                                            {
                                                match = text.Contains("COMPLETION STATE");
                                                break;
                                            }
                                        case SystemStatus.AlertPrevented:
                                        case SystemStatus.ThargoidControlledRegainedUnpopulated:
                                        case SystemStatus.ThargoidControlledRegainedPopulated:
                                            {
                                                match = text.Contains("VICTORY STATE");
                                                break;
                                            }
                                        case SystemStatus.ThargoidControlled:
                                            {
                                                match = text.Contains("DESTROY");
                                                break;
                                            }
                                        case SystemStatus.Recovery:
                                            {
                                                match = text.Contains("REBOOT");
                                                break;
                                            }
                                    }
                                    if (match)
                                    {
                                        if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect bounds))
                                        {
                                            progressBarLowerY = bounds.Y1;
                                        }
                                        processingStep = ImageProcessingStep.Completed;
                                    }
                                    break;
                                }
                            case ImageProcessingStep.Completed:
                                {
                                    switch (systemStatus)
                                    {
                                        case SystemStatus.InvasionInProgress:
                                            {
                                                if (text.Contains("THARGOID CONTROL IN"))
                                                {
                                                    remainingTime = text;
                                                }
                                                break;
                                            }
                                        case SystemStatus.ThargoidControlledRegainedPopulated:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }
                        }
                        if (iter.IsAtFinalOf(PageIteratorLevel.Para, PageIteratorLevel.TextLine))
                        {
                            log.LogDebug("End of para");
                        }
                        paragraphLineNumber++;
                    } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                    paragraphNumber++;
                    paragraphLineNumber = 0;
                } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
            } while (iter.Next(PageIteratorLevel.Block));

            progressBarUpperY += 20;
            progressBarLowerY -= 10;

            log.LogInformation("{id}: processingStep: {processingStep} systemName: {systemName} systemStatus: {systemStatus} remainingTime: {remainingTime} progressBarUpperY: {progressBarUpperY} progressBarLowerY: {progressBarLowerY}", fileId, processingStep, systemName, systemStatus, remainingTime, progressBarUpperY, progressBarLowerY);

            bool success = processingStep == ImageProcessingStep.Completed && systemName != "INVALID";
            if (success && systemStatus != SystemStatus.HumanControlled && systemStatus != SystemStatus.Unpopulated)
            {
                List<ColorRange> progressColors = systemStatus switch
                {
                    SystemStatus.InvasionInProgress or SystemStatus.InvasionPrevented => InvasionProgressColors,
                    SystemStatus.AlertPrevented or SystemStatus.AlertInProgressPopulated or SystemStatus.AlertInProgressUnpopulated => AlertProgressColors,
                    SystemStatus.ThargoidControlled or SystemStatus.ThargoidControlledRegainedUnpopulated or SystemStatus.ThargoidControlledRegainedPopulated => InvasionProgressColors,
                    SystemStatus.Recovery or SystemStatus.RecoveryComplete => AlertProgressColors,
                    _ => throw new NotImplementedException(),
                };
                List<ColorRange> remainingColors = systemStatus switch
                {
                    SystemStatus.InvasionInProgress or SystemStatus.InvasionPrevented or SystemStatus.AlertInProgressUnpopulated => InvasionRemainingColors,
                    SystemStatus.AlertPrevented or SystemStatus.AlertInProgressPopulated => AlertRemainingColors,
                    SystemStatus.ThargoidControlled or SystemStatus.ThargoidControlledRegainedUnpopulated or SystemStatus.ThargoidControlledRegainedPopulated => InvasionRemainingColors,
                    SystemStatus.Recovery or SystemStatus.RecoveryComplete => InvasionProgressColors,
                    _ => throw new NotImplementedException(),
                };

                int pixelsProgress = 0;
                int pixelsRemaining = 0;

                imageContent.Position = 0;
                using Image<Rgba32> image = await Image.LoadAsync<Rgba32>(imageContent);
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = progressBarUpperY; y < progressBarLowerY; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        // pixelRow.Length has the same value as accessor.Width,
                        // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                        for (int x = leftSideBorder; x < pixelRow.Length; x++)
                        {
                            // Get a reference to the pixel at position x
                            ref Rgba32 pixel = ref pixelRow[x];
                            foreach (ColorRange progressColor in progressColors)
                            {
                                if (progressColor.IsInRange(pixel))
                                {
                                    pixel.R = 255;
                                    pixel.G = 0;
                                    pixel.B = 0;
                                    pixelsProgress++;
                                    break;
                                }
                            }
                            foreach (ColorRange remainingColor in remainingColors)
                            {
                                if (remainingColor.IsInRange(pixel))
                                {
                                    pixel.R = 0;
                                    pixel.G = 255;
                                    pixel.B = 0;
                                    pixelsRemaining++;
                                    break;
                                }
                            }
                        }
                    }
                });
#if DEBUG
                await image.SaveAsPngAsync($"test_result{fileId}.png");
#endif
                int progressRemainingPixels = pixelsProgress + pixelsRemaining;
                if (progressRemainingPixels > 1000)
                {
                    progress = Math.Round(pixelsProgress / (decimal)progressRemainingPixels * 50, 0) * 2;
                    remaining = Math.Round(pixelsRemaining / (decimal)progressRemainingPixels * 50, 0) * 2;

                    switch (systemStatus)
                    {
                        case SystemStatus.InvasionInProgress:
                        case SystemStatus.AlertInProgressPopulated:
                        case SystemStatus.AlertInProgressUnpopulated:
                        case SystemStatus.ThargoidControlled:
                        case SystemStatus.Recovery:
                            {
                                if (progress >= 2)
                                {
                                    progress -= 2;
                                    remaining += 2;
                                }
                                break;
                            }
                    }

                    log.LogDebug("progress: {progress}% ({pixelsProgress}/{progressRemainingPixels})", progress, pixelsProgress, progressRemainingPixels);
                    log.LogDebug("remaining: {remaining}% ({pixelsRemaining}/{progressRemainingPixels})", remaining, pixelsRemaining, progressRemainingPixels);
                }
                else
                {
                    success = false;
                }
            }

            if (!success)
            {
                log.LogWarning("Failed!");
            }

            if (SystemNameCorrections.TryGetValue(systemName, out string? newSystemName))
            {
                systemName = newSystemName;
            }
            TimeSpan remTime = TimeSpan.Zero;
            if (!string.IsNullOrEmpty(remainingTime))
            {
                Regex r = DurationRegex();
                Match m = r.Match(remainingTime);
                if (m.Success)
                {
                    string? weeksStr = m.Groups[2]?.Value;
                    string? daysStr = m.Groups[4]?.Value;

                    int days = 0;
                    if (!string.IsNullOrEmpty(weeksStr))
                    {
                        days += int.Parse(weeksStr) * 7;
                    }
                    if (!string.IsNullOrEmpty(daysStr))
                    {
                        days += int.Parse(daysStr);
                    }
                    remTime = new(days, 0, 0, 0);
                }
            }

            return new ExtractSystemProgressResult(
                success,
                systemName,
                systemStatus,
                progress,
                remaining,
                remTime);
        }

        // Manually fixing some stuff until a better solution is found...
        private static Dictionary<string, string> SystemNameCorrections { get; } = new()
        {
            { "ARIETIS SECTOR AG-P B5-0", "ARIETIS SECTOR AQ-P B5-0" },
            { "OBASSI 0SAW", "OBASSI OSAW" },
            { "HIP 20880", "HIP 20890" },
            { "HIP 20816", "HIP 20916" },
            { "HIP 208186", "HIP 20916" },
            { "O0BAMUMBO", "OBAMUMBO" },
            { "LAHUA n", "LAHUA" },
            { "GLIESE 8035", "GLIESE 9035" },
            { "HIP 20418", "HIP 20419" },
            { "HIP 18138", "HIP 19198" },
            { "HYADES SECTOR EG-O B6-3", "HYADES SECTOR EQ-O B6-3" },
            { "HIP 208389", "HIP 20899" },
            { "HIP 22436", "HIP 22496" },
            { "TRIANGULI SECTOR EG-Y B1", "TRIANGULI SECTOR EQ-Y B1" },
        };

        [GeneratedRegex("IN ((\\d{0,1})W|)\\s{0,}((\\d{0,1})D|)$", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex DurationRegex();
    }

    public class ColorRange
    {
        public int RLow { get; }
        public int RHigh { get; }
        public int GLow { get; }
        public int GHigh { get; }
        public int BLow { get; }
        public int BHigh { get; }

        public ColorRange(int rLow, int rHigh, int gLow, int gHigh, int bLow, int bHigh)
        {
            RLow = rLow;
            RHigh = rHigh;
            GLow = gLow;
            GHigh = gHigh;
            BLow = bLow;
            BHigh = bHigh;
        }

        public bool IsInRange(Rgba32 pixelColor)
        {
            return (
                pixelColor.R >= RLow &&
                pixelColor.R <= RHigh &&
                pixelColor.G >= GLow &&
                pixelColor.G <= GHigh &&
                pixelColor.B >= BLow &&
                pixelColor.B <= BHigh);
        }
    }

    internal enum ImageProcessingStep
    {
        WaitForTitle,
        SystemName,
        NextStep,
        AboveProgressBar,
        BelowProgressBar,
        Completed,
    }

    public enum SystemStatus
    {
        Unknown,
        [EnumMember(Value = "Thargoid invasion")]
        InvasionInProgress,
        [EnumMember(Value = "Thargoid invasion prevented")]
        InvasionPrevented,
        [EnumMember(Value = "Thargoid alert")]
        AlertInProgressPopulated,
        [EnumMember(Value = "Thargoid alert")]
        AlertInProgressUnpopulated,
        [EnumMember(Value = "Thargoid alert prevented")]
        AlertPrevented,
        [EnumMember(Value = "Thargoid controlled")]
        ThargoidControlled,
        [EnumMember(Value = "Recovery")]
        Recovery,
        [EnumMember(Value = "Recovery completed")]
        RecoveryComplete,
        [EnumMember(Value = "Human controlled")]
        HumanControlled,
        Unpopulated,
        [EnumMember(Value = "Thargoid controlled, regained")]
        ThargoidControlledRegainedUnpopulated,
        [EnumMember(Value = "Thargoid controlled, regained")]
        ThargoidControlledRegainedPopulated,
    }

    public readonly struct ExtractSystemProgressResult
    {
        public bool Success { get; }
        public string SystemName { get; }
        public SystemStatus SystemStatus { get; }
        public decimal Progress { get; }
        public decimal Remaining { get; }
        public TimeSpan RemainingTime { get; }

        public ExtractSystemProgressResult(bool success, string systemName, SystemStatus systemStatus, decimal progress, decimal remaining, TimeSpan remainingTime)
        {
            Success = success;
            SystemName = systemName;
            SystemStatus = systemStatus;
            Progress = progress;
            Remaining = remaining;
            RemainingTime = remainingTime;
        }
    }
}
