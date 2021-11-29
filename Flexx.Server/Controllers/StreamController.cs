using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using static Flexx.Core.Data.Global;

namespace Flexx.Server.Controllers;

[ApiController]
[Route("/api/stream/")]
public class StreamController : ControllerBase
{
    [HttpGet("start")]
    public JsonResult StartStream(string id, string username, string library, string version, int? season, int? episode, int? start_time)
    {
        MediaBase media = library.Equals("movies") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        var stream = Transcoder.GetTranscodedStream(Users.Instance.Get(username), media, foundVersion, start_time.GetValueOrDefault(0), 0);
        return new(new
        {
            UUID = stream.StartTime.ToString(),
        });
    }

    [HttpGet("get/version")]
    public FileStreamResult GetVideoStream(string id, string username, string library, string version, int? season, int? episode, int? start_time, long? startTick)
    {
        MediaBase media = library.Equals("movies") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        if (config.UseVersionFile)
            return File(new FileStream(Paths.GetVersionPath(Directory.GetParent(media.Metadata.PATH).FullName, media.Title, foundVersion.Height, foundVersion.BitRate), FileMode.Open, FileAccess.Read), "video/mp4", true);
        else
        {
            MediaStream stream = ActiveStreams.Instance.Get(Users.Instance.Get(username), foundVersion, startTick.GetValueOrDefault(0)) ?? Transcoder.GetTranscodedStream(Users.Instance.Get(username), media, foundVersion, start_time.GetValueOrDefault(0), startTick.GetValueOrDefault(0));
            stream.ResetTimeout();

            System.Net.Mime.ContentDisposition cd = new()
            {
                FileName = "stream.m3u8",
                Inline = false,
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            stream.FileStream = new(Directory.GetFiles(stream.WorkingDirectory, "*.m3u8")[0], FileMode.Open, FileAccess.Read);
            return File(stream.FileStream, "application/x-mpegURL", true);
        }
    }

    [HttpGet("get/{file}")]
    public IActionResult GetStreamPart(string file)
    {
        string[] files = Directory.GetFiles(Paths.TempData, file, SearchOption.AllDirectories);
        if (files.Length == 0) return BadRequest();
        try
        {
            return File(new FileStream(files[0], FileMode.Open, FileAccess.Read), "video/MP2T", true);
        }
        catch (Exception e)
        {
            log.Error($"Cannot access Stream Part", e);
            return BadRequest();
        }
    }

    [HttpGet("get/stream_info")]
    public JsonResult GetStreamInfo(string id, string username, string library, long startTime, string version, int? season, int? episode)
    {
        MediaBase media = library.Equals("movies") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault(0)).GetEpisodeByNumber(episode.GetValueOrDefault(0)) : null;
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        int ts = 0;
        MediaStream stream = ActiveStreams.Instance.Get(Users.Instance.Get(username), foundVersion, startTime);
        if (stream != null)
        {
            if (Directory.Exists(stream.WorkingDirectory))
            {
                ts = Directory.GetFiles(stream.WorkingDirectory, "*.ts", SearchOption.TopDirectoryOnly).Length;
            }
            stream.ResetTimeout();
        }
        return new(new
        {
            mime = config.UseVersionFile ? "video/mp4" : "application/x-mpegURL",
            currentPosition = ts == 0 ? 0 : ts * 10,
            maxPosition = media.MediaInfo.Duration.TotalSeconds,
        });
    }

    [HttpPost("remove")]
    public IActionResult RemoveFromActiveStream([FromForm] string id, [FromForm] string username, [FromForm] string version, [FromForm] long startTime, [FromForm] string library, [FromForm] int? season, [FromForm] int? episode)
    {
        MediaBase media = library.Equals("movies") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        if (media == null)
        {
            log.Error($"No Media File from Library \"{library}\" and an ID of \"{id}\"");
            return BadRequest(new { message = $"No Media File from Library \"{library}\" and an ID of \"{id}\"" });
        }
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        var stream = ActiveStreams.Instance.Get(Users.Instance.Get(username), foundVersion, startTime);
        if (stream == null)
        {
            log.Warn("Stream Info",
                $"ID: {id}",
                $"Username: {username}",
                $"Version: {version}",
                $"StartTime: {startTime}",
                $"Library: {library}"
                );
            log.Error($"Unable to Remove Active Stream because stream provided was null");
            return BadRequest();
        }
        stream.KillAsync();
        return Ok(new { message = "Stream Successfully Removed" });
    }
}