using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
using Flexx.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using static Flexx.Data.Global;

namespace Flexx.Server.Controllers;

[ApiController]
[Route("/api/stream/")]
public class StreamController : ControllerBase
{
    #region Public Methods

    /// <summary>
    /// Returns all Active Streams with necessary information
    /// </summary>
    /// <returns> </returns>
    [HttpGet("get/active")]
    public JsonResult GetAllActiveStreams()
    {
        List<object> json = new();
        foreach (MediaStream stream in ActiveStreams.Instance.Get())
        {
            json.Add(new
            {
                ID = stream.MediaID,
                Platform = stream.Platform.ToString(),
                State = stream.CurrentState.ToString(),
                stream.Type,
                stream.CurrentPosition,
                Episode = stream.Episode.GetValueOrDefault(-1),
                Season = stream.Season.GetValueOrDefault(-1),
                Resolution = new
                {
                    Name = stream.Version.DisplayName,
                    BitRate = $"{Math.Round(stream.Version.BitRate / 1000d, 2)}Mbps"
                },
            });
        }
        return new(json);
    }

    /// <summary>
    /// Retrieves the trailer direct URL from YouTube/Google Video.
    /// </summary>
    /// <param name="id"> </param>
    /// <returns> </returns>
    [HttpGet("trailer")]
    public IActionResult GetMovieTrailer(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "ID cannot be empty" });
        MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
        string trailerURL = string.Empty;
        if (movie == null)
        {
            if (Functions.TryGetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}/videos?api_key={TMDB_API}", out JObject jresult))
            {
                JToken results = jresult["results"];
                if (results.Any())
                {
                    JToken keyObject = results[0]["key"];
                    string key = (string) keyObject;
                    IVideoStreamInfo streamInfo = new YoutubeClient().Videos.Streams.GetManifestAsync(key).Result.GetMuxedStreams().GetWithHighestVideoQuality();
                    if (!string.IsNullOrWhiteSpace(key) && streamInfo != null)
                    {
                        trailerURL = streamInfo.Url;
                    }
                    else
                    {
                        trailerURL = "";
                    }
                }
                else
                {
                    trailerURL = "";
                }
            }
            else
            {
                return BadRequest();
            }
        }
        else
        {
            if (movie.HasTrailer)
            {
                trailerURL = movie.TrailerUrl;
            }
        }

        if (string.IsNullOrWhiteSpace(trailerURL))
        {
            return new NotFoundResult();
        }

        return RedirectPermanent(trailerURL);
    }

    /// <summary>
    /// Gets the stream information needed for the video player
    /// </summary>
    /// <param name="id">        The TMDB ID of the Movie or Series </param>
    /// <param name="username">  The requesting users username or email </param>
    /// <param name="library">   Rather its a Movie or Series </param>
    /// <param name="platform"> 
    /// The Video Players Platform, <see cref="Platform"> Platform List </see>
    /// </param>
    /// <param name="startTime"> what position the video should start at </param>
    /// <param name="version">  
    /// The quality setting that the stream is set to. See <see cref="MediaVersion"> Versions </see>
    /// </param>
    /// <param name="season">   
    /// If Library parameter is set to "tv" then this will be the shows season
    /// </param>
    /// <param name="episode">  
    /// If Library parameter is set to "tv" then this will be the shows episode
    /// </param>
    /// <returns> </returns>
    [HttpGet("get/stream_info")]
    public JsonResult GetStreamInfo(string id, string username, string library, Platform platform, long startTime, string version, int? season, int? episode)
    {
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault(0)).GetEpisodeByNumber(episode.GetValueOrDefault(0)) : null;
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        int ts = 0;
        MediaStream stream = ActiveStreams.Instance.Get(Users.Instance.Get(username), foundVersion, startTime, platform);
        if (stream != null)
        {
            if (Directory.Exists(stream.WorkingDirectory))
            {
                ts = Directory.GetFiles(stream.WorkingDirectory, "*.ts", SearchOption.TopDirectoryOnly).Length;
            }
            stream.ResetTimeout();
            return new(new
            {
                mime = config.UseVersionFile ? "video/mp4" : "application/x-mpegURL",
                currentTranscodedPosition = ts == 0 ? 0 : ts * 10,
                maxPosition = media.MediaInfo.Duration.TotalSeconds,
                currentPosition = stream.CurrentPosition,
                currentPlayState = stream.CurrentState,
            });
        }

        return new(new
        {
            mime = config.UseVersionFile ? "video/mp4" : "application/x-mpegURL",
            currentTranscodedPosition = ts == 0 ? 0 : ts * 10,
            maxPosition = media.MediaInfo.Duration.TotalSeconds,
        });
    }

    /// <summary>
    /// Returns the HLS stream part from the temp directory
    /// </summary>
    /// <param name="file"> </param>
    /// <returns> </returns>
    [HttpGet("get/{file}")]
    public IActionResult GetStreamPart(string file)
    {
        string[] files = Directory.GetFiles(Paths.TempData, file, SearchOption.AllDirectories);
        if (files.Length == 0)
            return BadRequest();
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

    /// <summary>
    /// Will return the HLS stream file
    /// </summary>
    /// <param name="id">         The TMDB ID of the Movie or Series </param>
    /// <param name="username">   The requesting users username or email </param>
    /// <param name="library">    Rather its a Movie or Series </param>
    /// <param name="platform">  
    /// The Video Players Platform, <see cref="Platform"> Platform List </see>
    /// </param>
    /// <param name="start_time"> what position the video should start at </param>
    /// <param name="version">   
    /// The quality setting that the stream is set to. See <see cref="MediaVersion"> Versions </see>
    /// </param>
    /// <param name="season">    
    /// If Library parameter is set to "tv" then this will be the shows season
    /// </param>
    /// <param name="episode">   
    /// If Library parameter is set to "tv" then this will be the shows episode
    /// </param>
    /// <param name="startTick">  The time-stamp when the stream started </param>
    /// <returns> </returns>
    [HttpGet("get/version")]
    public IActionResult GetVideoStream(string id, string username, string library, string version, Platform platform, int? season, int? episode, int? start_time, long? startTick)
    {
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        if (config.UseVersionFile)
            return File(new FileStream(Paths.GetVersionPath(Directory.GetParent(media.Metadata.PATH).FullName, media.Title, foundVersion.Height, foundVersion.BitRate), FileMode.Open, FileAccess.Read), "video/mp4", true);
        else
        {
            MediaStream stream = ActiveStreams.Instance.Get(Users.Instance.Get(username), foundVersion, startTick.GetValueOrDefault(0), platform) ?? Transcoder.GetTranscodedStream(Users.Instance.Get(username), media, foundVersion, start_time.GetValueOrDefault(0), startTick.GetValueOrDefault(0), platform);
            if (stream == null)
            {
                return BadRequest();
            }
            stream.ResetTimeout();

            System.Net.Mime.ContentDisposition cd = new()
            {
                FileName = "stream.m3u8",
                Inline = false,
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            if (stream.FileStream != null)
                stream.FileStream.Dispose();
            stream.FileStream = new(Directory.GetFiles(stream.WorkingDirectory, "*.m3u8")[0], FileMode.Open, FileAccess.Read);
            return File(stream.FileStream, "application/x-mpegURL", true);
        }
    }

    /// <summary>
    /// Removes a currently active stream and disposes of its resources
    /// </summary>
    /// <param name="id">        The TMDB ID of the Movie or Series </param>
    /// <param name="username">  The requesting users username or email </param>
    /// <param name="library">   Rather its a Movie or Series </param>
    /// <param name="platform"> 
    /// The Video Players Platform, <see cref="Platform"> Platform List </see>
    /// </param>
    /// <param name="startTime"> The time-stamp when the stream started </param>
    /// <param name="version">  
    /// The quality setting that the stream is set to. See <see cref="MediaVersion"> Versions </see>
    /// </param>
    /// <param name="season">   
    /// If Library parameter is set to "tv" then this will be the shows season
    /// </param>
    /// <param name="episode">  
    /// If Library parameter is set to "tv" then this will be the shows episode
    /// </param>
    /// <param name="startTime"> The time-stamp when the stream started </param>
    /// <returns> </returns>
    [HttpPost("remove")]
    public IActionResult RemoveFromActiveStream([FromForm] string id, [FromForm] string username, [FromForm] string version, [FromForm] Platform platform, [FromForm] long startTime, [FromForm] string library, [FromForm] int? season, [FromForm] int? episode)
    {
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        if (media == null)
        {
            log.Error($"No Media File from Library \"{library}\" and an ID of \"{id}\"");
            return BadRequest(new { message = $"No Media File from Library \"{library}\" and an ID of \"{id}\"" });
        }
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        MediaStream stream = ActiveStreams.Instance.Get(Users.Instance.Get(username), foundVersion, startTime, platform);
        if (stream == null)
        {
            log.Error($"Unable to Remove Active Stream because stream provided was null");
            return BadRequest();
        }
        stream.KillAsync();
        return Ok(new { message = "Stream Successfully Removed" });
    }

    /// <summary>
    /// Will start a new stream with specified parameters
    /// </summary>
    /// <param name="id">         The TMDB ID of the Movie or Series </param>
    /// <param name="username">   The requesting users username or email </param>
    /// <param name="library">    Rather its a Movie or Series </param>
    /// <param name="version">   
    /// The quality setting that the stream is set to. See <see cref="MediaVersion"> Versions </see>
    /// </param>
    /// <param name="platform">  
    /// The Video Players Platform, <see cref="Platform"> Platform List </see>
    /// </param>
    /// <param name="season">    
    /// If Library parameter is set to "tv" then this will be the shows season
    /// </param>
    /// <param name="episode">   
    /// If Library parameter is set to "tv" then this will be the shows episode
    /// </param>
    /// <param name="start_time"> The time-stamp when the stream started </param>
    /// <returns> </returns>
    [HttpGet("start")]
    public JsonResult StartStream(string id, string username, string library, string version, Platform platform, int? season, int? episode, int? start_time)
    {
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        MediaStream stream = Transcoder.GetTranscodedStream(Users.Instance.Get(username), media, foundVersion, start_time.GetValueOrDefault(0), 0, platform);
        if (stream == null)
        {
            return new(new
            {
                message = "Stream failed to start"
            });
        }
        return new(new
        {
            UUID = stream.StartTime,
        });
    }

    /// <summary>
    /// Will update servers information on the active stream. <br/> If not polled every 15 seconds
    /// stream will auto kill itself.
    /// </summary>
    /// <param name="id">              The TMDB ID of the Movie or Series </param>
    /// <param name="username">        The requesting users username or email </param>
    /// <param name="version">        
    /// The quality setting that the stream is set to. See <see cref="MediaVersion"> Versions </see>
    /// </param>
    /// <param name="startTime">       </param>
    /// <param name="platform">       
    /// The Video Players Platform, <see cref="Platform"> Platform List </see>
    /// </param>
    /// <param name="library">         Rather its a Movie or Series </param>
    /// <param name="season">         
    /// If Library parameter is set to "tv" then this will be the shows season
    /// </param>
    /// <param name="episode">        
    /// If Library parameter is set to "tv" then this will be the shows episode
    /// </param>
    /// <param name="state">          
    /// If the video is paused, playing, etc. See <see cref="PlayState"> Available States </see>
    /// </param>
    /// <param name="currentPosition"> the current play position of the stream </param>
    /// <returns> </returns>
    [HttpPost("update")]
    public IActionResult UpdateStream([FromForm] string id, [FromForm] string username, [FromForm] string version, [FromForm] long startTime, [FromForm] Platform platform, [FromForm] string library, [FromForm] int? season, [FromForm] int? episode, [FromForm] PlayState state, [FromForm] int currentPosition)
    {
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.GetValueOrDefault()).GetEpisodeByNumber(episode.GetValueOrDefault()) : null;
        if (media == null)
        {
            log.Error($"No Media File from Library \"{library}\" and an ID of \"{id}\"");
            return BadRequest(new { message = $"No Media File from Library \"{library}\" and an ID of \"{id}\"" });
        }
        MediaVersion foundVersion = media.AlternativeVersions.FirstOrDefault(m => m.DisplayName.Equals(version), media.AlternativeVersions[0]);
        MediaStream stream = ActiveStreams.Instance.Get(Users.Instance.Get(username), foundVersion, startTime, platform);
        if (stream == null)
        {
            return BadRequest();
        }
        stream.UpdateState(state);
        stream.UpdatePlayPosition(currentPosition);
        stream.ResetTimeout();
        return Ok(new { message = "Stream Successfully Updated" });
    }

    #endregion Public Methods
}