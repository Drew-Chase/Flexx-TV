using Microsoft.AspNetCore.Mvc;

namespace Flexx.Server.Controllers.View;

[Route("/settings/")]
public class ServerSettingsController : Controller
{
    #region Public Methods

    public IActionResult Index(string? c)
    {
        ViewData["Title"] = "Settings";
        ViewData["UseNav"] = true;
        ViewData["Page"] = c ?? "Dashboard";
        return View();
    }

    [Route("page")]
    public IActionResult Page(string page)
    {
        ViewData["Title"] = $"{page} Settings";
        ViewData["UseNav"] = false;
        return PartialView(page);
    }

    #endregion Public Methods
}