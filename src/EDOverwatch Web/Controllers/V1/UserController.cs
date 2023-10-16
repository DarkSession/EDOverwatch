using EDCApi;
using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets;
using EDOverwatch_Web.WebSockets.Handler;
using Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public partial class UserController : ControllerBase
    {
        private UserManager<ApplicationUser> UserManager { get; }
        private SignInManager<ApplicationUser> SignInManager { get; }
        private EdDbContext DbContext { get; }
        private FDevOAuth FDevOAuth { get; }
        private ActiveMqMessageProducer Producer { get; }
        private WebSocketServer WebSocketServer { get; }

        public UserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EdDbContext dbContext,
            FDevOAuth fDevOAuth,
            ActiveMqMessageProducer producer,
            WebSocketServer webSocketServer)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            DbContext = dbContext;
            FDevOAuth = fDevOAuth;
            Producer = producer;
            WebSocketServer = webSocketServer;
        }

        [HttpPost]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            Microsoft.AspNetCore.Identity.SignInResult signInResult = await SignInManager.PasswordSignInAsync(loginRequest.UserName, loginRequest.Password, true, false);
            return new LoginResponse(signInResult.Succeeded);
        }

        [HttpGet]
        public async Task<ActionResult> Logout()
        {
            await SignInManager.SignOutAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<OAuthResponse>> Register(RegistrationRequest registrationRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            ApplicationUser user = new(registrationRequest.UserName)
            {
                Email = registrationRequest.Email,
            };
            IdentityResult result = await UserManager.CreateAsync(user, registrationRequest.Password);
            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, true);
                user.Commander = new(
                            0,
                            string.Empty,
                            0,
                            false,
                            DateTimeOffset.UtcNow.AddYears(-1),
                            DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-14).Date),
                            0,
                            DateTimeOffset.UtcNow.AddDays(-14).Date,
                            DateTimeOffset.UtcNow,
                            CommanderOAuthStatus.Inactive,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            CommanderFleetHasFleetCarrier.Unknown,
                            CommanderPermissions.Default)
                {
                    User = user
                };
                await DbContext.SaveChangesAsync(cancellationToken);
                return new OAuthResponse(new MeResponse(user));
            }
            return new OAuthResponse(result.Errors.Select(e => e.Description).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<OAuthResponse>> OAuth(OAuthRequest requestData, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            OAuthCode? oAuthCode = await DbContext.OAuthCodes.FirstOrDefaultAsync(f => f.State == requestData.State, cancellationToken);
            if (oAuthCode != null)
            {
                DbContext.OAuthCodes.Remove(oAuthCode);
                await DbContext.SaveChangesAsync(cancellationToken);
                OAuthenticationResult? oAuthenticationResult = await FDevOAuth.AuthenticateUser(requestData.Code, oAuthCode.Code, cancellationToken);
                if (oAuthenticationResult != null && oAuthenticationResult.CustomerId > 0)
                {
                    EDDatabase.Commander? commander = await DbContext.Commanders
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(u => u.FDevCustomerId == oAuthenticationResult.CustomerId, cancellationToken);
                    bool isCommanderNew = false;
                    if (commander == null)
                    {
                        isCommanderNew = true;
                        Profile? profile = await FDevOAuth.GetProfile(oAuthenticationResult.Credentials, cancellationToken);
                        if (string.IsNullOrEmpty(profile?.Commander?.Name))
                        {
                            return new OAuthResponse(new List<string>() { "Authentication failed! Your Frontier profile does not seem to have an Elite Dangerous profile." });
                        }
                        string userName = RegexRemoveCharacters().Replace(profile!.Commander!.Name, "_") + "-" + oAuthenticationResult.CustomerId;

                        ApplicationUser user = new(userName)
                        {
                            Email = $"{userName}@edct.dev",
                        };
                        IdentityResult result = await UserManager.CreateAsync(user);
                        if (!result.Succeeded)
                        {
                            return new OAuthResponse(new List<string>() { "Registration could not be completed." });
                        }
                        await DbContext.SaveChangesAsync(cancellationToken);
                        commander = new(
                            0,
                            profile!.Commander!.Name,
                            oAuthenticationResult.CustomerId,
                            false,
                            DateTimeOffset.UtcNow.AddYears(-1),
                            DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-14).Date),
                            0,
                            DateTimeOffset.UtcNow.AddDays(-14).Date,
                            DateTimeOffset.UtcNow,
                            CommanderOAuthStatus.Active,
                            oAuthenticationResult.Credentials.AccessToken,
                            oAuthenticationResult.Credentials.RefreshToken,
                            oAuthenticationResult.Credentials.TokenType,
                            CommanderFleetHasFleetCarrier.Unknown,
                            CommanderPermissions.Default)
                        {
                            User = user
                        };
                        user.Commander = commander;
                        DbContext.Commanders.Add(commander);
                    }
                    if (commander.User != null)
                    {
                        bool oAuthWasExpired = commander.OAuthStatus != CommanderOAuthStatus.Active;
                        commander.OAuthAccessToken = oAuthenticationResult.Credentials.AccessToken;
                        commander.OAuthRefreshToken = oAuthenticationResult.Credentials.RefreshToken;
                        commander.OAuthTokenType = oAuthenticationResult.Credentials.TokenType;
                        commander.OAuthStatus = CommanderOAuthStatus.Active;
                        await DbContext.SaveChangesAsync(cancellationToken);
                        await SignInManager.SignInAsync(commander.User, true);
                        if (isCommanderNew || oAuthWasExpired)
                        {
                            CommanderCApi commanderCApi = new(oAuthenticationResult.CustomerId);
                            await Producer.SendAsync(CommanderCApi.QueueName, CommanderCApi.Routing, commanderCApi.Message, cancellationToken);
                        }
                        if (oAuthWasExpired)
                        {
                            List<WebSocketSession> sessions = WebSocketServer.ActiveSessions.Where(a => a.UserId == commander.User.Id).ToList();
                            if (sessions.Any())
                            {
                                WebSocketMessage webSocketMessage = new(nameof(CommanderMe), new User(commander));
                                foreach (WebSocketSession session in sessions)
                                {
                                    await webSocketMessage.Send(session, cancellationToken);
                                }
                            }
                        }
                        return new OAuthResponse(new MeResponse(commander.User));
                    }
                }
            }
            return new OAuthResponse(new List<string>() { "Authentication failed!" });
        }

        [HttpPost]
        public async Task<ActionResult<OAuthGetStateResponse>> OAuthGetUrl(CancellationToken cancellationToken)
        {
            OAuthAuthorizeUrl oAuthAuthorizeUrl = FDevOAuth.CreateAuthorizeUrl();
            DbContext.OAuthCodes.Add(new(oAuthAuthorizeUrl.State, oAuthAuthorizeUrl.CodeVerifier, DateTimeOffset.UtcNow));
            await DbContext.SaveChangesAsync(cancellationToken);
            return new OAuthGetStateResponse(oAuthAuthorizeUrl.Url);
        }


        [GeneratedRegex("[^0-9a-z]", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex RegexRemoveCharacters();
    }

    public class LoginRequest
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        public LoginRequest(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }

        public LoginResponse(bool success)
        {
            Success = success;
        }
    }

    public class RegistrationRequest
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public RegistrationRequest(string userName, string password, string email)
        {
            UserName = userName;
            Password = password;
            Email = email;
        }
    }

    public class OAuthResponse
    {
        public bool Success { get; }
        public MeResponse? Me { get; }
        public List<string>? Error { get; }

        public OAuthResponse(MeResponse me)
        {
            Success = true;
            Me = me;
        }

        public OAuthResponse(List<string> errors)
        {
            Success = false;
            Error = errors;
        }
    }

    public class OAuthRequest
    {
        [Required]
        public string State { get; set; }

        [Required]
        public string Code { get; set; }

        public OAuthRequest(string state, string code)
        {
            State = state;
            Code = code;
        }
    }

    public class OAuthGetStateResponse
    {
        public string Url { get; }
        public OAuthGetStateResponse(string url)
        {
            Url = url;
        }
    }

    public class MeResponse
    {
        public bool LoggedIn { get; set; }
        public User? User { get; }

        public MeResponse(ApplicationUser? applicationUser)
        {
            LoggedIn = applicationUser != null;
            if (applicationUser?.Commander != null)
            {
                User = new(applicationUser.Commander);
            }
        }
    }
}
