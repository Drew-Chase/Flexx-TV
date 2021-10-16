using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
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

        public IActionResult Index()
        {
            return new JsonResult(MovieLibraryModel.Instance.GetList());
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
                IVideoStreamInfo streamInfo = new YoutubeClient().Videos.Streams.GetManifestAsync(((JObject)JsonConvert.DeserializeObject(new System.Net.WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}/videos?api_key={TMDB_API}")))["results"][0]["key"].ToString()).Result.GetMuxedStreams().GetWithHighestVideoQuality();
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

        [HttpGet("{tmdb}")]
        public IActionResult GetMovie(string tmdb)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
                if (movie == null)
                {
                    using System.Net.WebClient client = new();
                    JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}"));
                    string mpaa = string.Empty;

                    foreach (JToken child in ((JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}/release_dates?api_key={TMDB_API}")))["results"].Children().ToList())
                    {
                        try
                        {
                            if (child["iso_3166_1"].ToString().ToLower().Equals("us"))
                            {
                                mpaa = child["release_dates"][0]["certification"].ToString();
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    return new JsonResult(new
                    {
                        id = tmdb,
                        title = json["title"].ToString(),
                        plot = json["overview"],
                        year = DateTime.Parse(json["release_date"].ToString()).Year,
                        release_date = DateTime.Parse(json["release_date"].ToString()).ToString("MM-dd-yyyy"),
                        rating = json["vote_average"],
                        mpaa = mpaa,
                        watched = false,
                        watched_duration = 0,
                        downloaded = false,
                    });
                }
                return new JsonResult(movie.ModelObject);
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("recently-added")]
        public IActionResult GetRecentlyAddedMovies()
        {
            return new JsonResult(MovieLibraryModel.Instance.GetRecentlyAddedList());
        }

        [HttpGet("continue-watching")]
        public IActionResult GetContinueWatchingMovies()
        {
            return new JsonResult(MovieLibraryModel.Instance.GetContinueWatchingList());
        }

        [HttpGet("{tmdb}/images/poster")]
        public IActionResult GetMoviePoster(string tmdb)
        {
            MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
            if (movie == null)
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}"));
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
                    using System.Net.WebClient client = new();
                    JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}"));
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