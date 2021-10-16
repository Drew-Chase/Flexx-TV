using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using static Flexx.Core.Data.Global;

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

        [HttpGet("continue-watching")]
        public IActionResult GetContinueWatchingTv()
        {
            return new JsonResult(TvLibraryModel.Instance.GetContinueWatchingList());
        }

        [HttpGet("{tmdb}")]
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

        [HttpGet("{tmdb}/images/poster")]
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

        [HttpGet("{tmdb}/images/cover")]
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
        [HttpGet("{tmdb}/seasons")]
        public IActionResult GetSeasons(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            SeasonModel[] seasons = show.Seasons.ToArray();
            object[] json = new object[seasons.Length];
            for (int i = 0; i < json.Length; i++)
            {
                SeasonModel season = seasons[i];
                json[i] = new
                {
                    title = season.Title,
                    number = season.Season_Number,
                    episodes = season.Episodes.Count,
                };
            }
            return new JsonResult(new { seasons = json });

        }
        [HttpGet("{tmdb}/{season_number}")]
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

        [HttpGet("{tmdb}/{season_number}/poster")]
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


        [HttpGet("{tmdb}/{season_number}/episodes")]
        public IActionResult GetEpisoes(string tmdb, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            EpisodeModel[] episodes = show.GetSeasonByNumber(season_number).Episodes.ToArray();
            object[] json = new object[episodes.Length];
            for (int i = 0; i < json.Length; i++)
            {
                EpisodeModel episode = episodes[i];
                json[i] = new
                {
                    title = episode.Title,
                    number = episode.Episode_Number,
                    name = episode.FriendlyName,
                };
            }
            return new JsonResult(new { episodes = json });

        }

        [HttpGet("{tmdb}/{season_number}/{episode_number}")]
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

            return File(((MediaBase)episode).Stream, "video/mp4", true);
        }

        #endregion Episodes

        #endregion Season
    }
}