using Microsoft.AspNetCore.Mvc;

namespace Flexx.Server.Controllers.View
{
    public class Library : Controller
    {
        #region Public Methods

        [Route("/")]
        public IActionResult Index()
        {
            ViewData["UseNav"] = true;
            ViewData["Title"] = "Home";
            ViewData["Library"] = "Home";
            return View();
        }

        [Route("/Movies")]
        public IActionResult Movies()
        {
            ViewData["UseNav"] = true;
            ViewData["Title"] = "Movies";
            ViewData["Library"] = "Movies";
            return View();
        }

        [Route("/Search")]
        public IActionResult Search(string? c, string? q)
        {
            ViewData["UseNav"] = true;
            if (string.IsNullOrWhiteSpace(c))
            {
                return View("Search-Options");
            }
            else
            {
                ViewData["Search"] = q ?? "";
                ViewData["Category"] = c;
                return View("Search");
            }
        }

        [Route("/TV")]
        public IActionResult TV()
        {
            ViewData["UseNav"] = true;
            ViewData["Title"] = "TV";
            ViewData["Library"] = "TV";
            return View();
        }

        [Route("/View")]
        public IActionResult ViewMedia(string library, string id, int? season, int? episode)
        {
            ViewData["UseNav"] = true;
            ViewData["Library"] = library;
            ViewData["ID"] = id;
            ViewData["Episode"] = episode.GetValueOrDefault(-1);
            ViewData["Season"] = season.GetValueOrDefault(-1);
            return View("View");
        }

        [Route("/Watch")]
        public IActionResult Watch(string library, string id, bool? trailer, bool? resume, bool? debug, int? episode, int? season)
        {
            ViewData["UseNav"] = false;
            ViewData["Library"] = library;
            ViewData["ID"] = id;
            ViewData["Trailer"] = trailer.GetValueOrDefault(false);
            ViewData["Resume"] = resume.GetValueOrDefault(false);
            ViewData["Debug"] = debug.GetValueOrDefault(false);
            ViewData["Episode"] = episode.GetValueOrDefault(-1);
            ViewData["Season"] = season.GetValueOrDefault(-1);
            return View();
        }

        #endregion Public Methods
    }
}