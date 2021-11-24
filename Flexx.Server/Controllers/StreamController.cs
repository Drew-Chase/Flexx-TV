using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using static Flexx.Core.Data.Global;
using Flexx.Media.Objects.Extras;
using System.IO;

namespace Flexx.Server.Controllers;
[ApiController]
[Route("/api/stream/")]
public class StreamController : ControllerBase
{

    [HttpGet("get/version")]
    public FileStreamResult GetVideoStream(string id, string username, string library, string version, int? season, int? episode, int? start_time)
    {
        MediaBase media = library.Equals("movies") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        if (config.UseVersionFile)
            return File(new FileStream(Paths.GetVersionPath(Directory.GetParent(media.Metadata.PATH).FullName, media.Title, foundVersion.Height, foundVersion.BitRate), FileMode.Open, FileAccess.Read), "video/mp4", true);
        else
        {
            var (process, stream) = Transcoder.GetTranscodedStream(username, media, foundVersion, start_time.GetValueOrDefault(0));
            Users.Instance.Get(username).AddActiveStream($"{media.TMDB}_{season.GetValueOrDefault()}_{episode.GetValueOrDefault()}", process);
            System.Net.Mime.ContentDisposition cd = new()
            {
                FileName = "stream.m3u8",
                Inline = false  // false = prompt the user for downloading;  true = browser to try to show the file inline
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(stream, "application/x-mpegURL", true);
        }
    }

    [HttpGet("get/stream_info")]
    public JsonResult GetVideoStream(string id, string username, string library, string version, int? season, int? episode)
    {
        MediaBase media = library.Equals("movies") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        int ts = 0;
        string path = Path.Combine(Paths.TempData, "streams", "hls", $"stream_{foundVersion.DisplayName}_{username}");
        if (Directory.Exists(path))
            ts = Directory.GetFiles(path, "*.ts", SearchOption.TopDirectoryOnly).Length;
        return new(new
        {
            mime = config.UseVersionFile ? "video/mp4" : "application/x-mpegURL",
            currentPosition = ts == 0 ? 0 : ts * 10,
            maxPosition = media.MediaInfo.Duration.TotalSeconds,
        });

    }

    [HttpPost("remove")]
    public IActionResult RemoveFromActiveStream([FromForm] string id, [FromForm] string username, [FromForm] string library, [FromForm] int? season, [FromForm] int? episode)
    {
        MediaBase media = library.Equals("movies") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        if (media == null) return BadRequest(new { message = $"No Media File from Library \"{library}\" and an ID of \"{id}\"" });
        try
        {
            Users.Instance.Get(username).RemoveActiveStream($"{media.TMDB}_{season.GetValueOrDefault()}_{episode.GetValueOrDefault()}");
        }
        catch
        {
            return BadRequest();
        }
        return Ok(new { message = "Stream Successfully Removed" });
    }
}
