using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System.Net;
using System.Text.RegularExpressions;

namespace EDDataProcessor.Inara
{
    internal partial class InaraClient : IDisposable
    {
        private string BaseUrl { get; }
        private IBrowsingContext IBrowsingContext { get; }
        private Regex PathRegex { get; } = PathRegexGen();
        private DateTimeOffset LastRequest { get; set; } = DateTimeOffset.Now;
        private int RequestsSent { get; set; }
        private ILogger Log { get; }
        private CookieContainer CookieContainer { get; } = new();
        private HttpClientHandler ClientHandler { get; }
        private HttpClient HttpClient { get; }

        public InaraClient(Microsoft.Extensions.Configuration.IConfiguration configuration, ILogger<InaraClient> log)
        {
            AngleSharp.IConfiguration config = Configuration.Default;
            IBrowsingContext = BrowsingContext.New(config);
            BaseUrl = configuration.GetValue<string>("Inara:BaseUrl") ?? throw new Exception("Inara:BaseUrl is not configured");
            Log = log;
            ClientHandler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                CookieContainer = CookieContainer,
                UseCookies = true,
            };
            HttpClient = new(ClientHandler);
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
            HttpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            HttpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            HttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            HttpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-CH-UA", @"""Not_A Brand"";v=""99"", ""Google Chrome"";v=""109"", ""Chromium"";v=""109""");
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-CH-UA-Platform", @"""Windows""");
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "none");
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-User", "?1");
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            HttpClient.DefaultRequestHeaders.Referrer = new Uri("https://inara.cz/elite/news/");
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
            if (table?.GetElementsByTagName("thead").FirstOrDefault() is IHtmlTableSectionElement thead)
            {
                int dateCellNumber = -1;
                int killsCellNumber = -1;
                int rescueCellNumber = -1;
                int suppliesCellNumber = -1;
                {
                    if (thead.Rows.Any() && thead.Rows.First() is IHtmlTableRowElement row)
                    {
                        if (row.Cells.Length == 5)
                        {
                            int cellNumber = 0;
                            foreach (IHtmlTableCellElement cell in row.Cells)
                            {
                                switch (cell.TextContent)
                                {
                                    case "Date":
                                        {
                                            dateCellNumber = cellNumber;
                                            break;
                                        }
                                    case "Kills":
                                        {
                                            killsCellNumber = cellNumber;
                                            break;
                                        }
                                    case "Rescues":
                                        {
                                            rescueCellNumber = cellNumber;
                                            break;
                                        }
                                    case "Supplies":
                                        {
                                            suppliesCellNumber = cellNumber;
                                            break;
                                        }
                                }
                                cellNumber++;
                            }
                        }
                    }
                }
                bool failed = false;
                if (dateCellNumber == -1)
                {
                    Log.LogWarning("Date cell could not be located");
                    failed = true;
                }
                if (killsCellNumber == -1)
                {
                    Log.LogWarning("Kills cell could not be located");
                    failed = true;
                }
                if (rescueCellNumber == -1)
                {
                    Log.LogWarning("Rescues cell could not be located");
                    failed = true;
                }
                if (suppliesCellNumber == -1)
                {
                    Log.LogWarning("Supplies cell could not be located");
                    failed = true;
                }
                if (!failed && table?.GetElementsByTagName("tbody").FirstOrDefault() is IHtmlTableSectionElement tbody)
                {
                    foreach (IHtmlTableRowElement row in tbody.Rows)
                    {
                        if (row.Cells.Length == 5)
                        {
                            if (// Date
                                !(row.Cells.ElementAt(dateCellNumber) is IHtmlTableDataCellElement dateCell &&
                                DateOnly.TryParseExact(dateCell.TextContent, "dd MMM yyyy", out DateOnly date)) ||
                                // Kills
                                !(row.Cells.ElementAt(killsCellNumber) is IHtmlTableDataCellElement killsCell &&
                                int.TryParse(killsCell.TextContent.Replace(",", string.Empty), out int kills)) ||
                                // Rescues
                                !(row.Cells.ElementAt(rescueCellNumber) is IHtmlTableDataCellElement rescueCell &&
                                rescueCell.FirstChild is IText rescueCellText &&
                                int.TryParse(rescueCellText.TextContent.Replace(",", string.Empty), out int rescues)) ||
                                // Supplies
                                !(row.Cells.ElementAt(suppliesCellNumber) is IHtmlTableDataCellElement suppliesCell &&
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
            }
            return result;
        }

        public int ResetRequestCount()
        {
            int result = RequestsSent;
            RequestsSent = 0;
            return result;
        }

        private async Task<IDocument?> Get(string path)
        {
            Random rnd = new();
            int d = rnd.Next(30, 60);
            TimeSpan nextRequestWait = LastRequest.AddSeconds(d) - DateTimeOffset.Now;
            if (nextRequestWait.TotalMilliseconds > 0)
            {
                await Task.Delay(nextRequestWait);
            }
            using HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl + path);
            RequestsSent++;
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
