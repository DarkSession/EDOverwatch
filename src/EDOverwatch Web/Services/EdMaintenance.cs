using Newtonsoft.Json;

namespace EDOverwatch_Web.Services
{
    public class EdMaintenance
    {
        public bool IsInMaintenanceMode { get; private set; }

        private HttpClient HttpClient { get; }
        private ILogger Log { get; }

        public EdMaintenance(HttpClient httpClient, ILogger<EdMaintenance> log)
        {
            HttpClient = httpClient;
            Log = log;
        }

        public async Task Update(CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpResponseMessage response = await HttpClient.GetAsync("https://ed-server-status.orerve.net/", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    Log.LogWarning($"Unable to retrieve server status. Http status code: {response.StatusCode}");
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrEmpty(responseBody))
                {
                    return;
                }

                StatusResponseModel? status = JsonConvert.DeserializeObject<StatusResponseModel>(responseBody);
                if (status is null)
                {
                    return;
                }

                IsInMaintenanceMode = status.Status == "Maintenance";
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Failed to retrieve server status");
            }
        }

        class StatusResponseModel
        {
            [JsonProperty("status")]
            public string? Status { get; set; }

            [JsonProperty("message")]
            public string? Message { get; set; }

            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("product")]
            public string? Product { get; set; }
        }
    }
}
