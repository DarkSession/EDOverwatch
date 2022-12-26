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
            new ColorRange(80, 190, 15, 65, 98, 255),
            new ColorRange(235, 255, 235, 255, 235, 255),
        };

        private static List<ColorRange> InvasionRemainingColors { get; } = new()
        {
            new ColorRange(30, 67, 43, 100, 0, 32),
        };

        private static List<ColorRange> AlertProgressColors { get; } = new()
        {
            new ColorRange(140, 160, 140, 160, 140, 160),
            new ColorRange(150, 170, 170, 180, 150, 170),
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
            new ColorRange(120, 180, 50, 102, 0, 47),
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
                                    if (paragraphNumber >= 2)
                                    {
                                        if (text.Contains("THARGOID INFESTATION IN"))
                                        {
                                            remainingTime = text;
                                            systemStatus = SystemStatus.InvasionInProgress;
                                            processingStep = ImageProcessingStep.AboveProgressBar;
                                        }
                                        else if (text.Contains("THARGOID CONTROL PREVENTED"))
                                        {
                                            systemStatus = SystemStatus.InvasionPrevented;
                                            processingStep = ImageProcessingStep.AboveProgressBar;
                                        }
                                        else if (text.Contains("INCURSION THREAT IN"))
                                        {
                                            remainingTime = text;
                                            systemStatus = SystemStatus.AlertInProgress;
                                            processingStep = ImageProcessingStep.BelowProgressBar;
                                            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect bounds))
                                            {
                                                progressBarUpperY = bounds.Y2;
                                            }
                                        }
                                        else if (text.Contains("HUMAN CONTROL MAINTAINED"))
                                        {
                                            systemStatus = SystemStatus.AlertPrevented;
                                            processingStep = ImageProcessingStep.AboveProgressBar;
                                        }
                                        else if (text.Contains("RECAPTURE ATTEMPT"))
                                        {
                                            remainingTime = text;
                                            systemStatus = SystemStatus.ThargoidControlled;
                                            processingStep = ImageProcessingStep.BelowProgressBar;
                                            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect bounds))
                                            {
                                                progressBarUpperY = bounds.Y2;
                                            }
                                        }
                                        else if (text.Contains("RECOVERY COMPLETE IN"))
                                        {
                                            remainingTime = text;
                                            systemStatus = SystemStatus.Recovery;
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
                                            {
                                                match = text.Contains("PORTS REMAININ");
                                                break;
                                            }
                                        case SystemStatus.InvasionPrevented:
                                            {
                                                match = text.Contains("PORTS REMAININ");
                                                if (!match && text.Contains("BEGINS"))
                                                {
                                                    remainingTime = text;
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
                                                match = text.Contains("INACTIVE PORTS");
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
                                        case SystemStatus.AlertInProgress:
                                            {
                                                match = text.Contains("DELIVER SUPPLIES") || text.Contains("ELVER SUPPLES") || text.Contains("JLENVETCNE") || text.Contains("JENVENNE");
                                                break;
                                            }
                                        case SystemStatus.InvasionPrevented:
                                            {
                                                match = text.Contains("POST-THARGOID RECOVER") || text.Contains("POST THARGOID RECOVER");
                                                break;
                                            }
                                        case SystemStatus.AlertPrevented:
                                        case SystemStatus.RecoveryComplete:
                                            {
                                                match = text.Contains("HUMAN CONTROLLED");
                                                break;
                                            }
                                        case SystemStatus.ThargoidControlled:
                                            {
                                                match = text.Contains("DESTROY THARGOID");
                                                break;
                                            }
                                        case SystemStatus.Recovery:
                                            {
                                                match = text.Contains("DELIVER") || text.Contains("ELVER SUPPLES");
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

            log.LogInformation("{id}: processingStep: {processingStep} systemName: {systemName} systemStatus: {} remainingTime: {remainingTime} progressBarUpperY: {progressBarUpperY} progressBarLowerY: {progressBarLowerY}", fileId, processingStep, systemName, systemStatus, remainingTime, progressBarUpperY, progressBarLowerY);

            bool success = processingStep == ImageProcessingStep.Completed && systemName != "INVALID";
            if (success)
            {
                List<ColorRange> progressColors = systemStatus switch
                {
                    SystemStatus.InvasionInProgress or SystemStatus.InvasionPrevented => InvasionProgressColors,
                    SystemStatus.AlertPrevented or SystemStatus.AlertInProgress => AlertProgressColors,
                    SystemStatus.ThargoidControlled => InvasionProgressColors,
                    SystemStatus.Recovery or SystemStatus.RecoveryComplete => AlertProgressColors,
                    _ => throw new NotImplementedException(),
                };
                List<ColorRange> remainingColors = systemStatus switch
                {
                    SystemStatus.InvasionInProgress or SystemStatus.InvasionPrevented => InvasionRemainingColors,
                    SystemStatus.AlertPrevented or SystemStatus.AlertInProgress => AlertRemainingColors,
                    SystemStatus.ThargoidControlled => InvasionRemainingColors,
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
                        for (int x = 0; x < pixelRow.Length; x++)
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
                        case SystemStatus.AlertInProgress:
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
        AlertInProgress,
        [EnumMember(Value = "Thargoid alert prevented")]
        AlertPrevented,
        [EnumMember(Value = "Thargoid controlled")]
        ThargoidControlled,
        [EnumMember(Value = "Recovery")]
        Recovery,
        [EnumMember(Value = "Recovery completed")]
        RecoveryComplete,
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
