using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Timers;
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
            return new JsonResult(MovieLibraryModel.Instance.GetLocalList(Users.Instance.Get(username)));
        }

        [HttpGet("{id}/{username}")]
        public IActionResult GetMovie(string id, string username)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
                if (movie == null)
                {
                    try
                    {
                        return new JsonResult(new MovieObject(new HttpClient().GetAsync($"https://api.themoviedb.org/3/movie/{id}?api_key={TMDB_API}").Result.Content.ReadAsStringAsync().Result));
                    }
                    catch
                    {
                        return new BadRequestResult();
                    }
                }
                return new JsonResult(new MovieObject(movie, Users.Instance.Get(username)));
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {id}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("discover/{category}")]
        public IActionResult GetMovieDiscoveryList(DiscoveryCategory category, string username)
        {
            object[] results = MovieLibraryModel.Instance.DiscoverMovies(Users.Instance.Get(username), category);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("discover/simular")]
        public IActionResult GetMovieDiscoveryFromSimularRequest(string id, string username)
        {
            object[] results = MovieLibraryModel.Instance.FindSimilar(id);
            if (results == null)
            {
                return this.GetMovieDiscoveryList(DiscoveryCategory.Popular, username);
            }

            return new JsonResult(results);
        }

        [HttpGet("discover/search")]
        public IActionResult GetMovieDiscoveryFromQuery(string query, int? year)
        {
            object[] results = MovieLibraryModel.Instance.SearchForMovies(query, year ?? -1);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("recently-added")]
        public IActionResult GetRecentlyAddedMovies(string username)
        {
            return new JsonResult(MovieLibraryModel.Instance.GetRecentlyAddedList(Users.Instance.Get(username)));
        }

        [HttpGet("continue-watching")]
        public IActionResult GetContinueWatchingMovies(string username)
        {
            return new JsonResult(MovieLibraryModel.Instance.GetContinueWatchingList(Users.Instance.Get(username)));
        }

        [HttpGet("{id}/images/poster")]
        public IActionResult GetMoviePoster(string id)
        {
            MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
            if (movie == null)
            {
                JArray json = (JArray)((JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}/images?api_key={TMDB_API}"))["posters"];
                if (json.Any())
                {
                    return new RedirectResult($"https://image.tmdb.org/t/p/original{json[0]["file_path"]}");
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(movie.PosterImage) && System.IO.File.Exists(movie.PosterImage))
                {
                    return new FileStreamResult(new FileStream(movie.PosterImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
                }
            }
            return new FileStreamResult(new FileStream(Paths.MissingPoster, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
        }

        [HttpGet("{id}/images/cover")]
        public IActionResult GetMovieCover(string id, bool? language)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
                if (movie == null)
                {
                    if (language.GetValueOrDefault())
                    {
                        JObject imagesJson = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}/images?api_key={TMDB_API}&include_image_language=en");
                        if (imagesJson["backdrops"].Any())
                        {
                            return new RedirectResult($"https://image.tmdb.org/t/p/original{imagesJson["backdrops"][0]["file_path"]}");
                        }
                        return new FileStreamResult(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
                    }

                    JObject json = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}?api_key={TMDB_API}");
                    return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["backdrop_path"]}");
                }
                string path = language.GetValueOrDefault() && !string.IsNullOrWhiteSpace(movie.CoverImageWithLanguage) ? movie.CoverImageWithLanguage : movie.CoverImage;
                if (System.IO.File.Exists(path))
                {
                    return File(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
                }
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {id}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("{id}/images/logo")]
        public IActionResult GetMovieLogo(string id)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
                if (movie == null)
                {
                    JObject imagesJson = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}/images?api_key={TMDB_API}&include_image_language=en");
                    if (imagesJson["logos"].Any())
                    {
                        return new RedirectResult($"https://image.tmdb.org/t/p/original{imagesJson["logos"][0]["file_path"]}");
                    }

                    return new NotFoundResult();
                }
                if (!string.IsNullOrWhiteSpace(movie.LogoImage) && System.IO.File.Exists(movie.LogoImage))
                {
                    return File(new FileStream(movie.LogoImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
                }
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {id}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("{id}/trailer")]
        public IActionResult GetMovieTrailer(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "ID cannot be empty" });
            MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
            string trailerURL = string.Empty;
            if (movie == null)
            {
                object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}/videos?api_key={TMDB_API}");
                if (jresult == new { }) return BadRequest();
                JToken results = ((JObject)jresult)["results"];
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

        #endregion Movies
    }
}