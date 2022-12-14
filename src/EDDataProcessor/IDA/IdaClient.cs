using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Text.RegularExpressions;
using static Google.Apis.Sheets.v4.SpreadsheetsResource;

namespace EDDataProcessor.IDA
{
    internal partial class IdaClient
    {
        private EdDbContext DbContext { get; }
        private IConfiguration Configuration { get; }
        public IdaClient(IConfiguration configuration, EdDbContext dbContext)
        {
            Configuration = configuration;
            DbContext = dbContext;
        }

        public async IAsyncEnumerable<long> UpdateData()
        {
            using SheetsService sheetsService = new(new BaseClientService.Initializer()
            {
                ApplicationName = Configuration.GetValue<string>("SheetsService:ApplicationName") ?? throw new Exception("SheetsService:ApplicationName is not configured"),
                ApiKey = Configuration.GetValue<string>("SheetsService:ApiKey") ?? throw new Exception("SheetsService:ApiKey is not configured"),
            });

            string speadsheetId = Configuration.GetValue<string>("IDA:SpeadsheetId") ?? throw new Exception("IDA:SpeadsheetId is not configured");

            GetRequest request = sheetsService.Spreadsheets.Get(speadsheetId);
            Spreadsheet response = await request.ExecuteAsync();

            Regex systemNameRegex1 = SystemNameRegex1();
            Regex systemNameRegex2 = SystemNameRegex2();

            foreach (Sheet sheet in response.Sheets)
            {
                Match match = systemNameRegex1.Match(sheet.Properties.Title);
                if (!match.Success)
                {
                    match = systemNameRegex2.Match(sheet.Properties.Title);
                }
                if (match.Success)
                {
                    StarSystem? starSystem = await DbContext.StarSystems.FirstOrDefaultAsync(s => s.Name == match.Groups[1].Value && s.WarRelevantSystem);
                    if (starSystem != null)
                    {
                        ValuesResource.GetRequest requestCells = sheetsService.Spreadsheets.Values.Get(speadsheetId, $"{sheet.Properties.Title}!A1:C2");
                        ValueRange valueRange = await requestCells.ExecuteAsync();
                        {
                            string? result = valueRange.Values.ElementAt(1).ElementAt(2) as string;
                            if (!string.IsNullOrEmpty(result) && long.TryParse(result.Replace(",", string.Empty), out long newTotalHauled))
                            {
                                long currentTotalHauled = await DbContext.WarEfforts
                                    .Where(w =>
                                            w.StarSystem == starSystem &&
                                            w.Type == WarEffortType.SupplyDelivery &&
                                            w.Side == WarEffortSide.Humans &&
                                            w.Source == WarEffortSource.IDA)
                                    .SumAsync(w => w.Amount);
                                if (currentTotalHauled != newTotalHauled)
                                {
                                    DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
                                    WarEffort? supplyDeliveries = await DbContext.WarEfforts
                                        .FirstOrDefaultAsync(w =>
                                                w.StarSystem == starSystem &&
                                                w.Type == WarEffortType.SupplyDelivery &&
                                                w.Side == WarEffortSide.Humans &&
                                                w.Source == WarEffortSource.IDA &&
                                                w.Date == today);
                                    if (supplyDeliveries != null)
                                    {
                                        supplyDeliveries.Amount += (newTotalHauled - currentTotalHauled);
                                    }
                                    else
                                    {
                                        supplyDeliveries = new(0, WarEffortType.SupplyDelivery, today, (newTotalHauled - currentTotalHauled), WarEffortSide.Humans, WarEffortSource.IDA)
                                        {
                                            StarSystem = starSystem,
                                        };
                                        DbContext.WarEfforts.Add(supplyDeliveries);
                                    }
                                    await DbContext.SaveChangesAsync();
                                    yield return starSystem.SystemAddress;
                                }
                            }
                        }
                        {
                            string? result = valueRange.Values.ElementAt(1).ElementAt(1) as string;
                            if (!string.IsNullOrEmpty(result) && long.TryParse(result.Replace(",", string.Empty), out long newTotalMissions))
                            {
                                long currentTotalMissions = await DbContext.WarEfforts
                                    .Where(w =>
                                            w.StarSystem == starSystem &&
                                            w.Type == WarEffortType.MissionCompletionDelivery &&
                                            w.Side == WarEffortSide.Humans &&
                                            w.Source == WarEffortSource.IDA)
                                    .SumAsync(w => w.Amount);
                                if (currentTotalMissions != newTotalMissions)
                                {
                                    DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
                                    WarEffort? supplyDeliveries = await DbContext.WarEfforts
                                        .FirstOrDefaultAsync(w =>
                                                w.StarSystem == starSystem &&
                                                w.Type == WarEffortType.MissionCompletionDelivery &&
                                                w.Side == WarEffortSide.Humans &&
                                                w.Source == WarEffortSource.IDA &&
                                                w.Date == today);
                                    if (supplyDeliveries != null)
                                    {
                                        supplyDeliveries.Amount += (newTotalMissions - currentTotalMissions);
                                    }
                                    else
                                    {
                                        supplyDeliveries = new(0, WarEffortType.MissionCompletionDelivery, today, (newTotalMissions - currentTotalMissions), WarEffortSide.Humans, WarEffortSource.IDA)
                                        {
                                            StarSystem = starSystem,
                                        };
                                        DbContext.WarEfforts.Add(supplyDeliveries);
                                    }
                                    await DbContext.SaveChangesAsync();
                                    yield return starSystem.SystemAddress;
                                }
                            }
                        }
                    }
                }
            }
        }

        [GeneratedRegex("^Thargoid War - (.*?)$", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex SystemNameRegex1();
        [GeneratedRegex("^(.*?) Recovery$", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex SystemNameRegex2();
    }
}
