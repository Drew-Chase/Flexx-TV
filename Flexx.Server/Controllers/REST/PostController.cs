using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Microsoft.AspNetCore.Mvc;

namespace Flexx.Server.Controllers;

[ApiController]
[Route("/api/post/")]
public class PostController : ControllerBase
{
    #region Public Methods

    /// <summary>
    /// Sets if media object has been watched or not.
    /// </summary>
    /// <param name="id">       The TMDB ID of the Movie or Series </param>
    /// <param name="library">  Rather its a Movie or Series </param>
    /// <param name="username"> The requesting users username or email </param>
    /// <param name="watched">  If its watched or not. </param>
    /// <param name="season">  
    /// If Library parameter is set to "tv" then this will be the shows season
    /// </param>
    /// <param name="episode"> 
    /// If Library parameter is set to "tv" then this will be the shows episode
    /// </param>
    /// <returns> </returns>
    [HttpPost("watched")]
    public IActionResult SetWatched([FromForm] string id, [FromForm] string library, [FromForm] string username, [FromForm] bool watched, [FromForm] int? season, [FromForm] int? episode)
    {
        User user = Users.Instance.Get(username);
        if (library.Equals("tv"))
        {
            var tv = TvLibraryModel.Instance.GetShowByTMDB(id);
            if (tv == null)
                return BadRequest();
            if (!season.HasValue && !episode.HasValue)
            {
                tv.MarkAsWatched(user);
                return Ok();
            }
            else if (season.HasValue && !episode.HasValue)
            {
                var s = tv.GetSeasonByNumber(season.Value);
                if (s == null)
                    return BadRequest();
                s.MarkAsWatched(user);
                return Ok();
            }
        }
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") && season.HasValue && episode.HasValue ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.Value).GetEpisodeByNumber(episode.Value) : null;
        if (media == null)
            return BadRequest(new { message = $"Movie with id of \"{id}\" does not exist" });
        user.SetHasWatched(media, watched);
        user.SetWatchedDuration(media, 0);
        return Ok();
    }

    /// <summary>
    /// Sets the watch duration of the media object.
    /// </summary>
    /// <param name="id">       The TMDB ID of the Movie or Series </param>
    /// <param name="library">  Rather its a Movie or Series </param>
    /// <param name="username"> The requesting users username or email </param>
    /// <param name="duration"> The current watched duration. </param>
    /// <param name="season">  
    /// If Library parameter is set to "tv" then this will be the shows season
    /// </param>
    /// <param name="episode"> 
    /// If Library parameter is set to "tv" then this will be the shows episode
    /// </param>
    /// <returns> </returns>
    [HttpPost("watched_duration")]
    public IActionResult SetWatchedDuration([FromForm] string id, [FromForm] string library, [FromForm] string username, [FromForm] int duration, [FromForm] int? season, [FromForm] int? episode)
    {
        MediaBase media = library.Equals("movie") ? MovieLibraryModel.Instance.GetMovieByTMDB(id) : library.Equals("tv") && season.HasValue && episode.HasValue ? TvLibraryModel.Instance.GetShowByTMDB(id).GetSeasonByNumber(season.Value).GetEpisodeByNumber(episode.Value) : null;
        if (media == null)
            return BadRequest(new { message = $"Movie with id of \"{id}\" does not exist" });
        if (duration > ushort.MaxValue)
            return BadRequest(new { message = $"A duration of \"{duration}\" was too long.  Duration max possible value is {ushort.MaxValue}" });
        Users.Instance.Get(username).SetWatchedDuration(media, (ushort) duration);
        return Ok();
    }

    #endregion Public Methods
}