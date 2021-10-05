using System;
using Microsoft.AspNetCore.Mvc;
using static Flexx.Core.Data.Global;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/")]
    public class GlobalController : ControllerBase
    {
        public IActionResult Index()
        {
            return GetStatus();
        }
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return new JsonResult(new
            {
                status = "Online",
            });
        }

    }
}
