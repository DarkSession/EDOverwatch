using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace EDCApi
{
    public class CAPI : IDisposable
    {
        private HttpClient HttpClient { get; } = new();
        private IConfiguration Configuration { get; }
        private ILogger Log { get; }
        private FDevOAuth FDevOAuth { get; }
        private static DateTimeOffset LastRequest { get; set; } = DateTimeOffset.Now.AddMinutes(-1);

        public CAPI(IConfiguration configuration, ILogger<CAPI> log, FDevOAuth fDevOAuth)
        {
            Configuration = configuration;
            Log = log;
            FDevOAuth = fDevOAuth;
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            GC.SuppressFinalize(this);
        }

        protected async Task<(HttpStatusCode httpStatusCode, string? content)> GetUrl(string url, OAuthCredentials oAuthCredentials, CancellationToken cancellationToken)
        {
            TimeSpan timeSinceLastRequest = DateTimeOffset.Now - LastRequest;
            if (timeSinceLastRequest < TimeSpan.FromMilliseconds(500))
            {
                TimeSpan waitTime = TimeSpan.FromMilliseconds(500) - timeSinceLastRequest;
                await Task.Delay(waitTime, cancellationToken);
            }
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(oAuthCredentials.TokenType, oAuthCredentials.AccessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Method = HttpMethod.Get;
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            LastRequest = DateTimeOffset.Now;
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (await FDevOAuth.TokenRefresh(oAuthCredentials, cancellationToken))
                {
                    using HttpRequestMessage newRequest = new(HttpMethod.Get, url);
                    newRequest.Headers.Authorization = new AuthenticationHeaderValue(oAuthCredentials.TokenType, oAuthCredentials.AccessToken);
                    newRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    newRequest.Method = HttpMethod.Get;
                    using HttpResponseMessage newResponse = await HttpClient.SendAsync(newRequest, cancellationToken);
                    return await GetUrlProcessResponse(newResponse, cancellationToken);
                }
                return (response.StatusCode, null);
            }
            else if (!response.IsSuccessStatusCode)
            {
                Log.LogWarning("GetUrl {url} returned {statusCode}", url, response.StatusCode);
            }
            return await GetUrlProcessResponse(response, cancellationToken);
        }

        private static async Task<(HttpStatusCode httpStatusCode, string? content)> GetUrlProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            }
            return (response.StatusCode, null);
        }

        public async Task<Profile?> GetProfile(OAuthCredentials oAuthCredentials, CancellationToken cancellationToken)
        {
            string profileUrl = Configuration.GetValue<string>("CApi:ProfileUrl") ?? throw new Exception("CApi:ProfileUrl is not configured");
            (HttpStatusCode httpStatusCode, string? content) = await GetUrl(profileUrl, oAuthCredentials, cancellationToken);
            if (httpStatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(content))
            {
                Profile? profile = JsonConvert.DeserializeObject<Profile>(content);
                return profile;
            }
            return null;
        }

        public async Task<(bool success, string? journal)> GetJournal(OAuthCredentials oAuthCredentials, DateOnly date, CancellationToken cancellationToken)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            bool isTodaysJournal = (today.Day == date.Day && today.Month == date.Month && today.Year == date.Year);
            string dateString = isTodaysJournal ? string.Empty : "/" + date.ToString("yyyy/MM/dd");
            string journalUrl = Configuration.GetValue<string>("CApi:JournalUrl") ?? throw new Exception("CApi:JournalUrl is not configured");
            for (int i = 0; i < 10; i++)
            {
                (HttpStatusCode httpStatusCode, string? content) = await GetUrl(journalUrl + dateString, oAuthCredentials, cancellationToken);
                switch (httpStatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.NoContent:
                        {
                            return (true, content);
                        }
                    case HttpStatusCode.PartialContent:
                        {
                            Log.LogWarning("GetJournal returned PartialContent. Waiting 5 seconds...");
                            await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
                            break;
                        }
                    default:
                        {
                            Log.LogWarning("GetJournal returned {httpStatusCode}", httpStatusCode);
                            return (false, null);
                        }
                }
            }
            return (false, null);
        }

        /*
        public async Task<(bool, FleetCarrier?)> GetFleetCarrier(OAuthCredentials oAuthCredentials)
        {
            string fleetCarrierUrl = Configuration.GetValue<string>("CApi:FleetCarrierUrl") ?? throw new Exception("CApi:FleetCarrierUrl is not configured");
            (string? fleetCarrierData, HttpStatusCode httpStatusCode) = await GetUrl(fleetCarrierUrl, oAuthCredentials);
            if (httpStatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(fleetCarrierData))
            {
                FleetCarrier? fleetCarrier = JsonConvert.DeserializeObject<FleetCarrier>(fleetCarrierData);
                return (true, fleetCarrier);
            }
            else if (httpStatusCode == HttpStatusCode.NotFound)
            {
                return (true, null);
            }
            return (false, null);
        }
        */
    }
}
