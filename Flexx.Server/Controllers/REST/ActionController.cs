using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/action")]
    public class ActionController : ControllerBase
    {
        #region Public Methods

        [HttpGet("movies/add/{tmdb}")]
        public IActionResult AddMovie(string tmdb)
        {
            try
            {
                MovieModel movie = new(tmdb, true);
                MovieLibraryModel.Instance.AddMedia(movie);
                Task.Run(() => movie.AddToTorrentClient());
            }
            catch
            {
                return new BadRequestResult();
            }
            return new OkResult();
        }

        #endregion Public Methods
    }
}