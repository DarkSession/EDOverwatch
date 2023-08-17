using EDOverwatch_Web.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EDOverwatch_Web.Controllers
{
    [AllowAnonymous]
    [Route("ws")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class WebSocketController : ControllerBase
    {
        private UserManager<ApplicationUser> UserManager { get; }
        private IServiceScopeFactory ServiceScopeFactory { get; }
        private WebSocketServer WebSocketServer { get; }

        public WebSocketController(UserManager<ApplicationUser> userManager, IServiceScopeFactory serviceScopeFactory, WebSocketServer webSocketServer)
        {
            UserManager = userManager;
            ServiceScopeFactory = serviceScopeFactory;
            WebSocketServer = webSocketServer;
        }

        public async Task Get(CancellationToken cancellationToken)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                // BadRequest();
                throw new Exception("Not a web socket request");
            }
            ApplicationUser? applicationUser = null;
            if (User != null)
            {
                applicationUser = await UserManager.GetUserAsync(User);
            }
            await WebSocketServer.ProcessRequest(HttpContext, applicationUser, ServiceScopeFactory, cancellationToken);
        }
    }
}
