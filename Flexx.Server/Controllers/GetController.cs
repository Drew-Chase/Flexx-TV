using Microsoft.AspNetCore.Mvc;
using System.IO;
using static Flexx.Core.Data.Global;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/get/")]
    public class GetController : ControllerBase
    {
        [HttpGet("images/missing-artwork")]
        public IActionResult GetMissingArtworkPoster()
        {
            return File(new FileStream(Paths.MissingPoster, FileMode.Open), "image/jpg");
        }
    }
}