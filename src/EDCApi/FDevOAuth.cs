using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace EDCApi
{
    public class FDevOAuth : IDisposable
    {
        private HttpClient HttpClient { get; } = new(/*new LoggingHandler(new HttpClientHandler())*/);
        private IConfiguration Configuration { get; }
        private string ClientId { get; }
        private ILogger Log { get; }

        public FDevOAuth(IConfiguration configuration, ILogger<FDevOAuth> log)
        {
            Configuration = configuration;
            ClientId = Configuration.GetValue<string>("FDevOAuth:ClientId") ?? throw new Exception("FDevOAuth:ClientId is not configured");
            Log = log;
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<OAuthenticationResult?> AuthenticateUser(string userCode, string codeVerifier, CancellationToken cancellationToken)
        {
            string authTokenUrl = Configuration.GetValue<string>("FDevOAuth:TokenUrl") ?? throw new Exception("FDevOAuth:TokenUrl is not configured");
            string redirectUrl = Configuration.GetValue<string>("FDevOAuth:RedirectUrl") ?? throw new Exception("FDevOAuth:RedirectUrl is not configured");
            TokenResponse tokenResponse = await HttpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = authTokenUrl,
                ClientId = ClientId,
                Code = userCode,
                RedirectUri = redirectUrl,
                CodeVerifier = codeVerifier,
                GrantType = "authorization_code",
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
            }, cancellationToken);
            if (tokenResponse.IsError)
            {
                Log.LogWarning("Request authorisation code token failed: {error} ({errorDescription})", tokenResponse.Error, tokenResponse.ErrorDescription);
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
            Log.LogWarning("User information request failed: {error}", userInfoResponse.Error);
            return null;
        }

        public OAuthAuthorizeUrl CreateAuthorizeUrl()
        {
            string authUrl = Configuration.GetValue<string>("FDevOAuth:AuthUrl") ?? throw new Exception("FDevOAuth:AuthUrl is not configured");
            string redirectUrl = Configuration.GetValue<string>("FDevOAuth:RedirectUrl") ?? throw new Exception("FDevOAuth:RedirectUrl is not configured");
            string scope = Configuration.GetValue<string>("FDevOAuth:Scope") ?? throw new Exception("FDevOAuth:Scope is not configured");
            string state = CryptoRandom.CreateUniqueId(32);
            string codeVerifier = CryptoRandom.CreateUniqueId(32);
            byte[] challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
            string codeChallenge = Base64Url.Encode(challengeBytes);
            RequestUrl requestUrl = new(authUrl);
            string url = requestUrl.CreateAuthorizeUrl(
                clientId: ClientId,
                responseType: "code",
                redirectUri: redirectUrl,
                state: state,
                scope: scope,
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256"
            );
            return new OAuthAuthorizeUrl(url, state, codeVerifier);
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

        public async Task<bool> TokenRefresh(OAuthCredentials oAuthCredentials, CancellationToken cancellationToken)
        {
            string authTokenUrl = Configuration.GetValue<string>("FDevOAuth:TokenUrl") ?? throw new Exception("FDevOAuth:TokenUrl is not configured");
            TokenResponse response = await HttpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = authTokenUrl,
                ClientId = ClientId,
                RefreshToken = oAuthCredentials.RefreshToken,
                GrantType = "refresh_token",
            }, cancellationToken);
            if (!response.IsError)
            {
                oAuthCredentials.TokenUpdated(response.AccessToken, response.RefreshToken);
                return true;
            }
            oAuthCredentials.TokenUpdateFailed();
            Log.LogWarning("TokenRefresh error: {responseError} {responseErrorDescription} (ErrorType: {responseErrorType}) (HttpStatusCode: {responseHttpStatusCode})", response.Error, response.ErrorDescription, response.ErrorType, response.HttpStatusCode);
            return false;
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
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public OAuthCredentialsStatus Status { get; private set; }

        public OAuthCredentials(string tokenType, string accessToken, string refreshToken)
        {
            TokenType = tokenType;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public void TokenUpdated(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                RefreshToken = refreshToken;
            }
            Status = OAuthCredentialsStatus.Refreshed;
        }

        public void TokenUpdateFailed()
        {
            Status = OAuthCredentialsStatus.Expired;
        }
    }

    public enum OAuthCredentialsStatus
    {
        Valid,
        Refreshed,
        Expired,
    }

    public class OAuthAuthorizeUrl
    {
        public string Url { get; }
        public string State { get; }
        public string CodeVerifier { get; }

        public OAuthAuthorizeUrl(string url, string state, string codeVerifier)
        {
            Url = url;
            State = state;
            CodeVerifier = codeVerifier;
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
