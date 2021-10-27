using Flexx.Core.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/get/tv/")]
    public class GetTVController : ControllerBase
    {
        #region TV

        [HttpGet("{username}")]
        public IActionResult GetShows(string username)
        {
            JsonResult result = new JsonResult(TvLibraryModel.Instance.GetList(Users.Instance.Get(username)));
            return result;
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

        [HttpGet("discover/search/{query}")]
        public IActionResult GetShowsDiscoveryFromQuery(string query, int? year)
        {
            object[] results = TvLibraryModel.Instance.SearchForShows(query, year.HasValue ? year.Value : -1);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("recently-added/{username}")]
        public IActionResult GetRecentlyAddedTv(string username)
        {
            return new JsonResult(TvLibraryModel.Instance.GetRecentlyAddedList(Users.Instance.Get(username)));
        }

        [HttpGet("continue-watching/{user}")]
        public IActionResult GetContinueWatchingTv(string user)
        {
            return new JsonResult(TvLibraryModel.Instance.GetContinueWatchingList(Users.Instance.Get(user)));
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
                JArray json = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/images?api_key={TMDB_API}")))["posters"];
                if (json.Any())
                {
                    return new RedirectResult($"https://image.tmdb.org/t/p/original/{json[0]["file_path"]}");
                }

                return File(new FileStream(Paths.MissingPoster, FileMode.Open), "image/jpg");
            }
            return File(new FileStream(show.PosterImage, FileMode.Open), "image/jpg");
        }

        [HttpGet("{tmdb}/images/cover")]
        public IActionResult GetShowCover(string tmdb, bool? language)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
            {
                if (language.GetValueOrDefault())
                {
                    JObject imagesJson = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/images?api_key={TMDB_API}&include_image_language=en"));
                    if (imagesJson["backdrops"].Any())
                    {
                        return new RedirectResult($"https://image.tmdb.org/t/p/original{imagesJson["backdrops"][0]["file_path"]}");
                    }
                }
                JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}?api_key={TMDB_API}"));
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["backdrop_path"]}");
            }
            return File(new FileStream(show.CoverImage, FileMode.Open), "image/jpg");
        }

        [HttpGet("{tmdb}/images/logo")]
        public IActionResult GetShowLogo(string tmdb)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            if (show == null)
            {
                JObject imagesJson = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/images?api_key={TMDB_API}&include_image_language=en"));
                if (imagesJson["logos"].Any())
                {
                    return new RedirectResult($"https://image.tmdb.org/t/p/original{imagesJson["logos"][0]["file_path"]}");
                }

                return new NotFoundResult();
            }
            if (string.IsNullOrWhiteSpace(show.LogoImage))
            {
                return new NotFoundResult();
            }

            return File(new FileStream(show.LogoImage, FileMode.Open), "image/png");
        }

        #endregion TV

        #region Season

        [HttpGet("{tmdb}/{username}/seasons")]
        public IActionResult GetSeasons(string tmdb, string username)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            List<object> json = new();
            if (show == null)
            {
                foreach (JToken token in (JArray)((JToken)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}?api_key={TMDB_API}")))["seasons"])
                {
                    json.Add(new SeasonObject(JsonConvert.SerializeObject(token)));
                }
                return new JsonResult(new { seasons = json.ToArray() });
            }
            SeasonModel[] seasons = show.Seasons.ToArray();
            foreach (SeasonModel season in seasons)
            {
                json.Add(new SeasonObject(season, Users.Instance.Get(username)));
            }
            return new JsonResult(new { seasons = json.ToArray() });
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
                if (json["poster_path"] == null || string.IsNullOrWhiteSpace(json["poster_path"].ToString()))
                {
                    return File(new FileStream(Paths.MissingPoster, FileMode.Open), "image/jpg");
                }

                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["poster_path"]}");
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            return File(new FileStream(season.PosterImage, FileMode.Open), "image/jpg");
        }

        #endregion Season

        #region Episodes

        [HttpGet("{tmdb}/{username}/{season_number}/episodes")]
        public IActionResult GetEpisodes(string tmdb, string username, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);
            List<object> json = new();
            if (show == null)
            {
                Parallel.ForEach((JArray)((JToken)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}?api_key={TMDB_API}")))["episodes"], token =>
                {
                    EpisodeObject episode = new EpisodeObject(JsonConvert.SerializeObject(token));
                    if (episode.ReleaseDate <= DateTime.Now)
                    {
                        json.Add(episode);
                    }
                });
            }
            else
            {
                log.Info($"Getting Episodes for \"{show.Title}\" season {season_number} for user {username}");
                EpisodeModel[] episodes = show.GetSeasonByNumber(season_number).Episodes.ToArray();
                User user = Users.Instance.Get(username);
                Parallel.ForEach(episodes, episode =>
                 {
                     json.Add(new EpisodeObject(episode, user));
                 });
            }
            json = json.OrderBy(e => ((EpisodeObject)e).Episode).ToList();
            return new JsonResult(new { episodes = json.ToArray() });
        }

        [HttpGet("{tmdb}/{username}/{season_number}/{episode_number}")]
        public IActionResult GetEpisode(string tmdb, string username, int season_number, int episode_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(tmdb);

            if (show == null || show.GetSeasonByNumber(season_number) == null || show.GetSeasonByNumber(season_number).GetEpisodeByNumber(episode_number) == null)
            {
                JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}/episode/{episode_number}?api_key={TMDB_API}"));

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
                JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}/episode/{episode_number}?api_key={TMDB_API}"));
                if (json["still_path"] == null || string.IsNullOrWhiteSpace(json["still_path"].ToString()))
                {
                    log.Debug($"https://api.themoviedb.org/3/tv/{tmdb}/season/{season_number}/episode/{episode_number}?api_key={TMDB_API}");
                    return new JsonResult(json);
                }
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["still_path"]}");
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            return File(new FileStream(episode.PosterImage, FileMode.Open), "image/jpg");
        }

        [HttpGet("{tmdb}/{user}/{season_number}/{episode_number}/video")]
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
    }
}