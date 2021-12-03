using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Microsoft.AspNetCore.Mvc;

namespace Flexx.Server.Controllers;

[ApiController]
[Route("/api/post/movies/")]
public class PostController : ControllerBase
{
    [HttpPost("watched")]
    public IActionResult SetWatched([FromForm] string id, [FromForm] string library, [FromForm] string username, [FromForm] bool watched, [FromBody] int? season, [FromBody] int? episode)
    {
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") && season.HasValue && episode.HasValue ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.Value).GetEpisodeByNumber(episode.Value) : null;
        if (media == null)
            return BadRequest(new { message = $"Movie with id of \"{id}\" does not exist" });
        Users.Instance.Get(username).SetHasWatched(media, watched);
        return Ok();
    }

    [HttpPost("watched_duration")]
    public IActionResult SetWatchedDuration([FromForm] string id, [FromForm] string library, [FromForm] string username, [FromForm] int duration, [FromBody] int? season, [FromBody] int? episode)
    {
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") && season.HasValue && episode.HasValue ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.Value).GetEpisodeByNumber(episode.Value) : null;
        if (media == null)
            return BadRequest(new { message = $"Movie with id of \"{id}\" does not exist" });
        if (duration > ushort.MaxValue)
            return BadRequest(new { message = $"A duration of \"{duration}\" was too long.  Duration max possible value is {ushort.MaxValue}" });
        Users.Instance.Get(username).SetWatchedDuration(media, (ushort)duration);
        return Ok();
    }
}