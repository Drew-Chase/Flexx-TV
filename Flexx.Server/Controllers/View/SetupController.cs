using Flexx.Authentication;
using Flexx.Networking;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using static Flexx.Data.Global;

namespace Flexx.Server.Controllers.View
{
    [Route("/Setup/")]
    public class SetupController : Controller
    {
        #region Public Methods

        [HttpPost("finish")]
        public IActionResult Finish([FromForm] string movie, [FromForm] string tv, [FromForm] bool portForward, [FromForm] int port, [FromForm] string token)
        {
            ViewData["UseNav"] = false;
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { error = "Token cannot be null." });
            config.MovieLibraryPath = movie;
            config.TVLibraryPath = tv;
            config.ApiPort = port;
            config.PortForward = portForward;
            config.Setup = true;
            config.Token = token;
            if (Remote.RegisterServer(Users.Instance.Get(token)))
            {
                new Process()
                {
                    StartInfo = new()
                    {
                        FileName = Paths.ExecutingBinary,
                        UseShellExecute = true,
                    }
                }.Start();
                Environment.Exit(0);
                return RedirectToAction("Index", "Library");
            }
            else
            {
                return BadRequest(new { error = "Unable to register server" });
            }
        }

        public IActionResult Index()
        {
            if (config.Setup)
                return Redirect("/");
            ViewData["Title"] = "Setup";
            ViewData["UseNav"] = false;
            return View();
        }

        [HttpPost("login")]
        public IActionResult Login([FromForm] string username, [FromForm] string password)
        {
            return new JsonResult(Users.Instance.Get(username).GenerateToken(password));
        }

        #endregion Public Methods
    }
}