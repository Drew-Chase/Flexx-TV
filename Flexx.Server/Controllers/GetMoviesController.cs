using Flexx.Core.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
using Microsoft.AspNetCore.Mvc;
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
            return new JsonResult(MovieLibraryModel.Instance.GetLocalList(Users.Instance.Get(username)));
        }

        [HttpGet("{tmdb}/{username}")]
        public IActionResult GetMovie(string tmdb, string username)
        {
            try
            {
                MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
                if (movie == null)
                {
                    try
                    {
                        MovieObject model = new(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}"));
                        return new JsonResult(new MovieObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{tmdb}?api_key={TMDB_API}")));
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
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
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

        [HttpGet("images/poster")]
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
                return File(new FileStream(Paths.MissingPoster, FileMode.Open), "image/jpg");
            }
            return new FileStreamResult(new FileStream(movie.PosterImage, FileMode.Open), "image/jpg");
        }

        [HttpGet("images/cover")]
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
                    }

                    JObject json = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}?api_key={TMDB_API}");
                    return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["backdrop_path"]}");
                }
                string path = language.GetValueOrDefault() && !string.IsNullOrWhiteSpace(movie.CoverImageWithLanguage) ? movie.CoverImageWithLanguage : movie.CoverImage;
                if (System.IO.File.Exists(path))
                {
                    return File(new FileStream(path, FileMode.Open), "image/jpg");
                }
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {id}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("images/logo")]
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
                    return File(new FileStream(movie.LogoImage, FileMode.Open), "image/jpg");
                }
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {id}", e);
            }
            return new NotFoundResult();
        }

        [HttpGet("trailer")]
        public IActionResult GetMovieTrailer(string id)
        {
            MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
            string trailerURL = string.Empty;
            if (movie == null)
            {
                object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}/videos?api_key={TMDB_API}");
                if (jresult == null) return BadRequest();
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

        [HttpGet("video")]
        public IActionResult GetMovieStream(string id, string resolution)
        {
            MediaBase movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
            if (movie == null) return new NotFoundResult();
            if (!string.IsNullOrWhiteSpace(resolution))
            {
                MediaVersion version = movie.AlternativeVersions.FirstOrDefault(d => d.DisplayName.Equals(resolution));
                string version_file = Paths.GetVersionPath(Directory.GetParent(movie.Metadata.PATH).FullName, movie.Title, version.Width, version.BitRate);
                if (System.IO.File.Exists(version_file))
                {
                    string dir = Directory.CreateDirectory(Path.Combine(Paths.TempData, $"m{id}_{resolution}")).FullName;
                    string[] files = Directory.GetFiles(dir, "*.mp4", SearchOption.TopDirectoryOnly);
                    string tempFile = "";
                    foreach (string file in files)
                    {
                        if (!Functions.IsFileLocked(new FileInfo(file)))
                        {
                            tempFile = file;
                        }
                    }
                    if (string.IsNullOrEmpty(tempFile))
                    {
                        tempFile = Path.Combine(dir, $"{files.Length}.mp4");
                        System.IO.File.Copy(version_file, tempFile);
                    }
                    return File(System.IO.File.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read), "mp4/video", true);
                }
            }
            return File(movie.Stream, "video/mp4", true);
        }

        //[HttpGet("{tmdb}/{user}/video/transcoded")]
        //public IActionResult GetMovieStream(string tmdb, string user, int resolution, int bitrate)
        //{
        //    MediaBase movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
        //    if (movie == null) return new NotFoundResult();
        //    var (transcoded, process) = Transcoder.GetTranscodedStream(user, movie, resolution, bitrate);
        //    Timer timer = new(10 * 1000)
        //    {
        //        AutoReset = true,
        //        Enabled = true,
        //    };
        //    timer.Elapsed += (s, e) =>
        //    {
        //        process.Kill();
        //    };
        //    long fileSize = new FileInfo(movie.PATH).Length;
        //    log.Fatal($"Content-Length: {fileSize}");
        //    log.Fatal(fileSize.ToString());
        //    int duration = (int)Math.Ceiling(movie.MediaInfo.Duration.TotalSeconds);
        //    Response.Headers.Clear();
        //    Response.ContentLength = fileSize;
        //    Response.Headers.Add("Accept-Ranges", $"bytes");
        //    Response.Headers.Add("Content-Range", $"bytes {0}-{fileSize}/{fileSize}");
        //    activeStreams.Add(Users.Instance.Get(user), timer);
        //    return File(transcoded, "application/x-mpegURL", true);
        //    //return RedirectPermanent("http://127.0.0.1:1234");
        //}

        //[HttpGet("{tmdb}/{user}/video/transcoded/stillwatching")]
        //public IActionResult MarkAsStillWatching(string tmdb, string user)
        //{
        //    MediaBase movie = MovieLibraryModel.Instance.GetMovieByTMDB(tmdb);
        //    if (movie == null) return new NotFoundResult();
        //    if (activeStreams.TryGetValue(Users.Instance.Get(user), out Timer value))
        //    {
        //        double interval = value.Interval;
        //        value.Stop();
        //        value.Interval = interval;
        //        value.Start();
        //    }
        //    return new OkResult();
        //}

        #endregion Movies
    }
}