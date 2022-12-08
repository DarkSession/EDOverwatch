using IdentityModel;
using IdentityModel.Client;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace EDOverwatch_Web.CAPI
{
    public class FDevOAuth : IDisposable
    {
        private HttpClient HttpClient { get; } = new(/*new LoggingHandler(new HttpClientHandler())*/);
        private IConfiguration Configuration { get; }
        private EdDbContext DbContext { get; }
        private string ClientId { get; }
        private string RedirectUrl { get; }

        public FDevOAuth(IConfiguration configuration, EdDbContext dbContext)
        {
            Configuration = configuration;
            DbContext = dbContext;
            ClientId = Configuration.GetValue<string>("FDevOAuth:ClientId") ?? throw new Exception("FDevOAuth:ClientId is not configured");
            RedirectUrl = Configuration.GetValue<string>("FDevOAuth:ReturnUrl") ?? throw new Exception("FDevOAuth:ReturnUrl is not configured");
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<OAuthenticationResult?> AuthenticateUser(string userCode, OAuthCode oAuthCode, CancellationToken cancellationToken)
        {
            string authTokenUrl = Configuration.GetValue<string>("FDevOAuth:TokenUrl") ?? throw new Exception("FDevOAuth:TokenUrl is not configured");
            // If this request fails with an error 500, then we probably have a compatibility issue.
            // It was tested and worked with IdentityModel 5.2

            TokenResponse tokenResponse = await HttpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = authTokenUrl,
                ClientId = ClientId,
                Code = userCode,
                RedirectUri = RedirectUrl,
                CodeVerifier = oAuthCode.Code,
                GrantType = "authorization_code",
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
            }, cancellationToken);
            if (tokenResponse.IsError)
            {
                return null;
            }
            string userInfoUrl = Configuration.GetValue<string>("FDevOAuth:UserInfoUrl") ?? throw new Exception("FDevOAuth:UserInfoUrl is not configured");
            UserInfoResponse userInfoResponse = await HttpClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = userInfoUrl,
                Token = tokenResponse.AccessToken,
            }, cancellationToken);
            if (!userInfoResponse.IsError)
            {
                string customerIdString = userInfoResponse.TryGet("customer_id") ?? string.Empty;
                if (!string.IsNullOrEmpty(customerIdString) && long.TryParse(customerIdString, out long customerId))
                {
                    return new OAuthenticationResult(tokenResponse, userInfoResponse, customerId);
                }
            }
            return null;
        }

        public string CreateAuthorizeUrl()
        {
            string authUrl = Configuration.GetValue<string>("FDevOAuth:AuthUrl") ?? throw new Exception("FDevOAuth:AuthUrl is not configured");
            string state = CryptoRandom.CreateUniqueId(32);
            string codeVerifier = CryptoRandom.CreateUniqueId(32);
            byte[] challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
            string codeChallenge = Base64Url.Encode(challengeBytes);
            RequestUrl requestUrl = new(authUrl);
            DbContext.OAuthCodes.Add(new OAuthCode(state, codeVerifier, DateTimeOffset.Now));
            return requestUrl.CreateAuthorizeUrl(
                clientId: ClientId,
                responseType: "code",
                redirectUri: RedirectUrl,
                state: state,
                scope: "auth",
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256"
            );
        }

        public Task<Profile?> GetProfile(OAuthCredentials apiCredentials, CancellationToken cancellationToken)
        {
            return ApiCall<Profile>(apiCredentials, Configuration.GetValue<string>("FDevOAuth:ProfileUrl") ?? throw new Exception("FDevOAuth:ProfileUrl is not configured"), cancellationToken);
        }

        private async Task<T?> ApiCall<T>(OAuthCredentials apiCredentials, string url, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(apiCredentials.TokenType, apiCredentials.AccessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Method = HttpMethod.Get;
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string bodyContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return (T?)JsonConvert.DeserializeObject(bodyContent, typeof(T));
            }
            return default;
        }
    }

    public class OAuthenticationResult
    {
        public TokenResponse TokenResponse { get; }
        public UserInfoResponse UserInfoResponse { get; }
        public long CustomerId { get; }
        public OAuthCredentials Credentials { get; }
        public OAuthenticationResult(TokenResponse tokenResponse, UserInfoResponse userInfoResponse, long customerId)
        {
            TokenResponse = tokenResponse;
            UserInfoResponse = userInfoResponse;
            CustomerId = customerId;
            Credentials = new(tokenResponse.TokenType, tokenResponse.AccessToken, tokenResponse.RefreshToken);
        }
    }

    public class OAuthCredentials
    {
        public string TokenType { get; }
        public string AccessToken { get; }
        public string RefreshToken { get; }

        public OAuthCredentials(string tokenType, string accessToken, string refreshToken)
        {
            TokenType = tokenType;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }
    }

    /*
    public class LoggingHandler : DelegatingHandler
    {
        public LoggingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine("Request:");
            Console.WriteLine(request.ToString());
            if (request.Content != null)
            {
                Console.WriteLine(await request.Content.ReadAsStringAsync());
            }
            Console.WriteLine();

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            Console.WriteLine("Response:");
            Console.WriteLine(response.ToString());
            if (response.Content != null)
            {
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
            Console.WriteLine();

            return response;
        }
    }
    */
}
