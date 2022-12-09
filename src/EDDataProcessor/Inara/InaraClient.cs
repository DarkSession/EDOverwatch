using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System.Text.RegularExpressions;

namespace EDDataProcessor.Inara
{
    internal class InaraClient : IDisposable
    {
        private const string BaseUrl = "https://inara.cz/elite/";
        private IBrowsingContext IBrowsingContext { get; }
        private Regex PathRegex { get; } = new("/([0-9]+)/$");
        private DateTimeOffset LastRequest { get; set; } = DateTimeOffset.Now;

        public InaraClient()
        {
            AngleSharp.IConfiguration config = Configuration.Default;
            IBrowsingContext = BrowsingContext.New(config);
        }

        public void Dispose()
        {
            IBrowsingContext.Dispose();
        }

        public async Task<int?> GetSystemId(string systemName)
        {
            using IDocument? document = await Get("starsystem/?search=" + Uri.EscapeDataString(systemName));
            IHtmlAnchorElement? overviewLink = document?.GetElementsByClassName("quickmenu")
                 .Where(d => d is IHtmlAnchorElement h && h.TextContent == "Overview")
                 .FirstOrDefault() as IHtmlAnchorElement;
            if (overviewLink != null)
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
            IHtmlTableSectionElement? tbody = table?.GetElementsByTagName("tbody").FirstOrDefault() as IHtmlTableSectionElement;
            if (tbody != null)
            {
                foreach (IHtmlTableRowElement row in tbody.Rows)
                {
                    IHtmlTableDataCellElement? cell = row.Cells.FirstOrDefault() as IHtmlTableDataCellElement;
                    IHtmlAnchorElement? link = cell?.GetElementsByTagName("a").FirstOrDefault() as IHtmlAnchorElement;
                    if (link != null)
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
            if (document?.GetElementsByTagName("div")?.Where(e => e is IHtmlDivElement i && i.TextContent == "Ships lost").Skip(1).FirstOrDefault() is IHtmlDivElement shipsLostCell &&
                shipsLostCell.NextSibling is IHtmlDivElement shipsLostValueCell &&
                int.TryParse(shipsLostValueCell.TextContent.Replace(",", string.Empty), out int shipsLost))
            {
                result.ShipsLost = shipsLost;
            }

            IHtmlTableElement? table = document?.GetElementsByTagName("table").FirstOrDefault() as IHtmlTableElement;
            IHtmlTableSectionElement? tbody = table?.GetElementsByTagName("tbody").FirstOrDefault() as IHtmlTableSectionElement;
            if (tbody != null)
            {
                foreach (IHtmlTableRowElement row in tbody.Rows)
                {
                    if (row.Cells.Count() == 5)
                    {

                        if (// Date
                            !(row.Cells.ElementAt(0) is IHtmlTableDataCellElement dateCell && DateOnly.TryParseExact(dateCell.TextContent, "dd MMM yyyy", out DateOnly date)) ||
                            // Scout kills
                            !(row.Cells.ElementAt(1) is IHtmlTableDataCellElement scoutKillCell && int.TryParse(scoutKillCell.TextContent.Replace(",", string.Empty), out int scoutKills)) ||
                            // Interceptor kills
                            !(row.Cells.ElementAt(2) is IHtmlTableDataCellElement interceptorKillCell &&
                            interceptorKillCell.FirstChild is IText interceptorKillCellText &&
                            int.TryParse(interceptorKillCellText.TextContent.Replace(",", string.Empty), out int interceptorKills)) ||
                            // Rescues
                            !(row.Cells.ElementAt(3) is IHtmlTableDataCellElement rescueCell &&
                            rescueCell.FirstChild is IText rescueCellText &&
                            int.TryParse(rescueCellText.TextContent.Replace(",", string.Empty), out int rescues))
                            )
                        {
                            continue;
                        }
                        result.Details.Add((date, scoutKills, interceptorKills, rescues));
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
            return null;
        }
    }

    internal class ConflictDetails
    {
        public List<(DateOnly date, int scoutKills, int interceptorKills, int rescues)> Details { get; } = new();
        public int ShipsLost { get; set; }
    }
}
