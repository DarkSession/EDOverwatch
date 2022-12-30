using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System.Text.RegularExpressions;

namespace EDDataProcessor.Inara
{
    internal partial class InaraClient : IDisposable
    {
        private string BaseUrl { get; }
        private IBrowsingContext IBrowsingContext { get; }
        private Regex PathRegex { get; } = PathRegexGen();
        private DateTimeOffset LastRequest { get; set; } = DateTimeOffset.Now;
        private ILogger Log { get; }

        public InaraClient(Microsoft.Extensions.Configuration.IConfiguration configuration, ILogger<InaraClient> log)
        {
            AngleSharp.IConfiguration config = Configuration.Default;
            IBrowsingContext = BrowsingContext.New(config);
            BaseUrl = configuration.GetValue<string>("Inara:BaseUrl") ?? throw new Exception("Inara:BaseUrl is not configured");
            Log = log;
        }

        public void Dispose()
        {
            IBrowsingContext.Dispose();
        }

        public async Task<int?> GetSystemId(string systemName)
        {
            using IDocument? document = await Get("starsystem/?search=" + Uri.EscapeDataString(systemName));
            if (document?.GetElementsByClassName("quickmenu")
                 .Where(d => d is IHtmlAnchorElement h && h.TextContent == "Overview")
                 .FirstOrDefault() is IHtmlAnchorElement overviewLink)
            {
                return LinkExtractStarSystemId(overviewLink);
            }
            return null;
        }

        private int? LinkExtractStarSystemId(IHtmlAnchorElement link)
        {
            Match pathMatch = PathRegex.Match(link.PathName);
            if (pathMatch.Success && int.TryParse(pathMatch.Groups[1].Value, out int result))
            {
                return result;
            }
            return null;
        }

        public async IAsyncEnumerable<(int systemId, string systemName)> GetThargoidConflictList()
        {
            using IDocument? document = await Get("thargoidwar-conflicts/");
            IHtmlTableElement? table = document?.GetElementsByTagName("table").FirstOrDefault() as IHtmlTableElement;
            if (table?.GetElementsByTagName("tbody").FirstOrDefault() is IHtmlTableSectionElement tbody)
            {
                foreach (IHtmlTableRowElement row in tbody.Rows)
                {
                    IHtmlTableDataCellElement? cell = row.Cells.FirstOrDefault() as IHtmlTableDataCellElement;
                    if (cell?.GetElementsByTagName("a").FirstOrDefault() is IHtmlAnchorElement link)
                    {
                        int? systemId = LinkExtractStarSystemId(link);
                        if (systemId is int s)
                        {
                            yield return (s, link.Text);
                        }
                    }
                }
            }
        }

        public async Task<ConflictDetails> GetConflictDetails(int systemId)
        {
            ConflictDetails result = new();
            using IDocument? document = await Get($"thargoidwar-conflict/{systemId}/");
            if (document?.GetElementsByTagName("div")?.Where(e => e is IHtmlDivElement i && i.TextContent == "Thargoids killed").Skip(1).FirstOrDefault() is IHtmlDivElement thargoidsKilledCell &&
                thargoidsKilledCell.NextSibling is IHtmlDivElement thargoidsKilledValueCell &&
                int.TryParse(thargoidsKilledValueCell.TextContent.Replace(",", string.Empty), out int thargoidsKilledTotal))
            {
                result.TotalThargoidsKilled = thargoidsKilledTotal;
            }
            if (document?.GetElementsByTagName("div")?.Where(e => e is IHtmlDivElement i && i.TextContent == "Rescues performed").Skip(1).FirstOrDefault() is IHtmlDivElement rescuesPerformedCell &&
                rescuesPerformedCell.NextSibling is IHtmlDivElement rescuesPerformedValueCell &&
                int.TryParse(rescuesPerformedValueCell.TextContent.Replace(",", string.Empty), out int rescuesPerfomedTotal))
            {
                result.TotalRescuesPerformed = rescuesPerfomedTotal;
            }
            if (document?.GetElementsByTagName("div")?.Where(e => e is IHtmlDivElement i && i.TextContent == "Supplies delivered").Skip(1).FirstOrDefault() is IHtmlDivElement suppliesDeliveredCell &&
                suppliesDeliveredCell.NextSibling is IHtmlDivElement suppliesDeliveredValueCell &&
                int.TryParse(suppliesDeliveredValueCell.TextContent.Replace(",", string.Empty), out int suppliesDeliveredTotal))
            {
                result.TotalSuppliesDelivered = suppliesDeliveredTotal;
            }
            if (document?.GetElementsByTagName("div")?.Where(e => e is IHtmlDivElement i && i.TextContent == "Ships lost").Skip(1).FirstOrDefault() is IHtmlDivElement shipsLostCell &&
                shipsLostCell.NextSibling is IHtmlDivElement shipsLostValueCell &&
                int.TryParse(shipsLostValueCell.TextContent.Replace(",", string.Empty), out int shipsLostTotal))
            {
                result.TotalShipsLost = shipsLostTotal;
            }

            IHtmlTableElement? table = document?.GetElementsByTagName("table").FirstOrDefault() as IHtmlTableElement;
            if (table?.GetElementsByTagName("tbody").FirstOrDefault() is IHtmlTableSectionElement tbody)
            {
                foreach (IHtmlTableRowElement row in tbody.Rows)
                {
                    if (row.Cells.Length == 5)
                    {
                        if (// Date
                            !(row.Cells.ElementAt(0) is IHtmlTableDataCellElement dateCell && DateOnly.TryParseExact(dateCell.TextContent, "dd MMM yyyy", out DateOnly date)) ||
                            // Scout kills
                            !(row.Cells.ElementAt(1) is IHtmlTableDataCellElement killsCell && int.TryParse(killsCell.TextContent.Replace(",", string.Empty), out int kills)) ||
                            // Rescues
                            !(row.Cells.ElementAt(2) is IHtmlTableDataCellElement rescueCell &&
                            rescueCell.FirstChild is IText rescueCellText &&
                            int.TryParse(rescueCellText.TextContent.Replace(",", string.Empty), out int rescues)) ||
                            // Supplies
                            !(row.Cells.ElementAt(3) is IHtmlTableDataCellElement suppliesCell &&
                            suppliesCell.FirstChild is IText suppliesCellText &&
                            int.TryParse(suppliesCellText.TextContent.Replace(",", string.Empty), out int supplies))
                            )
                        {
                            continue;
                        }
                        result.Details.Add((date, kills, rescues, supplies));
                    }
                }
            }
            return result;
        }

        private async Task<IDocument?> Get(string path)
        {
            TimeSpan nextRequestWait = LastRequest.AddSeconds(10) - DateTimeOffset.Now;
            if (nextRequestWait.TotalMilliseconds > 0)
            {
                await Task.Delay(nextRequestWait);
            }
            using HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.GetAsync(BaseUrl + path);
            LastRequest = DateTimeOffset.Now;
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return await IBrowsingContext.OpenAsync(req => req.Content(content));
            }
            else
            {
                Log.LogError("Response code: {responseCode}", response.StatusCode);
            }
            return null;
        }

        [GeneratedRegex("/([0-9]+)/$")]
        private static partial Regex PathRegexGen();
    }

    internal class ConflictDetails
    {
        public List<(DateOnly date, int kills, int rescues, int supplies)> Details { get; } = new();
        public int TotalThargoidsKilled { get; set; }
        public int TotalRescuesPerformed { get; set; }
        public int TotalSuppliesDelivered { get; set; }
        public int TotalShipsLost { get; set; }
    }
}
