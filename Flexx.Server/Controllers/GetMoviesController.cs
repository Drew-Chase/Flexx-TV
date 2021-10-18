using Flexx.Core.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using static Flexx.Core.Data.Global;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/get/movies/")]
    public class GetMoviesController : ControllerBase
    {
        #region Movies

        [HttpGet("{username}")]
        public IActionResult GetMovies(string username)
        {
            return new JsonResult(MovieLibraryModel.Instance.GetList(Users.Instance.Get(username)));
        }

        [HttpGet("discover/{category}")]
        public IActionResult GetMovieDiscoveryList(DiscoveryCategory category)
        {
            object[] results = MovieLibraryModel.Instance.DiscoverMovies(category);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("discover/search/{query}/{year?}")]
        public IActionResult GetMovieDiscoveryFromQuery(string query, int? year)
        {
            object[] results = MovieLibraryModel.Instance.SearchForMovies(query, year.HasValue ? year.Value : -1);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("{tmdb}/trailer")]
        public IActionResult GetMovieTrailer(string tmdb)
        {
            string trailerURL = "";
            MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
            if (movie == null)
            {
                IVideoStreamInfo streamInfo = new YoutubeClient().Videos.Streams.GetManifestAsync(((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}/videos?api_key={TMDB_API}")))["results"][0]["key"].ToString()).Result.GetMuxedStreams().GetWithHighestVideoQuality();
                if (streamInfo != null)
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
                trailerURL = movie.TrailerUrl;
            }

            if (string.IsNullOrWhiteSpace(trailerURL))
            {
                return new NotFoundResult();
            }

            return RedirectPermanent(trailerURL);
        }

        [HttpGet("{tmdb}/{username}")]
        public IActionResult GetMovie(string tmdb, string username)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
                if (movie == null)
                {
                    return new JsonResult(new MovieObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}")));
                }
                return new JsonResult(new MovieObject(movie, Users.Instance.Get(username)));
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("{username}/recently-added")]
        public IActionResult GetRecentlyAddedMovies(string username)
        {
            return new JsonResult(MovieLibraryModel.Instance.GetRecentlyAddedList(Users.Instance.Get(username)));
        }

        [HttpGet("{username}/continue-watching")]
        public IActionResult GetContinueWatchingMovies(string username)
        {
            return new JsonResult(MovieLibraryModel.Instance.GetContinueWatchingList(Users.Instance.Get(username)));
        }

        [HttpGet("{tmdb}/images/poster")]
        public IActionResult GetMoviePoster(string tmdb)
        {
            MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
            if (movie == null)
            {
                JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["poster_path"]}");
            }
            return new FileStreamResult(new FileStream(movie.PosterImage, FileMode.Open), "image/jpg");
        }

        [HttpGet("{tmdb}/images/cover")]
        public IActionResult GetMovieCover(string tmdb)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
                if (movie == null)
                {
                    JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}"));
                    return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["backdrop_path"]}");
                }
                return File(new FileStream(movie.CoverImage, FileMode.Open), "image/jpg");
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("{tmdb}/{user}/video/{resolution?}/{bitrate?}")]
        public IActionResult GetMovieStream(string tmdb, string user, int? resolution, int? bitrate)
        {
            MediaBase movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
            if (resolution.HasValue && bitrate.HasValue)
            {
                return File(FFMpegUtil.GetTranscodedStream(user, movie, resolution.Value, bitrate.Value), "application/x-mpegURL", true);
            }

            return File(movie.Stream, "video/mp4", true);
        }

        #endregion Movies
    }
}