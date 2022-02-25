using Flexx.Authentication;
using Flexx.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using static Flexx.Data.Global;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/settings/")]
    public class ServerSettingsController : ControllerBase
    {
        #region Public Methods

        /// <summary>
        /// Will return JSON interpreted version of the HOST computers file system
        /// </summary>
        /// <param name="dir">   </param>
        /// <param name="movie"> </param>
        /// <returns> </returns>
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

        public IActionResult Index(string username)
        {
            if (!Users.Instance.Get(username).IsHost)
            {
                return Unauthorized(new { success = false, message = "User is not authorized to view this data" });
            }
            SonarrRadarrSupport.TestConnection();
            return Ok(config);
        }

        /// <summary>
        /// Sets up Sonarr or Radarr
        /// </summary>
        /// <param name="token"> Users authentication token </param>
        /// <param name="url">   Radarr or Sonarr;s base url </param>
        /// <param name="key">   Radarr or Sonarr's API Key </param>
        /// <param name="port">  Radarr or Sonarr's port </param>
        /// <param name="type">  Radarr or Sonarr </param>
        /// <returns> </returns>
        [HttpPost("{type}")]
        public IActionResult SetupSonarrRadarr([FromForm] string token, [FromForm] string url, [FromForm] string key, [FromForm] int port, string type)
        {
            User user = Users.Instance.Get(token);
            if (!user.IsHost || !user.IsAuthorized(PlanTier.Rust))
            {
                return Unauthorized(new { success = false, message = "User is not authorized to modify this value" });
            }
            if (!url.Contains("http://") && !url.Contains("https://"))
            {
                return BadRequest(new { success = false, message = "no protocol was found" });
            }
            if (url.Replace("http://", "").Replace("https://", "").Contains(":"))
            {
                return BadRequest(new { success = false, message = "do not include port in url" });
            }
            type = type.ToLower();
            if (type.Equals("sonarr"))
            {
                config.SonarrBaseUrl = url;
                config.SonarrPort = port;
                config.SonarrAPIKey = key;
            }
            else if (type.Equals("radarr"))
            {
                config.RadarrBaseUrl = url;
                config.RadarrPort = port;
                config.RadarrAPIKey = key;
            }
            if (!SonarrRadarrSupport.TestConnection(type.Equals("radarr"), out HttpResponseMessage response))
            {
                string message = "";
                if (response != null)
                {
                    try
                    {
                        message = (string) JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result)["error"];
                    }
                    catch { }
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        message = $"Server returned {response.StatusCode}";
                    }
                    return Unauthorized(new { success = false, message });
                }
                else
                {
                    return BadRequest(new { success = false, message = $"Unknown issue occurred while trying to connect to {type.ToUpper()}" });
                }
            }
            return Ok(new { success = true, message = $"{type} was successfully setup" });
        }

        #endregion Public Methods
    }
}