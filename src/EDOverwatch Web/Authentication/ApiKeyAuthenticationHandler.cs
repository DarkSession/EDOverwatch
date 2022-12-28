using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EDOverwatch_Web.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private EdDbContext DbContext { get; }
        public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, EdDbContext dbContext)
            : base(options, logger, encoder, clock)
        {
            DbContext = dbContext;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Request.Headers.TryGetValue("X-Auth-Key", out StringValues authKey) && Guid.TryParse(authKey.ToString(), out Guid key))
            {
                if (await DbContext.ApiKeys.AnyAsync(a => a.Key == key))
                {
                    Claim[] claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, key.ToString()),
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
