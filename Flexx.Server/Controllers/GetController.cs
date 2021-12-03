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
using System.Net.Http;
using static Flexx.Core.Data.Global;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/get/")]
    public class GetController : ControllerBase
    {
        /// <summary>
        /// Will retrieve All <b><u><see cref="MovieObject">Movies</see></u></b>, <b><u><see cref="SeriesObject">Tv Shows</see></u></b>, <b><u><see cref="SeasonObject">Shows Seasons</see></u></b> or <b><u><see cref="EpisodeObject">Seasons Episodes</see></u></b>, <i>based on parameters</i>
        /// </summary>
        /// <param name="library">Either "tv" or "movies"</param>
        /// <param name="username">leave blank for guest user</param>
        /// <param name="id">TV Shows ID (only use with tv library)</param>
        /// <param name="season">TV Shows Season Number (only use with tv library and id)</param>
        /// <returns></returns>
        public JsonResult Index(string library, string? username, string? id, int? season)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(library))
                {
                    User user = Users.Instance.Get(username);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        return new(library.Equals("tv") ? TvLibraryModel.Instance.GetLocalList(user) : MovieLibraryModel.Instance.GetLocalList(user));
                    }
                    else
                    {
                        object obj = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/{library}/{id}{(library.Equals("tv") ? (season.HasValue ? $"/season/{season.Value}" : "") : "")}?api_key={TMDB_API}");
                        if (obj == null) return new(new { });
                        if (library.Equals("tv"))
                        {
                            TVModel tvModel = TvLibraryModel.Instance.GetShowByTMDB(id);
                            if (!season.HasValue)
                            {
                                // Get Show Seasons
                                List<SeasonObject> seasons = new();
                                if (tvModel == null || !tvModel.Added)
                                {
                                    if (obj != null)
                                    {
                                        foreach (var json in (JArray)((JToken)obj)["seasons"])
                                        {
                                            seasons.Add(new(JsonConvert.SerializeObject(json)));
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (SeasonModel seasonModel in tvModel.Seasons)
                                    {
                                        seasons.Add(new(seasonModel, user));
                                    }
                                }
                                seasons.Sort((x, y) => x.Season.CompareTo(y.Season));
                                return new(new { seasons });
                            }
                            else
                            {
                                // Get Seasons Episodes
                                List<EpisodeObject> episodes = new();
                                if (tvModel == null || !tvModel.Added || (tvModel != null && tvModel.GetSeasonByNumber(season.Value) == null))
                                {
                                    if (obj != null)
                                    {
                                        foreach (var json in (JArray)((JToken)obj)["episodes"])
                                        {
                                            episodes.Add(new(JsonConvert.SerializeObject(json)));
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var episodeModel in tvModel.GetSeasonByNumber(season.Value).Episodes)
                                    {
                                        episodes.Add(new(episodeModel, user));
                                    }
                                }
                                return new(new { episodes });
                            }
                        }
                    }
                }
            }
            catch
            {
                return new(new { });
            }
            return new(new { });
        }

        /// <summary>
        /// Will retrieve any specific information related to specified ID, library, season and episode.
        /// </summary>
        /// <param name="id">the TMDB ID of the element</param>
        /// <param name="library">Either "tv" or "movie"</param>
        /// <param name="username">leave blank for guest user</param>
        /// <param name="season">TV Shows Season Number (only use with tv library and id specified)</param>
        /// <param name="episode">Seasons Episode Number (only use with tv library, id and season specified)</param>
        /// <returns></returns>
        [HttpGet("info")]
        public JsonResult Info(string id, string library, string? username, int? season, int? episode)
        {
            try
            {
                User user = Users.Instance.Get(username);
                object obj = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/{library}/{id}{(library.Equals("tv") ? (season.HasValue ? $"/season/{season.Value}{(episode.HasValue ? $"/episode/{episode.Value}" : "")}" : "") : "")}?api_key={TMDB_API}");

                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(library) && obj != null)
                {
                    if (library.Equals("tv"))
                    {
                        TVModel tvModel = TvLibraryModel.Instance.GetShowByTMDB(id);
                        SeasonModel seasonModel = null;
                        EpisodeModel episodeModel;

                        if (!season.HasValue && !episode.HasValue)
                        {
                            // Show View
                            if (tvModel == null)
                            {
                                return new(new SeriesObject(JsonConvert.SerializeObject(obj)));
                            }
                            else
                            {
                                return new(new SeriesObject(tvModel, user));
                            }
                        }
                        else if (season.HasValue)
                        {
                            // Season View
                            if (tvModel != null)
                                seasonModel = tvModel.GetSeasonByNumber(season.Value);
                            if (!episode.HasValue)
                            {
                                if (tvModel == null || seasonModel == null)
                                {
                                    return new(new SeasonObject(JsonConvert.SerializeObject(obj)));
                                }
                                else
                                {
                                    SeasonModel s = tvModel.GetSeasonByNumber(season.Value);
                                    if (s != null)
                                        return new(new SeasonObject(s, user));
                                }
                            }
                            else
                            {
                                // Episode View
                                if (tvModel == null || seasonModel == null)
                                {
                                    return new(new EpisodeObject(JsonConvert.SerializeObject(obj)));
                                }
                                else
                                {
                                    episodeModel = seasonModel.GetEpisodeByNumber(episode.Value);
                                    if (episodeModel != null)
                                        return new(new EpisodeObject(episodeModel, user));
                                }
                            }
                        }
                    }
                    else if (library.Equals("movie"))
                    {
                        MovieModel movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
                        if (movie == null)
                        {
                            return new(new MovieObject(JsonConvert.SerializeObject(obj)));
                        }
                        else
                        {
                            return new(new MovieObject(movie, user));
                        }
                    }
                }
            }
            catch
            {
                return new(new { });
            }
            return new(new { });
        }

        /// <summary>
        /// Will retrieve any related images for media property.
        /// </summary>
        /// <param name="id">the TMDB ID of the element</param>
        /// <param name="library">Either "tv" or "movie"</param>
        /// <param name="username">leave blank for guest user</param>
        /// <param name="season">TV Shows Season Number (only use with "tv" library specified)</param>
        /// <param name="episode">Seasons Episode Number (only use with "tv" library and season specified)</param>
        /// <param name="type">Either "poster", "cover" or "logo"</param>
        /// <param name="language">if true will return cover image with text on it (only works if type is equal to "cover")</param>
        /// <returns></returns>
        [HttpGet("images")]
        public IActionResult Images(string id, string library, string type, bool? language, int? season, int? episode)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(library) || !string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(type))
                {
                    object obj = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/{library}/{id}{(season.HasValue ? $"/season/{season.Value}{(episode.HasValue ? $"/episode/{episode.Value}" : "")}" : "")}/images?api_key={TMDB_API}{(language.GetValueOrDefault(false) ? $"&include_image_language={config.LanguagePreference}" : "")}");
                    JArray images;
                    string key = "";
                    if (obj != null)
                    {
                        JObject json = (JObject)obj;
                        if (library.Equals("movie"))
                        {
                            var movie = MovieLibraryModel.Instance.GetMovieByTMDB(id);
                            if (movie == null)
                            {
                                if (type.Equals("poster"))
                                {
                                    images = (JArray)json["posters"];
                                    if (images.Any())
                                    {
                                        key = (string)images[0]["file_path"];
                                    }
                                    else
                                    {
                                        return File(new FileStream(Paths.MissingPoster, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                    }
                                }
                                else if (type.Equals("cover"))
                                {
                                    images = (JArray)json["backdrops"];
                                    if (images.Any())
                                    {
                                        key = (string)images[0]["file_path"];
                                    }
                                    else
                                    {
                                        return File(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                    }
                                }
                                else if (type.Equals("logo"))
                                {
                                    images = (JArray)json["logos"];
                                    if (images.Any())
                                    {
                                        key = (string)images[0]["file_path"];
                                    }
                                    else
                                    {
                                        return NotFound();
                                    }
                                }
                            }
                            else
                            {
                                if (type.Equals("poster"))
                                {
                                    if (!string.IsNullOrWhiteSpace(movie.PosterImage) && System.IO.File.Exists(movie.PosterImage))
                                    {
                                        return File(new FileStream(movie.PosterImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                    }
                                    else
                                    {
                                        return File(new FileStream(Paths.MissingPoster, FileMode.Open, FileAccess.Read), "image/jpg", false);
                                    }
                                }
                                else if (type.Equals("cover"))
                                {
                                    if (language.GetValueOrDefault(false))
                                    {
                                        if (!string.IsNullOrWhiteSpace(movie.CoverImageWithLanguage) && System.IO.File.Exists(movie.CoverImageWithLanguage))
                                        {
                                            return File(new FileStream(movie.CoverImageWithLanguage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                        }
                                        else
                                        {
                                            return Images(id, library, type, false, season, episode);
                                        }
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(movie.CoverImage) && System.IO.File.Exists(movie.CoverImage))
                                        {
                                            return File(new FileStream(movie.CoverImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                        }
                                        else
                                        {
                                            return File(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read), "image/jpg", false);
                                        }
                                    }
                                }
                                else if (type.Equals("logo"))
                                {
                                    if (!string.IsNullOrWhiteSpace(movie.LogoImage) && System.IO.File.Exists(movie.LogoImage))
                                    {
                                        return File(new FileStream(movie.LogoImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                    }
                                    else
                                    {
                                        return NotFound();
                                    }
                                }
                            }
                        }
                        else if (library.Equals("tv"))
                        {
                            TVModel tvModel = TvLibraryModel.Instance.GetShowByTMDB(id);
                            SeasonModel seasonModel;
                            EpisodeModel episodeModel;
                            if (tvModel != null)
                            {
                                if (type.Equals("poster"))
                                {
                                    if (season.HasValue)
                                    {
                                        seasonModel = tvModel.GetSeasonByNumber(season.Value);
                                        if (episode.HasValue)
                                        {
                                            if (tvModel.Added)
                                            {
                                                episodeModel = seasonModel.GetEpisodeByNumber(episode.Value);
                                                if (episodeModel != null && !string.IsNullOrWhiteSpace(episodeModel.PosterImage) && System.IO.File.Exists(episodeModel.PosterImage))
                                                {
                                                    return File(new FileStream(episodeModel.PosterImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                                }
                                                else
                                                {
                                                    return BadRequest();
                                                }
                                            }
                                            else
                                            {
                                                images = (JArray)json["stills"];
                                                if (!images.Any())
                                                {
                                                    return File(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                                }
                                                else
                                                {
                                                    key = (string)images[0]["file_path"];
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (seasonModel != null && !string.IsNullOrWhiteSpace(seasonModel.PosterImage) && System.IO.File.Exists(seasonModel.PosterImage))
                                            {
                                                return File(new FileStream(seasonModel.PosterImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(tvModel.PosterImage) && System.IO.File.Exists(tvModel.PosterImage))
                                        {
                                            return File(new FileStream(tvModel.PosterImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                        }
                                    }
                                }
                                else if (type.Equals("cover"))
                                {
                                    if (season.HasValue)
                                    {
                                        seasonModel = tvModel.GetSeasonByNumber(season.Value);
                                        if (episode.HasValue)
                                        {
                                            episodeModel = seasonModel.GetEpisodeByNumber(episode.Value);
                                            if (episodeModel != null && !string.IsNullOrWhiteSpace(tvModel.CoverImage) && System.IO.File.Exists(tvModel.CoverImage))
                                            {
                                                return File(new FileStream(tvModel.CoverImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                            }
                                        }
                                        else
                                        {
                                            if (seasonModel != null && !string.IsNullOrWhiteSpace(tvModel.CoverImage) && System.IO.File.Exists(tvModel.CoverImage))
                                            {
                                                return File(new FileStream(tvModel.CoverImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(tvModel.CoverImage) && System.IO.File.Exists(tvModel.CoverImage))
                                        {
                                            return File(new FileStream(tvModel.CoverImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                        }
                                    }
                                }
                                else if (type.Equals("logo"))
                                {
                                    if (!string.IsNullOrWhiteSpace(tvModel.LogoImage) && System.IO.File.Exists(tvModel.LogoImage))
                                    {
                                        return File(new FileStream(tvModel.LogoImage, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                    }
                                }
                            }
                            if (type.Equals("poster"))
                            {
                                if (season.HasValue)
                                {
                                    if (episode.HasValue)
                                    {
                                        images = (JArray)json["stills"];
                                        if (!images.Any())
                                        {
                                            return File(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                        }
                                        else
                                        {
                                            key = (string)images[0]["file_path"];
                                        }
                                    }
                                    else
                                    {
                                        images = (JArray)json["posters"];
                                        if (!images.Any())
                                        {
                                            return File(new FileStream(Paths.MissingPoster, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                        }
                                        else
                                        {
                                            key = (string)images[0]["file_path"];
                                        }
                                    }
                                }
                                else
                                {
                                    images = (JArray)json["posters"];
                                    if (!images.Any())
                                    {
                                        return File(new FileStream(Paths.MissingPoster, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                    }
                                    else
                                    {
                                        key = (string)images[0]["file_path"];
                                    }
                                }
                            }
                            else if (type.Equals("cover"))
                            {
                                images = (JArray)json["backdrops"];
                                if (images.Any())
                                {
                                    key = (string)images[0]["file_path"];
                                }
                                else
                                {
                                    return File(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read, FileShare.Read), "image/png");
                                }
                            }
                            else if (type.Equals("logo"))
                            {
                                images = (JArray)json["logos"];
                                if (images.Any())
                                {
                                    key = (string)images[0]["file_path"];
                                }
                                else
                                {
                                    return NotFound();
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            return File(new HttpClient().GetAsync($"https://image.tmdb.org/t/p/original{key}").Result.Content.ReadAsStream(), "image/png");
                        }
                    }
                }
                return File(new FileStream(Paths.MissingCover, FileMode.Open, FileAccess.Read), "image/jpg", false);
            }
            catch
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Filter Media properties by various parameters
        /// </summary>
        /// <param name="library">Either "tv" or "movie"</param>
        /// <param name="category"></param>
        /// <param name="username">Leave blank for guest user</param>
        /// <param name="query">If Category is "search" then this is the search query</param>
        /// <param name="year">Filters by year</param>
        /// <param name="id">The TMDB ID of the element</param>
        /// <returns></returns>
        [HttpGet("filtered")]
        public JsonResult FilteredList(string library, string category, string? username, string? query, int? year, string? id)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(library) && !string.IsNullOrWhiteSpace(category))
                {
                    object[] results = null;
                    User user = Users.Instance.Get(username);
                    if (category.Equals("similar"))
                    {
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            if (library.Equals("movie"))
                            {
                                results = MovieLibraryModel.Instance.FindSimilar(id);
                            }
                            else if (library.Equals("tv"))
                            {
                                results = TvLibraryModel.Instance.FindSimilar(id);
                            }
                        }
                        if (results == null)
                        {
                            return FilteredList(library, "popular", username, query, year, id);
                        }
                        return new(results);
                    }
                    else if (category.Equals("search"))
                    {
                        if (!string.IsNullOrWhiteSpace(query))
                        {
                            if (library.Equals("movie"))
                            {
                                results = MovieLibraryModel.Instance.Search(query);
                            }
                            else if (library.Equals("tv"))
                            {
                                results = TvLibraryModel.Instance.Search(query);
                            }
                            else if (library.Equals("actors"))
                            {
                                results = CastListModel.GetMediaByActor(query);
                            }
                        }
                        if (results == null)
                        {
                            return FilteredList(library, "popular", username, query, year, id);
                        }
                        return new(results);
                    }
                    else if (category.Equals("recently-added"))
                    {
                        if (library.Equals("movie"))
                        {
                            return new(MovieLibraryModel.Instance.GetRecentlyAddedList(Users.Instance.Get(username)));
                        }
                        else if (library.Equals("tv"))
                        {
                            return new(TvLibraryModel.Instance.GetRecentlyAddedList(Users.Instance.Get(username)));
                        }
                    }
                    else if (category.Equals("continue-watching"))
                    {
                        List<object> list = new();
                        foreach (MediaBase media in user.ContinueWatchingList())
                        {
                            if (media.GetType().Equals(typeof(MovieModel)))
                            {
                                list.Add(new MovieObject((MovieModel)media, user));
                            }
                            else if (media.GetType().Equals(typeof(EpisodeModel)))
                            {
                                list.Add(new EpisodeObject((EpisodeModel)media, user));
                            }
                        }
                        return new(list);
                    }
                    else if (Enum.TryParse(typeof(DiscoveryCategory), category, out object cat))
                    {
                        DiscoveryCategory discoveryCategory = (DiscoveryCategory)cat;
                        if (library.Equals("movie"))
                        {
                            results = MovieLibraryModel.Instance.Discover(user, discoveryCategory);
                        }
                        else if (library.Equals("tv"))
                        {
                            results = TvLibraryModel.Instance.Discover(user, discoveryCategory);
                        }
                        if (results != null)
                        {
                            return new(results);
                        }
                    }
                }
            }
            catch
            {
                return new(new { });
            }
            return new(new { });
        }

        #region Notifications

        [HttpGet("{username}/notifications")]
        public IActionResult GetUsersNotifications(string username)
        {
            Notifications Notifications = Users.Instance.Get(username).Notifications;

            return new JsonResult(new
            {
                @new = Notifications.Get().Where(n => n.New).ToList().Count,
                count = Notifications.Get().Length,
                notifications = Notifications.GetObject()
            });
        }

        [HttpGet("{username}/notifications/push")]
        public IActionResult PushNotification(string username, string type, string title, string message)
        {
            if (Enum.TryParse(typeof(NotificationType), type, out object typeObject))
            {
                User user = Users.Instance.Get(username);
                user.Notifications.Push(new((NotificationType)typeObject, user, title, message, DateTime.Now, true));
                return new OkResult();
            }
            return new BadRequestResult();
        }

        [HttpGet("{username}/notifications/mark-as-read")]
        public IActionResult MarkNotificationAsRead(string username, string title)
        {
            Users.Instance.Get(username).Notifications.MarkAsRead(title);
            return new OkResult();
        }

        #endregion Notifications
    }
}