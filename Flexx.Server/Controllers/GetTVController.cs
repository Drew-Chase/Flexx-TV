using Flexx.Core.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;
using static Flexx.Media.Objects.Extras.MovieObject;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/get/tv/")]
    public class GetTVController : ControllerBase
    {
        public IActionResult Index()
        {
            return new JsonResult(TvLibraryModel.Instance.GetList());
        }

        [HttpGet("discover/{category}")]
        public IActionResult GetShowsDiscoveryList(DiscoveryCategory category)
        {
            object[] results = TvLibraryModel.Instance.DiscoverShows(category);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("discover/search/{query}/{year?}")]
        public IActionResult GetShowsDiscoveryFromQuery(string query, int? year)
        {
            object[] results = TvLibraryModel.Instance.SearchForShows(query, year.HasValue ? year.Value : -1);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("recently-added")]
        public IActionResult GetRecentlyAddedTv()
        {
            return new JsonResult(TvLibraryModel.Instance.GetRecentlyAddedList());
        }

        [HttpGet("{user}/continue-watching")]
        public IActionResult GetContinueWatchingTv(string user)
        {
            return new JsonResult(TvLibraryModel.Instance.GetContinueWatchingList(user));
        }

        [HttpGet("{tmdb}/{username}")]
        public IActionResult GetShow(string tmdb, string username)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
            {
                return new JsonResult(new SeriesObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}?api_key={TMDB_API}")));
            }

            return new JsonResult(new SeriesObject(show, Users.Instance.Get(username)));
        }

        [HttpGet("{tmdb}/images/poster")]
        public IActionResult GetShowPoster(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);

            if (show == null)
            {
                JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["poster_path"]}");
            }
            return File(new FileStream(show.PosterImage, FileMode.Open), "image/jpg");
        }

        [HttpGet("{tmdb}/images/cover")]
        public IActionResult GetShowCover(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
            {
                JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["backdrop_path"]}");
            }
            return File(new FileStream(show.CoverImage, FileMode.Open), "image/jpg");
        }

        #region Season

        [HttpGet("{tmdb}/{username}/seasons")]
        public IActionResult GetSeasons(string tmdb, string username)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            SeasonModel[] seasons = show.Seasons.ToArray();
            object[] json = new object[seasons.Length];
            for (int i = 0; i < json.Length; i++)
            {
                json[i] = new SeasonObject(seasons[i], Users.Instance.Get(username));
            }
            return new JsonResult(new { seasons = json });
        }

        [HttpGet("{tmdb}/{username}/{season_number}")]
        public IActionResult GetSeason(string tmdb, string username, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null || show.GetSeasonByNumber(season_number) == null)
            {
                return new JsonResult(new SeasonObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}?api_key={TMDB_API}")));
            }
            return new JsonResult(new SeasonObject(show.GetSeasonByNumber(season_number), Users.Instance.Get(username)));
        }

        [HttpGet("{tmdb}/{season_number}/poster")]
        public IActionResult GetSeasonPoster(string tmdb, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null || show.GetSeasonByNumber(season_number) == null)
            {
                JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["poster_path"]}");
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            return File(new FileStream(season.PosterImage, FileMode.Open), "image/jpg");
        }

        #region Episodes

        [HttpGet("{tmdb}/{username}/{season_number}/episodes")]
        public IActionResult GetEpisodes(string tmdb, string username, int season_number)
        {
            DateTime now = DateTime.Now;
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            log.Info($"Getting Episodes for \"{show.Title}\" season {season_number} for user {username}");
            EpisodeModel[] episodes = show.GetSeasonByNumber(season_number).Episodes.ToArray();
            object[] json = new object[episodes.Length];
            User user = Users.Instance.Get(username);
            Parallel.For(0, json.Length, i =>
             {
                 json[i] = new EpisodeObject(episodes[i], user);
             });
            log.Info($"Done Fetching Episodes for \"{show.Title}\" season {season_number}");
            log.Warn($"Fetch took {(DateTime.Now - now).TotalSeconds}s");
            return new JsonResult(new { episodes = json });
        }

        [HttpGet("{tmdb}/{username}/{season_number}/{episode_number}")]
        public IActionResult GetEpisode(string tmdb, string username, int season_number, int episode_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);

            if (show == null || show.GetSeasonByNumber(season_number) == null || show.GetSeasonByNumber(season_number).GetEpisodeByNumber(episode_number) == null)
            {
                using System.Net.WebClient client = new();
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}/episode/{episode_number}?api_key={TMDB_API}"));

                return new JsonResult(new EpisodeObject(JsonConvert.SerializeObject(json)));
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            User user = Users.Instance.Get(username);
            return new JsonResult(new EpisodeObject(episode, user));
        }

        [HttpGet("{tmdb}/{season_number}/{episode_number}/poster")]
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

        [HttpGet("{tmdb}/{user}/{season_number}/{episode_number}/video/{resolution?}/{bitrate?}")]
        public IActionResult GetEpisodeStream(string tmdb, int season_number, int episode_number, string user, int? resolution, int? bitrate)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
            {
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" not found" });
            }

            SeasonModel season = show.GetSeasonByNumber(season_number);
            if (season == null)
            {
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" doesn't contain a season numbered {season_number}" });
            }

            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            if (episode == null)
            {
                return new JsonResult(new { message = $"Show with ID of \"{tmdb}\" and season #{season_number} doesn't contain episode numbered {episode_number}" });
            }

            if (resolution.HasValue && bitrate.HasValue)
            {
                return File(FFMpegUtil.GetTranscodedStream(user, episode, resolution.Value, bitrate.Value), "application/x-mpegURL", true);
            }

            return File(episode.Stream, "video/mp4", true);
        }

        #endregion Episodes

        #endregion Season
    }
}