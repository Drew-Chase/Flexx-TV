using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using static Flexx.Data.Global;

namespace Flexx.Server.Controllers.View;

[Route("/settings/")]
public class ServerSettingsController : Controller
{
    #region Public Methods

    [HttpPost("fs")]
    public JsonResult GetFS([FromForm] string dir, [FromForm] bool? movie)
    {
        try
        {
            dir = movie.HasValue ? (movie.Value ? config.MovieLibraryPath : config.TVLibraryPath) : string.IsNullOrWhiteSpace(dir) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : dir;
            string[] dirLongs = Directory.GetDirectories(dir);
            for (int i = 0; i < dirLongs.Length; i++)
            {
                dirLongs[i] = new DirectoryInfo(dirLongs[i]).Name;
            }
            return new JsonResult(new
            {
                cd = dir,
                parent = Directory.GetParent(dir).FullName,
                directories = dirLongs
            });
        }
        catch
        {
            return GetFS("", movie);
        }
    }

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