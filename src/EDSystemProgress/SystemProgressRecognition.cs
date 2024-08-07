﻿using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Layout;

namespace EDSystemProgress
{
    public static partial class SystemProgressRecognition
    {
        private static List<ColorRange> InvasionProgressColors { get; } =
        [
            // Bar
            new ColorRange(25, 45, 25, 45, 25, 45),
            new ColorRange(30, 40, 10, 25, 20, 25),
            new ColorRange(25, 30, 15, 25, 10, 20),
            new ColorRange(30, 35, 25, 30, 20, 28),
            new ColorRange(35, 50, 35, 50, 35, 50),
            new ColorRange(40, 50, 15, 25, 35, 45),
            new ColorRange(45, 50, 20, 25, 30, 40),
            new ColorRange(45, 55, 45, 55, 45, 55),
            new ColorRange(50, 60, 15, 25, 50, 65),
            new ColorRange(50, 60, 40, 50, 30, 45),
            new ColorRange(50, 55, 30, 40, 20, 25),
            new ColorRange(50, 100, 0, 50, 75, 130),
            new ColorRange(60, 70, 55, 65, 50, 60),
            new ColorRange(70, 190, 0, 95, 98, 255),
            new ColorRange(80, 110, 5, 15, 135, 190),
            new ColorRange(100, 125, 5, 15, 200, 230),
            // Arrow
            new ColorRange(45, 65, 45, 65, 45, 65),
            new ColorRange(55, 75, 55, 75, 55, 75),
            new ColorRange(120, 140, 120, 140, 110, 140),
            new ColorRange(140, 170, 140, 170, 140, 170),
            new ColorRange(230, 255, 230, 255, 230, 255),
        ];

        private static List<ColorRange> InvasionRemainingColors { get; } =
        [
            new ColorRange(5, 15, 20, 50, 0, 10),
            new ColorRange(10, 20, 18, 30, 0, 5),
            new ColorRange(15, 30, 24, 50, 0, 15),
            new ColorRange(15, 25, 40, 60, 0, 15),
            new ColorRange(20, 25, 20, 25, 0, 0),
            new ColorRange(50, 70, 60, 100, 0, 35),
            new ColorRange(20, 60, 45, 100, 0, 35),
        ];

        private static List<ColorRange> AlertProgressColors { get; } =
        [
            new ColorRange(140, 160, 140, 160, 110, 160),
            new ColorRange(150, 170, 150, 180, 120, 170),
            new ColorRange(160, 180, 160, 180, 130, 180),
            new ColorRange(170, 190, 170, 190, 140, 190),
            new ColorRange(180, 200, 180, 200, 150, 200),
            new ColorRange(190, 210, 190, 210, 160, 210),
            new ColorRange(200, 220, 200, 220, 170, 220),
            new ColorRange(210, 230, 210, 230, 180, 230),
            new ColorRange(220, 240, 220, 240, 190, 240),
            new ColorRange(230, 255, 230, 255, 200, 255),

            new ColorRange(160, 190, 140, 170, 130, 160),
            new ColorRange(190, 205, 170, 195, 160, 240),
            new ColorRange(210, 240, 190, 230, 180, 210),
            new ColorRange(240, 255, 220, 235, 210, 225),
        ];

        private static List<ColorRange> AlertRemainingColors { get; } =
        [
            new ColorRange(100, 130, 30, 50, 0, 20),
            new ColorRange(120, 180, 45, 102, 0, 47),
            new ColorRange(220, 255, 100, 125, 0, 5),
        ];

        public static async Task<ExtractSystemProgressResult> ExtractSystemProgress(MemoryStream imageContent, ILogger log)
        {
            Guid fileId = Guid.NewGuid();
            log.LogDebug("Starting analysis of file ({fileSize}). Id: {Id}", imageContent.Length, fileId);

            await using MemoryStream invertedImage = new();
            {
                using Image<Rgba32> image = await Image.LoadAsync<Rgba32>(imageContent);
                image.Mutate(x => x.Invert());
                /*
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < image.Width; x++)
                        {
                            ref Rgba32 pixel = ref pixelRow[x];
                            int m = (int)Math.Floor((pixel.R + pixel.G + pixel.G) / 3f);
                            if (Math.Abs(pixel.R - m) <= 10 && Math.Abs(pixel.G - m) <= 10 && Math.Abs(pixel.B - m) <= 10)
                            {
                                if (m > 200)
                                {
                                    pixel.R = 255;
                                    pixel.G = 255;
                                    pixel.B = 255;
                                }
                                else if (m > 80)
                                {
                                    pixel.R -= 80;
                                    pixel.G -= 80;
                                    pixel.B -= 80;
                                }
                                else if (m <= 80)
                                {
                                    pixel.R = 0;
                                    pixel.G = 0;
                                    pixel.B = 0;
                                }
                            }
                        }
                    }
                });
                */
                /*
                PngEncoder encoder = new();
                encoder.CompressionLevel = PngCompressionLevel.Level0;
                encoder.ColorType = PngColorType.Grayscale;
                encoder.BitDepth = PngBitDepth.Bit2;
                */
#if DEBUG
                await image.SaveAsPngAsync($"test_result{fileId}_i.png");
#endif
                await image.SaveAsPngAsync(invertedImage);
            }
            byte[] file = invertedImage.ToArray();

            using Engine engine = new(@"./tessdata", Language.English, EngineMode.Default);
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz01234567890:.- ");

            log.LogInformation("TesseractEngine version {version}", engine.Version);

            using TesseractOCR.Pix.Image img = TesseractOCR.Pix.Image.LoadFromMemory(file);
            using Page page = engine.Process(img);

            int paragraphNumber = 0;
            ImageProcessingStep processingStep = ImageProcessingStep.WaitForTitle;

            string systemName = string.Empty;
            string remainingTime = string.Empty;
            SystemStatus systemStatus = SystemStatus.Unknown;
            decimal progress = 0;
            decimal remaining = 0;

            int progressBarUpperY = 0;
            int progressBarLowerY = 0;
            int leftSideBorder = 0;
            int? rightSideBorder = null;

            foreach (Block block in page.Layout)
            {
                foreach (Paragraph paragraph in block.Paragraphs)
                {
                    foreach (TextLine textLine in paragraph.TextLines)
                    {
                        string text = textLine.Text.Trim();
                        log.LogDebug("Line Text: {text}", text);

                        if (textLine.BoundingBox != null && (processingStep == ImageProcessingStep.AboveProgressBar || processingStep == ImageProcessingStep.NextStep))
                        {
                            Rect bounds = textLine.BoundingBox.Value;
                            int tempRightSideBorder = bounds.X2 + (int)Math.Ceiling(bounds.Width * 0.05);
                            if (rightSideBorder == null || tempRightSideBorder > rightSideBorder)
                            {
                                rightSideBorder = tempRightSideBorder;
                            }
                        }

                        if (string.IsNullOrEmpty(text))
                        {
                            continue;
                        }

                        switch (processingStep)
                        {
                            case ImageProcessingStep.WaitForTitle:
                                {
                                    if (text.Contains("THARGOID WAR"))
                                    {
                                        processingStep = ImageProcessingStep.SystemName;

                                        if (textLine.BoundingBox != null)
                                        {
                                            Rect bounds = textLine.BoundingBox.Value;
                                            leftSideBorder = bounds.X1 + (int)Math.Floor(bounds.Width * 0.04);
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
                                        if (text.EndsWith("."))
                                        {
                                            text = text[..^1].Trim();
                                        }
                                        systemName = text;
                                    }
                                    processingStep = ImageProcessingStep.NextStep;
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
                                        else if (text.Contains("currently populat") || text.Contains("populate"))
                                        {
                                            systemStatus = SystemStatus.HumanControlled;
                                            processingStep = ImageProcessingStep.Completed;
                                        }
                                        else if (text.Contains("no human") || text.Contains("population present"))
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
                                                if (systemStatus == SystemStatus.ThargoidControlledRegainedPopulated && text.Contains("BEGINS"))
                                                {
                                                    match = true;
                                                    remainingTime = text;
                                                }
                                                else
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
                                                    else if (text.Contains("CONTROL REGAINED"))
                                                    {
                                                        systemStatus = SystemStatus.ThargoidControlledRegainedPopulated;
                                                    }
                                                    else if (text.Contains("LOCATION"))
                                                    {
                                                        match = true;
                                                    }
                                                }
                                                break;
                                            }
                                        case SystemStatus.AlertInProgressPopulated:
                                        case SystemStatus.AlertInProgressUnpopulated:
                                            {
                                                if (text.Contains("HUMAN CONTROL MAINTAINED"))
                                                {
                                                    systemStatus = SystemStatus.AlertPreventedPopulated;
                                                    break;
                                                }
                                                else if (text.Contains("THARGOID CONTROL PREVENTED"))
                                                {
                                                    systemStatus = SystemStatus.AlertPreventedUnpopulated;
                                                    break;
                                                }
                                                if (text.Contains("DEFENSIVE WINDOW") ||
                                                    (text.Contains("DEFENSIVE") && text.Contains("ENDS")) ||
                                                    text.Contains("ENDS IN"))
                                                {
                                                    remainingTime = text;
                                                    match = true;
                                                }
                                                break;
                                            }
                                        case SystemStatus.AlertPreventedPopulated:
                                        case SystemStatus.AlertPreventedUnpopulated:
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
                                        if (textLine.BoundingBox != null)
                                        {
                                            Rect bounds = textLine.BoundingBox.Value;
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
                                        case SystemStatus.RecoveryComplete:
                                            {
                                                match = text.Contains("COMPLETION STATE");
                                                break;
                                            }
                                        case SystemStatus.AlertPreventedPopulated:
                                        case SystemStatus.AlertPreventedUnpopulated:
                                        case SystemStatus.ThargoidControlledRegainedUnpopulated:
                                        case SystemStatus.ThargoidControlledRegainedPopulated:
                                        case SystemStatus.InvasionPrevented:
                                            {
                                                match = text.Contains("VICTORY STATE") || text.Contains("VIGTORY STATE");
                                                break;
                                            }
                                        case SystemStatus.ThargoidControlled:
                                            {
                                                match = text.Contains("DESTROY") || text.Contains("SPIRE");
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
                                        if (textLine.BoundingBox != null)
                                        {
                                            Rect bounds = textLine.BoundingBox.Value;
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
                    }

                    paragraphNumber++;
                }
            }

            progressBarUpperY += 20;
            progressBarLowerY -= 10;

            log.LogInformation("{id}: processingStep: {processingStep} systemName: {systemName} systemStatus: {systemStatus} remainingTime: {remainingTime} progressBarUpperY: {progressBarUpperY} progressBarLowerY: {progressBarLowerY}", fileId, processingStep, systemName, systemStatus, remainingTime, progressBarUpperY, progressBarLowerY);

            bool success = processingStep == ImageProcessingStep.Completed && systemName != "INVALID";
            if (success && systemStatus != SystemStatus.HumanControlled && systemStatus != SystemStatus.Unpopulated)
            {
                List<ColorRange> progressColors = systemStatus switch
                {
                    SystemStatus.InvasionInProgress or
                    SystemStatus.InvasionPrevented => InvasionProgressColors,
                    SystemStatus.AlertPreventedPopulated or
                    SystemStatus.AlertInProgressPopulated => AlertProgressColors,
                    SystemStatus.ThargoidControlled or
                    SystemStatus.ThargoidControlledRegainedUnpopulated or
                    SystemStatus.ThargoidControlledRegainedPopulated or
                    SystemStatus.AlertInProgressUnpopulated or
                    SystemStatus.AlertPreventedUnpopulated => InvasionProgressColors,
                    SystemStatus.Recovery or SystemStatus.RecoveryComplete => AlertProgressColors,
                    _ => throw new NotImplementedException(),
                };
                List<ColorRange> remainingColors = systemStatus switch
                {
                    SystemStatus.InvasionInProgress or
                    SystemStatus.InvasionPrevented or
                    SystemStatus.AlertInProgressUnpopulated or
                    SystemStatus.AlertPreventedUnpopulated => InvasionRemainingColors,
                    SystemStatus.AlertPreventedPopulated or
                    SystemStatus.AlertInProgressPopulated => AlertRemainingColors,
                    SystemStatus.ThargoidControlled or
                    SystemStatus.ThargoidControlledRegainedUnpopulated or
                    SystemStatus.ThargoidControlledRegainedPopulated => InvasionRemainingColors,
                    SystemStatus.Recovery or
                    SystemStatus.RecoveryComplete => InvasionProgressColors,
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
                        if (rightSideBorder is null || rightSideBorder > pixelRow.Length)
                        {
                            rightSideBorder = pixelRow.Length;
                        }

                        bool colorRangeFound = false;
                        int pixelsSinceColorRangeNotFound = 0;
                        int progressBarRangeWidth = ((int)rightSideBorder - leftSideBorder);
                        for (int x = leftSideBorder; x < rightSideBorder; x++)
                        {
                            bool found = false;
                            // Get a reference to the pixel at position x
                            ref Rgba32 pixel = ref pixelRow[x];
                            if (y == progressBarUpperY || y == (progressBarLowerY - 1) || x == leftSideBorder || x == (rightSideBorder - 1))
                            {
                                bool isRightSideEnd = pixel.R == 0 && pixel.B == 0 && pixel.G == 0;
                                pixel.R = 0;
                                pixel.G = 255;
                                pixel.B = 255;
                                if (isRightSideEnd)
                                {
                                    rightSideBorder = x;
                                    break;
                                }
                                continue;
                            }
                            foreach (ColorRange progressColor in progressColors)
                            {
                                if (progressColor.IsInRange(pixel))
                                {
                                    pixel.R = 255;
                                    pixel.G = 0;
                                    pixel.B = 0;
                                    pixelsProgress++;
                                    found = true;
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
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                colorRangeFound = true;
                                pixelsSinceColorRangeNotFound = 0;
                            }
                            else
                            {
                                int treshold = (int)Math.Ceiling(progressBarRangeWidth * (colorRangeFound ? 0.05 : 0.1));
                                if (pixelsSinceColorRangeNotFound >= treshold)
                                {
                                    break;
                                }
                                pixelsSinceColorRangeNotFound++;
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
                        int d = int.Parse(daysStr);
                        if (d == 8)
                        {
                            d = 6;
                        }
                        days += d;
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
        private static Dictionary<string, string> SystemNameCorrections { get; } =
        [
            /*
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
            { "HIP 21891", "HIP 21991" },
            { "HIP 204892", "HIP 20492" },
            { "HIP 20893", "HIP 20899" },
            { "PEGASI SECTOR GE-N A8-0", "PEGASI SECTOR QE-N A8-0" },
            { "Bl DHORORA", "BI DHORORA" },
            */
        ];

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
        AlertPreventedPopulated,
        [EnumMember(Value = "Thargoid alert prevented")]
        AlertPreventedUnpopulated,
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
