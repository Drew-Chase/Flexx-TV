using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Microsoft.AspNetCore.Mvc;

namespace Flexx.Server.Controllers;

[ApiController]
[Route("/api/post/movies/")]
public class PostMoviesController : ControllerBase
{
    [HttpPost("watched")]
    public IActionResult SetWatched([FromForm] string id, [FromForm] string username, [FromForm] bool watched)
    {
        MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
        if (movie == null)
            return BadRequest(new { message = $"Movie with id of \"{id}\" does not exist" });
        Users.Instance.Get(username).SetHasWatched(movie, watched);
        return Ok();
    }

    [HttpPost("watched_duration")]
    public IActionResult SetWatchedDuration([FromForm] string id, [FromForm] string username, [FromForm] int duration)
    {
        MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
        if (movie == null)
            return BadRequest(new { message = $"Movie with id of \"{id}\" does not exist" });
        if (duration > ushort.MaxValue)
            return BadRequest(new { message = $"A duration of \"{duration}\" was too long.  Duration max possible value is {ushort.MaxValue}" });
        Users.Instance.Get(username).SetWatchedDuration(movie, (ushort)duration);
        return Ok();
    }
}