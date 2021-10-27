using Microsoft.AspNetCore.Mvc;

namespace Flexx.Server.Controllers
{
    public class ServerSettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}