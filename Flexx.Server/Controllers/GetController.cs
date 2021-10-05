using System;
using System.IO;
using Flexx.Core.Data.Exceptions;
using Flexx.Media.Interfaces;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using static Flexx.Core.Data.Global;

namespace Flexx.WebAssembly.Controllers
{
    [ApiController]
    [Route("/api/get/")]
    public class GetController : ControllerBase
    {
        [HttpGet("images/missing-artwork")]
        public IActionResult GetMissingArtworkPoster() => File(new FileStream(Paths.MissingPoster, FileMode.Open), "image/jpg");
        #region Movies
        [HttpGet("movies")]
        public IActionResult GetMovies()
        {
            return new JsonResult(MovieLibraryModel.Instance.GetList());
        }
        [HttpGet("movies/discover/{category}")]
        public IActionResult GetMovieDiscoveryList(DiscoveryCategory category)
        {
            object[] results = MovieLibraryModel.DiscoverMovies(category);
            if (results == null)
                return new JsonResult(new { message = $"No Results" });
            return new JsonResult(results);
        }
        [HttpGet("movies/discover/search/{query}/{year?}")]
        public IActionResult GetMovieDiscoveryFromQuery(string query, int? year)
        {
            object[] results = MovieLibraryModel.SearchForMovies(query, year.HasValue ? year.Value : -1);
            if (results == null)
                return new JsonResult(new { message = $"No Results" });
            return new JsonResult(results);

        }
        [HttpGet("movies/{tmdb}")]
        public IActionResult GetMovie(string tmdb)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
                return new JsonResult(new
                {
                    id = movie.TMDB,
                    title = movie.Title,
                    plot = movie.Plot,
                    year = movie.ReleaseDate.Year,
                    release_date = movie.ReleaseDate.ToString("MM-dd-yyyy"),
                    rating = movie.Rating,
                    mpaa = movie.MPAA,
                });
            }
            catch (MediaNotFoundException)
            {
                log.Error($"Movie with the TMDB ID of {tmdb} was not loaded");
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }
        [HttpGet("movies/{tmdb}/images/poster")]
        public IActionResult GetMoviePoster(string tmdb)
        {
            try
            {
                return new FileStreamResult(new FileStream(MovieLibraryModel.Instance.GetMovieByTMDB(tmdb).PosterImage, FileMode.Open), "image/jpg");
            }
            catch (MediaNotFoundException)
            {
                log.Error($"Movie with the TMDB ID of {tmdb} was not loaded");
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }
        [HttpGet("movies/{tmdb}/images/cover")]
        public IActionResult GetMovieCover(string tmdb)
        {
            try
            {
                return File(new FileStream(MovieLibraryModel.Instance.GetMovieByTMDB(tmdb).CoverImage, FileMode.Open), "image/jpg");
            }
            catch (MediaNotFoundException)
            {
                log.Error($"Movie with the TMDB ID of {tmdb} was not loaded");
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }
        [HttpGet("movies/{tmdb}/{user}/video/{resolution?}/{bitrate?}")]
        public IActionResult GetMovieStream(string tmdb, string user, int? resolution, int? bitrate)
        {
            IMedia movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
            if (resolution.HasValue && bitrate.HasValue)
                return File(FFMpegUtil.GetTranscodedStream(user, movie, resolution.Value, bitrate.Value), "application/x-mpegURL", true);
            return File(movie.Stream, "video/mp4", true);
        }
        #endregion

        #region TV
        [HttpGet("tv")]
        public IActionResult GetShows()
        {
            return new JsonResult(TvLibraryModel.Instance.GetList());
        }
        [HttpGet("tv/{tmdb}")]
        public IActionResult GetShow(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            return new JsonResult(new
            {
                id = show.TMDB,
                title = show.Title,
                plot = show.Plot,
                year = show.StartDate.Year,
                release_date = show.StartDate.ToString("MM-dd-yyyy"),
            });
        }
        [HttpGet("tv/{tmdb}/images/poster")]
        public IActionResult GetShowPoster(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            return File(new FileStream(show.PosterImage, FileMode.Open), "image/jpg");
        }
        [HttpGet("tv/{tmdb}/images/cover")]
        public IActionResult GetShowCover(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            return File(new FileStream(show.CoverImage, FileMode.Open), "image/jpg");
        }
        #region Season
        [HttpGet("tv/{tmdb}/{season_number}")]
        public IActionResult GetSeason(string tmdb, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            SeasonModel season = show.GetSeasonByNumber(season_number);
            if (season == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" doesn't contain a season numbered {season_number}" });
            return new JsonResult(new
            {
                title = season.Title,
                plot = season.Plot,
                start_date = season.StartDate.ToString("MM-dd-yyyy"),
            });
        }
        [HttpGet("tv/{tmdb}/{season_number}/poster")]
        public IActionResult GetSeasonPoster(string tmdb, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            SeasonModel season = show.GetSeasonByNumber(season_number);
            if (season == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" doesn't contain a season numbered {season_number}" });
            return File(new FileStream(season.PosterImage, FileMode.Open), "image/jpg");
        }
        #region Episodes
        [HttpGet("tv/{tmdb}/{season_number}/{episode_number}")]
        public IActionResult GetEpisode(string tmdb, int season_number, int episode_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            SeasonModel season = show.GetSeasonByNumber(season_number);
            if (season == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" doesn't contain a season numbered {season_number}" });
            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            if (episode == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" and season #{season_number} doesn't contain episode numbered {episode_number}" });
            return new JsonResult(new
            {
                title = episode.Title,
                plot = episode.Plot,
                release_date = episode.ReleaseDate.ToString("MM-dd-yyyy"),
            });
        }
        [HttpGet("tv/{tmdb}/{season_number}/{episode_number}/poster")]
        public IActionResult GetEpisodeStill(string tmdb, int season_number, int episode_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            SeasonModel season = show.GetSeasonByNumber(season_number);
            if (season == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" doesn't contain a season numbered {season_number}" });
            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            if (episode == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" and season #{season_number} doesn't contain episode numbered {episode_number}" });
            return File(new FileStream(episode.PosterImage, FileMode.Open), "image/jpg");
        }
        [HttpGet("tv/{tmdb}/{user}/{season_number}/{episode_number}/video/{resolution?}/{bitrate?}")]
        public IActionResult GetEpisodeStream(string tmdb, int season_number, int episode_number, string user, int? resolution, int? bitrate)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            SeasonModel season = show.GetSeasonByNumber(season_number);
            if (season == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" doesn't contain a season numbered {season_number}" });
            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            if (episode == null)
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" and season #{season_number} doesn't contain episode numbered {episode_number}" });

            if (resolution.HasValue && bitrate.HasValue)
                return File(FFMpegUtil.GetTranscodedStream(user, episode, resolution.Value, bitrate.Value), "application/x-mpegURL", true);
            return File(((IMedia)episode).Stream, "video/mp4", true);
        }
        #endregion
        #endregion
        #endregion
    }
}
