using Microsoft.AspNetCore.Mvc;

namespace Flexx.Server.Controllers
{
    public class ServerSettingsController : Controller
    {
        #region Public Methods

        public IActionResult Index()
        {
            return View();
        }

        #endregion Public Methods
    }
}