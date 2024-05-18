using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EDOverwatch_Web.Authentication
{
    public class CommanderApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, EdDbContext dbContext) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        private EdDbContext DbContext { get; } = dbContext;

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Request.Headers.TryGetValue("X-Auth-Key", out StringValues authKey) && Guid.TryParse(authKey.ToString(), out Guid key))
            {
                if (await DbContext.CommanderApiKeys.AnyAsync(a => a.Key == key))
                {
                    Claim[] claims =
                    [
                        new Claim(ClaimTypes.NameIdentifier, key.ToString()),
                    ];

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
