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
using System.Linq;
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

        [HttpGet("discover/search/{query}")]
        public IActionResult GetMovieDiscoveryFromQuery(string query, int? year)
        {
            object[] results = MovieLibraryModel.Instance.SearchForMovies(query, year ?? -1);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("recently-added/{username}")]
        public IActionResult GetRecentlyAddedMovies(string username)
        {
            return new JsonResult(MovieLibraryModel.Instance.GetRecentlyAddedList(Users.Instance.Get(username)));
        }

        [HttpGet("continue-watching/{username}")]
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
                JArray json = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}/images?api_key={TMDB_API}")))["posters"];
                if (json.Any())
                {
                    return new RedirectResult($"https://image.tmdb.org/t/p/original{json[0]["file_path"]}");
                }
                return File(new FileStream(Paths.MissingPoster, FileMode.Open), "image/jpg");

            }
            return new FileStreamResult(new FileStream(movie.PosterImage, FileMode.Open), "image/jpg");
        }

        [HttpGet("{tmdb}/images/cover")]
        public IActionResult GetMovieCover(string tmdb, bool? language)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
                if (movie == null)
                {
                    if (language.GetValueOrDefault())
                    {
                        JObject imagesJson = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}/images?api_key={TMDB_API}&include_image_language=en"));
                        if (imagesJson["backdrops"].Any())
                        {
                            return new RedirectResult($"https://image.tmdb.org/t/p/original{imagesJson["backdrops"][0]["file_path"]}");
                        }
                    }

                    JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}"));
                    return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["backdrop_path"]}");
                }
                return File(new FileStream(language.GetValueOrDefault() && !string.IsNullOrWhiteSpace(movie.CoverImageWithLanguage) ? movie.CoverImageWithLanguage : movie.CoverImage, FileMode.Open), "image/jpg");
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("{tmdb}/images/logo")]
        public IActionResult GetMovieLogo(string tmdb)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
                if (movie == null)
                {
                    JObject imagesJson = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}/images?api_key={TMDB_API}&include_image_language=en"));
                    if (imagesJson["logos"].Any())
                    {
                        return new RedirectResult($"https://image.tmdb.org/t/p/original{imagesJson["logos"][0]["file_path"]}");
                    }

                    return new NotFoundResult();
                }
                if (string.IsNullOrWhiteSpace(movie.LogoImage))
                {
                    return new NotFoundResult();
                }

                return File(new FileStream(movie.LogoImage, FileMode.Open), "image/jpg");
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("{tmdb}/trailer")]
        public IActionResult GetMovieTrailer(string tmdb)
        {
            MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
            string trailerURL = string.Empty;
            if (movie == null)
            {
                JToken results = ((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}/videos?api_key={TMDB_API}")))["results"];
                if (results.Any())
                {
                    JToken keyObject = results[0]["key"];
                    string key = keyObject.ToString();
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

        [HttpGet("{tmdb}/{user}/video")]
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