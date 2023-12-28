using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/v1/status")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class Status : ControllerBase
    {
        [HttpGet]
        public ActionResult Get()
        {
            return Ok("Ok");
        }
    }
}
