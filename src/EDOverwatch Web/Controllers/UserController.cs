using EDOverwatch_Web.CAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EDOverwatch_Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [AllowAnonymous]
    internal partial class UserController : ControllerBase
    {
        private UserManager<ApplicationUser> UserManager { get; }
        private SignInManager<ApplicationUser> SignInManager { get; }
        private EdDbContext DbContext { get; }
        private FDevOAuth FDevOAuth { get; }

        public UserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EdDbContext dbContext,
            FDevOAuth fDevOAuth
            )
        {
            UserManager = userManager;
            SignInManager = signInManager;
            DbContext = dbContext;
            FDevOAuth = fDevOAuth;
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
        public async Task<ActionResult<RegistrationResponse>> Register(RegistrationRequest registrationRequest)
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
                return new RegistrationResponse(true);
            }
            return new RegistrationResponse(result.Errors.Select(e => e.Description).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<RegistrationResponse>> OAuth(OAuthRequest requestData, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            OAuthCode? oAuthCode = await DbContext.OAuthCodes.FirstOrDefaultAsync(f => f.State == requestData.State, cancellationToken);
            if (oAuthCode != null)
            {
                DbContext.OAuthCodes.Remove(oAuthCode);
                OAuthenticationResult? oAuthenticationResult = await FDevOAuth.AuthenticateUser(requestData.Code, oAuthCode, cancellationToken);
                if (oAuthenticationResult != null && oAuthenticationResult.CustomerId > 0)
                {
                    Commander? commander = await DbContext.Commanders
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(u => u.FDevCustomerId == oAuthenticationResult.CustomerId, cancellationToken);
                    if (commander == null)
                    {
                        Profile? profile = await FDevOAuth.GetProfile(oAuthenticationResult.Credentials, cancellationToken);
                        if (string.IsNullOrEmpty(profile?.Commander?.Name))
                        {
                            return new RegistrationResponse(new List<string>() { "Authentication failed! Your Frontier profile does not seem to have an Elite Dangerous profile." });
                        }
                        string userName = RegexRemoveCharacters().Replace(profile!.Commander!.Name, "_") + "-" + oAuthenticationResult.CustomerId;

                        ApplicationUser user = new(userName)
                        {
                            Email = $"{userName}@edct.dev",
                        };
                        IdentityResult result = await UserManager.CreateAsync(user);
                        if (result.Succeeded)
                        {
                            await DbContext.SaveChangesAsync(cancellationToken);
                            await SignInManager.SignInAsync(user, true);
                            return new RegistrationResponse(true);
                        }
                        return new RegistrationResponse(new List<string>() { "Registration could not be completed." });
                    }
                    else if (commander.User != null)
                    {
                        await SignInManager.SignInAsync(commander.User, true);
                        return new RegistrationResponse(true);
                    }
                }
            }
            return new RegistrationResponse(new List<string>() { "Authentication failed!" });
        }

        [HttpPost]
        public async Task<ActionResult<FDevGetStateResponse>> OAuthGetUrl(CancellationToken cancellationToken)
        {
            string url = FDevOAuth.CreateAuthorizeUrl();
            await DbContext.SaveChangesAsync(cancellationToken);
            return new FDevGetStateResponse(url);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Me()
        {
            ApplicationUser? user = await UserManager.GetUserAsync(User);
            if (user != null)
            {

            }
            return Ok();
        }

        [GeneratedRegex("[^0-9a-z]", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex RegexRemoveCharacters();
    }

    internal class LoginRequest
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

    internal class LoginResponse
    {
        public bool Success { get; set; }

        public LoginResponse(bool success)
        {
            Success = success;
        }
    }

    internal class RegistrationRequest
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

    internal class RegistrationResponse
    {
        public bool Success { get; }

        public List<string>? Error { get; }

        public RegistrationResponse(bool success)
        {
            Success = success;
        }

        public RegistrationResponse(List<string> errors) : this(false)
        {
            Error = errors;
        }
    }

    internal class OAuthRequest
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

    internal class FDevGetStateResponse
    {
        public string Url { get; }
        public FDevGetStateResponse(string url)
        {
            Url = url;
        }
    }
}
