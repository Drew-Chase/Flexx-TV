using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
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
            return new JsonResult(TvLibraryModel.Instance.GetLocalList(Users.Instance.Get(username)));
        }

        [HttpGet("discover/{category}")]
        public IActionResult GetShowsDiscoveryList(DiscoveryCategory category, string username)
        {
            SeriesObject[] results = TvLibraryModel.Instance.DiscoverShows(Users.Instance.Get(username), category);
            if (results == null)
            {
                return new JsonResult(new { message = $"No Results" });
            }

            return new JsonResult(results);
        }

        [HttpGet("discover/search")]
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
        public IActionResult GetRecentlyAddedTv(string username)
        {
            return new JsonResult(TvLibraryModel.Instance.GetRecentlyAddedList(Users.Instance.Get(username)));
        }

        [HttpGet("continue-watching")]
        public IActionResult GetContinueWatchingTv(string username)
        {
            return new JsonResult(TvLibraryModel.Instance.GetContinueWatchingList(Users.Instance.Get(username)));
        }

        [HttpGet("{id}/{username}")]
        public IActionResult GetShow(string id, string username)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            if (show == null)
            {
                try
                {
                    return new JsonResult(new SeriesObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{id}?api_key={TMDB_API}")));
                }
                catch
                {
                    return BadRequest();
                }
            }

            return new JsonResult(new SeriesObject(show, Users.Instance.Get(username)));
        }

        [HttpGet("{id}/images/poster")]
        public IActionResult GetShowPoster(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            if (show == null)
            {
                var j = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}/images?api_key={TMDB_API}");
                if (j != new { })
                {
                    JArray json = (JArray)((JObject)j)["posters"];
                    if (json.Any())
                    {
                        if (json[0]["file_path"] != null && !string.IsNullOrWhiteSpace(json[0]["file_path"].ToString()))
                        {
                            return new RedirectResult($"https://image.tmdb.org/t/p/original/{json[0]["file_path"]}");
                        }
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(show.PosterImage) && System.IO.File.Exists(show.PosterImage))
            {
                return File(new FileStream(show.PosterImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
            }

            return new FileStreamResult(new FileStream(Paths.MissingPoster, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
        }

        [HttpGet("{id}/images/cover")]
        public IActionResult GetShowCover(string id, bool? language)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            if (show == null)
            {
                if (language.GetValueOrDefault(false))
                {
                    JObject imagesJson = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}/images?api_key={TMDB_API}&include_image_language=en");
                    if (imagesJson["backdrops"].Any())
                    {
                        return new RedirectResult($"https://image.tmdb.org/t/p/original{imagesJson["backdrops"][0]["file_path"]}");
                    }
                }
                JObject json = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}?api_key={TMDB_API}");
                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["backdrop_path"]}");
            }
            if (language.GetValueOrDefault(false) && !string.IsNullOrWhiteSpace(show.CoverImageWithLanguage) && System.IO.File.Exists(show.CoverImageWithLanguage))
            {
                return File(new FileStream(show.CoverImageWithLanguage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(show.CoverImage) && System.IO.File.Exists(show.CoverImage))
                {
                    return File(new FileStream(show.CoverImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
                }
            }

            return new FileStreamResult(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
        }

        [HttpGet("{id}/images/logo")]
        public IActionResult GetShowLogo(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            if (show == null)
            {
                JObject imagesJson = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}/images?api_key={TMDB_API}&include_image_language=en");
                if (imagesJson["logos"].Any())
                {
                    return new RedirectResult($"https://image.tmdb.org/t/p/original{imagesJson["logos"][0]["file_path"]}");
                }

                return new NotFoundResult();
            }
            if (!string.IsNullOrWhiteSpace(show.LogoImage) && System.IO.File.Exists(show.LogoImage))
            {
                return File(new FileStream(show.LogoImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
            }

            return new NotFoundResult();
        }

        #endregion TV

        #region Season

        [HttpGet("{id}/{username}/seasons")]
        public IActionResult GetSeasons(string id, string username)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            List<object> json = new();
            if (show == null || !show.Added)
            {
                foreach (JToken token in (JArray)((JToken)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}?api_key={TMDB_API}"))["seasons"])
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

        [HttpGet("{id}/{username}/{season_number}")]
        public IActionResult GetSeason(string id, string username, int season_number)
        {
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            if (show == null || show.GetSeasonByNumber(season_number) == null)
            {
                return new JsonResult(new SeasonObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{id}/season/{season_number}?api_key={TMDB_API}")));
            }
            return new JsonResult(new SeasonObject(show.GetSeasonByNumber(season_number), Users.Instance.Get(username)));
        }

        [HttpGet("{id}/{season_number}/poster")]
        public IActionResult GetSeasonPoster(string id, int season_number)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });

            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            if (show == null || show.GetSeasonByNumber(season_number) == null)
            {
                object jobj = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}/season/{season_number}?api_key={TMDB_API}");
                if (jobj == new { }) return BadRequest();
                JObject json = (JObject)jobj;
                if (json["poster_path"] == null || string.IsNullOrWhiteSpace(json["poster_path"].ToString()))
                {
                    return new FileStreamResult(new FileStream(Paths.MissingPoster, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
                }

                return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["poster_path"]}");
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);

            if (!string.IsNullOrWhiteSpace(season.PosterImage) && System.IO.File.Exists(season.PosterImage))
            {
                return File(new FileStream(season.PosterImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
            }

            return new FileStreamResult(new FileStream(Paths.MissingPoster, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
        }

        #endregion Season

        #region Episodes

        [HttpGet("{id}/{username}/{season_number}/episodes")]
        public IActionResult GetEpisodes(string id, string username, int season_number)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            List<object> json = new();
            if (show == null || !show.Added)
            {
                object jobj = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}/season/{season_number}?api_key={TMDB_API}");
                Parallel.ForEach((JArray)((JToken)jobj)["episodes"], token =>
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

        [HttpGet("{id}/{username}/{season_number}/{episode_number}")]
        public IActionResult GetEpisode(string id, string username, int season_number, int episode_number)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);

            if (show == null || show.GetSeasonByNumber(season_number) == null || show.GetSeasonByNumber(season_number).GetEpisodeByNumber(episode_number) == null)
            {
                JObject json = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}/season/{season_number}/episode/{episode_number}?api_key={TMDB_API}");

                return new JsonResult(new EpisodeObject(JsonConvert.SerializeObject(json)));
            }
            SeasonModel season = show.GetSeasonByNumber(season_number);
            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            User user = Users.Instance.Get(username);
            return new JsonResult(new EpisodeObject(episode, user));
        }

        [HttpGet("{id}/{season_number}/{episode_number}/poster")]
        public IActionResult GetEpisodeStill(string id, int season_number, int episode_number)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);

            if (show == null || show.GetSeasonByNumber(season_number) == null || show.GetSeasonByNumber(season_number).GetEpisodeByNumber(episode_number) == null)
            {
                JObject json = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}/season/{season_number}/episode/{episode_number}?api_key={TMDB_API}");
                if (json["still_path"] == null || string.IsNullOrWhiteSpace(json["still_path"].ToString()))
                {
                    return new JsonResult(json);
                }
                if (json["still_path"] != null && !string.IsNullOrWhiteSpace(json["still_path"].ToString()))
                    return new RedirectResult($"https://image.tmdb.org/t/p/original/{json["still_path"]}");
            }
            else
            {
                SeasonModel season = show.GetSeasonByNumber(season_number);
                EpisodeModel episode = season.GetEpisodeByNumber(episode_number);

                if (!string.IsNullOrWhiteSpace(episode.PosterImage) && System.IO.File.Exists(episode.PosterImage))
                {
                    return File(new FileStream(episode.PosterImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
                }
            }

            return new FileStreamResult(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read, FileShare.Read), "image/jpg");
        }

        [HttpGet("{id}/{season_number}/{episode_number}/video")]
        public IActionResult GetEpisodeStream(string id, int season_number, int episode_number, int? resolution, int? bitrate)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "The id cannot be empty" });
            TVModel show = TvLibraryModel.Instance.GetShowByTMDB(id);
            if (show == null)
            {
                return new JsonResult(new { message = $"Show with ID of \"{id}\" not found" });
            }

            SeasonModel season = show.GetSeasonByNumber(season_number);
            if (season == null)
            {
                return new JsonResult(new { message = $"Show with ID of \"{id}\" doesn't contain a season numbered {season_number}" });
            }

            EpisodeModel episode = season.GetEpisodeByNumber(episode_number);
            if (episode == null)
            {
                return new JsonResult(new { message = $"Show with ID of \"{id}\" and season #{season_number} doesn't contain episode numbered {episode_number}" });
            }

            if (resolution.HasValue && bitrate.HasValue)
            {
                //return File(Transcoder.GetTranscodedStream(user, episode, resolution.Value, bitrate.Value), "application/x-mpegURL", true);
            }

            return File(episode.Stream, "video/mp4", true);
        }

        #endregion Episodes
    }
}