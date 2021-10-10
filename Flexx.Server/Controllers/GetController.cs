using System;
using System.IO;
using System.Linq;
using System.Xml;
using Flexx.Core.Networking;
using Flexx.Media.Interfaces;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using static Flexx.Core.Data.Global;

namespace Flexx.Server.Controllers
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
            object[] results = MovieLibraryModel.Instance.DiscoverMovies(category);
            if (results == null)
                return new JsonResult(new { message = $"No Results" });
            return new JsonResult(results);
        }
        [HttpGet("movies/discover/search/{query}/{year?}")]
        public IActionResult GetMovieDiscoveryFromQuery(string query, int? year)
        {
            object[] results = MovieLibraryModel.Instance.SearchForMovies(query, year.HasValue ? year.Value : -1);
            if (results == null)
                return new JsonResult(new { message = $"No Results" });
            return new JsonResult(results);
        }
        [HttpGet("movies/{tmdb}/trailer")]
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
                return new NotFoundResult();
            return RedirectPermanent(trailerURL);

        }
        [HttpGet("movies/{tmdb}")]
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
                        downloaded = false,
                    });

                }
                return new JsonResult(new
                {
                    id = movie.TMDB,
                    title = movie.Title,
                    plot = movie.Plot,
                    year = movie.ReleaseDate.Year,
                    release_date = movie.ReleaseDate.ToString("MM-dd-yyyy"),
                    rating = movie.Rating,
                    mpaa = movie.MPAA,
                    downloaded = !string.IsNullOrWhiteSpace(movie.PATH) && System.IO.File.Exists(movie.PATH),
                });
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to get information on movie with TMDB ID of {tmdb}", e);
            }
            return new NotFoundResult();
        }
        [HttpGet("movies/recently-added")]
        public IActionResult GetRecentlyAddedMovies()
        {
            return new JsonResult(MovieLibraryModel.Instance.GetRecentlyAddedList());
        }
        [HttpGet("movies/continue-watching")]
        public IActionResult GetContinueWatchingMovies()
        {
            return new JsonResult(MovieLibraryModel.Instance.GetContinueWatchingList());
        }
        [HttpGet("movies/{tmdb}/images/poster")]
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
        [HttpGet("movies/{tmdb}/images/cover")]
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

        [HttpGet("tv/discover/{category}")]
        public IActionResult GetShowsDiscoveryList(DiscoveryCategory category)
        {
            object[] results = TvLibraryModel.Instance.DiscoverShows(category);
            if (results == null)
                return new JsonResult(new { message = $"No Results" });
            return new JsonResult(results);
        }
        [HttpGet("tv/discover/search/{query}/{year?}")]
        public IActionResult GetShowsDiscoveryFromQuery(string query, int? year)
        {
            object[] results = TvLibraryModel.Instance.SearchForShows(query, year.HasValue ? year.Value : -1);
            if (results == null)
                return new JsonResult(new { message = $"No Results" });
            return new JsonResult(results);
        }

        [HttpGet("tv/recently-added")]
        public IActionResult GetRecentlyAddedTv()
        {
            return new JsonResult(TvLibraryModel.Instance.GetRecentlyAddedList());
        }
        [HttpGet("tv/continue-watching")]
        public IActionResult GetContinueWatchingTv()
        {
            return new JsonResult(TvLibraryModel.Instance.GetContinueWatchingList());
        }
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
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}?api_key={TMDB_API}"));
                return new JsonResult(new
                {
                    id = tmdb,
                    title = json["name"].ToString(),
                    plot = json["overview"].ToString(),
                    year = DateTime.Parse(json["first_air_date"].ToString()).Year,
                    release_date = DateTime.Parse(json["first_air_date"].ToString()).ToString("MM-dd-yyyy"),
                    added = false,
                });
            }
            //return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            return new JsonResult(new
            {
                id = show.TMDB,
                title = show.Title,
                plot = show.Plot,
                year = show.StartDate.Year,
                release_date = show.StartDate.ToString("MM-dd-yyyy"),
                added = true
            });
        }
        [HttpGet("tv/{tmdb}/images/poster")]
        public IActionResult GetShowPoster(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);

            if (show == null)
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["poster_path"]}");
            }
            return File(new FileStream(show.PosterImage, FileMode.Open), "image/jpg");
        }
        [HttpGet("tv/{tmdb}/images/cover")]
        public IActionResult GetShowCover(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["backdrop_path"]}");
            }
            return File(new FileStream(show.CoverImage, FileMode.Open), "image/jpg");
        }
        #region Season
        [HttpGet("tv/{tmdb}/{season_number}")]
        public IActionResult GetSeason(string tmdb, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null || show.GetSeasonByNumber(season_number) == null)
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}?api_key={TMDB_API}"));
                return new JsonResult(new
                {
                    title = json["name"].ToString(),
                    plot = json["overview"].ToString(),
                    start_date = DateTime.Parse(json["air_date"].ToString()).ToString("MM-dd-yyyy"),
                    added = false,
                });
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            return new JsonResult(new
            {
                title = season.Title,
                plot = season.Plot,
                start_date = season.StartDate.ToString("MM-dd-yyyy"),
                added = true,
            });
        }
        [HttpGet("tv/{tmdb}/{season_number}/poster")]
        public IActionResult GetSeasonPoster(string tmdb, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null || show.GetSeasonByNumber(season_number) == null)
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["poster_path"]}");
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            return File(new FileStream(season.PosterImage, FileMode.Open), "image/jpg");
        }
        #region Episodes
        [HttpGet("tv/{tmdb}/{season_number}/{episode_number}")]
        public IActionResult GetEpisode(string tmdb, int season_number, int episode_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);

            if (show == null || show.GetSeasonByNumber(season_number) == null || show.GetSeasonByNumber(season_number).GetEpisodeByNumber(episode_number) == null)
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}/episode/{episode_number}?api_key={TMDB_API}"));

                return new JsonResult(new
                {
                    title = json["name"].ToString(),
                    plot = json["overview"].ToString(),
                    release_date = DateTime.Parse(json["air_date"].ToString()).ToString("MM-dd-yyyy"),
                    downloaded = false,
                });
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            return new JsonResult(new
            {
                title = episode.Title,
                plot = episode.Plot,
                release_date = episode.ReleaseDate.ToString("MM-dd-yyyy"),
                downloaded = !string.IsNullOrWhiteSpace(episode.PATH) && System.IO.File.Exists(episode.PATH),
            });
        }
        [HttpGet("tv/{tmdb}/{season_number}/{episode_number}/poster")]
        public IActionResult GetEpisodeStill(string tmdb, int season_number, int episode_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);

            if (show == null || show.GetSeasonByNumber(season_number) == null || show.GetSeasonByNumber(season_number).GetEpisodeByNumber(episode_number) == null)
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}/episode/{episode_number}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["still_path"]}");
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
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
