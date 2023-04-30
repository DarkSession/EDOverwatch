using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EDOverwatch_Web.Authentication
{
    public class CommanderApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private EdDbContext DbContext { get; }
        public CommanderApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, EdDbContext dbContext)
            : base(options, logger, encoder, clock)
        {
            DbContext = dbContext;
        }

        private StringValues? GetApiKeyStringValuesFromHeader()
        {
            if (Context.Request.Headers.TryGetValue("apikey", out StringValues apikey))
            {
                return apikey;
            }
            if (Context.Request.Headers.TryGetValue("X-Auth-Key", out StringValues authKey))
            {
                return authKey;
            }
            return null;
        }

        private Guid? GetApiKeyFromHeader()
        {
            StringValues? apiKey = GetApiKeyStringValuesFromHeader();
            if (apiKey is not null && Guid.TryParse(apiKey.ToString(), out Guid key))
            {
                return key;
            }
            return null;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {   
            if (GetApiKeyFromHeader() is Guid key)
            {
                Commander? commander = await DbContext.Commanders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ApiKey!.Key == key);
                if (commander != null)
                {
                    Claim[] claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, key.ToString()),
                        new Claim("CommanderId", commander.Id.ToString()),
                    };

                    ClaimsIdentity claimsIdentity = new(claims, Scheme.Name);
                    ClaimsPrincipal claimsPrincipal = new(claimsIdentity);
                    AuthenticationTicket ticket = new(claimsPrincipal, Scheme.Name);
                    return AuthenticateResult.Success(ticket);
                }
            }
            return AuthenticateResult.Fail("Fail");
        }
    }
}
