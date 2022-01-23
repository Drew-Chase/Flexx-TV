using Flexx.Utilities;
using Microsoft.AspNetCore.Mvc;
using static Flexx.Data.Global;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/")]
    public class GlobalController : ControllerBase
    {
        #region Public Methods

        /// <summary>
        /// Gets server base information.
        /// </summary>
        /// <returns> </returns>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            if (!config.Setup)
            {
                return NotFound();
            }
            return new JsonResult(new
            {
                status = "Online",
                usage = ApplicationMonitor.CurrentUsage
            });
        }

        /// <summary>
        /// Redirects to <see cref="GetStatus"> Status </see>
        /// </summary>
        /// <returns> </returns>
        public IActionResult Index()
        {
            return GetStatus();
        }

        #endregion Public Methods
    }
}