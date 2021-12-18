using Flexx.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/")]
    public class GlobalController : ControllerBase
    {
        #region Public Methods

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return new JsonResult(new
            {
                status = "Online",
                usage = ApplicationMonitor.CurrentUsage
            });
        }

        public IActionResult Index()
        {
            return GetStatus();
        }

        #endregion Public Methods
    }
}