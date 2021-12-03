using Flexx.Authentication;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Libraries
{
    public class TvLibraryModel : LibraryModel
    {
        #region Private Fields

        private static TvLibraryModel _instance = null;

        #endregion Private Fields

        #region Public Properties

        public static TvLibraryModel Instance
        {
            get
            {
                if (_instance == null) _instance = new TvLibraryModel();
                return _instance;
            }
        }

        public List<TVModel> TVShows { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void AddGhostEpisodes(TVModel show = null)
        {
            try
            {
                JObject json = null;
                if (show == null)
                {
                    Parallel.ForEach(TVShows.Where(t => t.Category == DiscoveryCategory.None && t.Added), tvModel =>
                        {
                            try
                            {
                                object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{tvModel.TMDB}?api_key={TMDB_API}");
                                if (jresult != null)
                                {
                                    json = (JObject)jresult;
                                    JArray seasons = (JArray)json["seasons"];
                                    Parallel.ForEach(seasons, season =>
                                    {
                                        int season_number = int.Parse((string)season["season_number"]);
                                        try
                                        {
                                            SeasonModel seasonModel = tvModel.GetSeasonByNumber(season_number);
                                            if (seasonModel == null)
                                            {
                                                log.Debug($"Adding Ghost Season for {tvModel.Title} Season {season_number}");
                                                seasonModel = tvModel.AddSeason(season_number);
                                            }
                                            JObject seasonJson = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{tvModel.TMDB}/season/{season_number}?api_key={TMDB_API}");
                                            if (seasonJson != null)
                                            {
                                                JArray episodes = (JArray)seasonJson["episodes"];
                                                Parallel.ForEach(episodes, episode =>
                                                {
                                                    int episode_number = int.Parse((string)episode["episode_number"]);
                                                    try
                                                    {
                                                        if (seasonModel.GetEpisodeByNumber(episode_number) == null)
                                                        {
                                                            log.Debug($"Adding Ghost Episode for {tvModel.Title} Season {season_number} Episode {episode_number}");
                                                            seasonModel.AddEpisode(episode_number);
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        log.Error($"Had issues trying to load Ghost Episodes for \"{tvModel.Title}\" Season {season_number} Episode {episode_number}", e);
                                                    }
                                                });
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            log.Error($"Had issues trying to load Ghost Episodes for \"{tvModel.Title}\" Season {season_number}", e);
                                        }
                                    });
                                }
                            }
                            catch (Exception e)
                            {
                                log.Error($"Had issues trying to load Ghost Episodes for \"{tvModel.Title}\"", e);
                            }
                        });
                }
                else
                {
                    try
                    {
                        object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{show.TMDB}?api_key={TMDB_API}");
                        if (jresult != null)
                        {
                            json = (JObject)jresult;
                            JArray seasons = (JArray)json["seasons"];
                            Parallel.ForEach(seasons, season =>
                            {
                                int season_number = int.Parse((string)season["season_number"]);
                                try
                                {
                                    SeasonModel seasonModel = show.GetSeasonByNumber(season_number);
                                    if (seasonModel == null)
                                    {
                                        log.Debug($"Adding Ghost Season for {show.Title} Season {season_number}");
                                        seasonModel = show.AddSeason(season_number);
                                    }
                                    JObject seasonJson = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{show.TMDB}/season/{season_number}?api_key={TMDB_API}");
                                    JArray episodes = (JArray)seasonJson["episodes"];
                                    Parallel.ForEach(episodes, episode =>
                                    {
                                        int episode_number = int.Parse((string)episode["episode_number"]);
                                        try
                                        {
                                            if (seasonModel.GetEpisodeByNumber(episode_number) == null)
                                            {
                                                log.Debug($"Adding Ghost Episode for {show.Title} Season {season_number} Episode {episode_number}");
                                                seasonModel.AddEpisode(episode_number);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            log.Error($"Had issues trying to load Ghost Episodes for \"{show.Title}\" Season {season_number} Episode {episode_number}", e);
                                        }
                                    });
                                }
                                catch (Exception e)
                                {
                                    log.Error($"Had issues trying to load Ghost Episodes for \"{show.Title}\" Season {season_number}", e);
                                }
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error($"Had issues trying to load Ghost Episodes for \"{show.Title}\"", e);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"Had Issues trying to load Ghost Episodes", e);
            }
        }

        public void AddMedia(params TVModel[] shows)
        {
            foreach (var show in shows)
            {
                TVShows.Add(show);
            }
        }

        public SeriesObject[] Discover(User user, DiscoveryCategory category = DiscoveryCategory.popular)
        {
            return GetList(user).Where(t => t.Category == category).ToArray();
        }

        public SeriesObject[] FindSimilar(string id)
        {
            List<SeriesObject> model = new();
            object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{id}/similar?api_key={TMDB_API}");
            if (jresult == null) return model.ToArray();
            JArray results = (JArray)((JObject)jresult)["results"];
            if (results != null && results.Any())
            {
                foreach (JToken result in results)
                {
                    if (result == null || result["release_date"] == null || string.IsNullOrWhiteSpace((string)result["release_date"]))
                    {
                        continue;
                    }

                    model.Add(new SeriesObject(JsonConvert.SerializeObject(result)));
                }
            }
            return model.ToArray();
        }

        public SeriesObject[] GetList(User user)
        {
            List<SeriesObject> model = new();
            foreach (TVModel show in TVShows)
            {
                model.Add(new(show, user));
            }
            return model.ToArray();
        }

        public SeriesObject[] GetLocalList(User user)
        {
            return GetList(user).Where(t => t.Added).ToArray();
        }

        public object[] GetRecentlyAddedList(User user)
        {
            MediaBase[] episode = medias.OrderBy(m => m.ScannedDate).ToArray();
            object[] model = new object[episode.Length > 10 ? 10 : episode.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (episode[i] != null)
                {
                    model[i] = new EpisodeObject((EpisodeModel)episode[i], user);
                }
            }
            return model;
        }

        public TVModel GetShowByTMDB(string tmdb)
        {
            try
            {
                if (TVShows == null) log.Fatal($"TVShows are NULL?");
                foreach (TVModel model in TVShows)
                {
                    if (model.TMDB.Equals(tmdb))
                    {
                        return model;
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"Something went wrong while trying to GetShowByTMDB: {tmdb}", e);
            }
            return null;
        }

        public TVModel GetTVShowByName(string name)
        {
            foreach (TVModel model in TVShows)
            {
                if (model.Title.Equals(name))
                {
                    return model;
                }
            }
            return null;
        }

        public override void Initialize()
        {
            try
            {
                TVShows = new();
                Scanner.ForTV();
            }
            catch (Exception e)
            {
                log.Fatal("Unhandled Exception was found while trying to initialize TV Shows Library", e);
                Environment.Exit(0);
            }
            base.Initialize();
        }

        public override void PostInitializationEvent()
        {
            AddGhostEpisodes();
            log.Debug("Done Processing Ghost Episodes");
        }

        public object[] Search(string query, int year = -1)
        {
            string url;
            if (year != -1)
            {
                url = $"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={query}&year={year}&language=en-US";
            }
            else
            {
                url = $"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={query}&language=en-US";
            }
            object jresult = Functions.GetJsonObjectFromURL(url);
            List<object> model = new();
            if (jresult == null) return model.ToArray();
            JArray results = (JArray)((JObject)jresult)["results"];
            if (results == null)
            {
                return model.ToArray();
            }

            foreach (JToken result in results)
            {
                model.Add(new SeriesObject(JsonConvert.SerializeObject(result)));
            }
            return model.ToArray();
        }

        #endregion Public Methods
    }
}