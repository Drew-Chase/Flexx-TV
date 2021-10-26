using Flexx.Core.Authentication;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Libraries
{
    public class TvLibraryModel : LibraryModel
    {
        public static TvLibraryModel Instance = Instance ?? new TvLibraryModel();
        public List<TVModel> TVShows { get; private set; }

        protected TvLibraryModel() : base()
        {
            Instance = this;
        }

        public override void Initialize()
        {
            TVShows = new();
            Scanner.ForTV();
            Task.Run(() => AddGhostEpisodes());
            base.Initialize();
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

        public TVModel GetShowByTMDB(string tmdb)
        {
            foreach (TVModel model in TVShows)
            {
                if (model.TMDB.Equals(tmdb))
                {
                    return model;
                }
            }
            return null;
        }

        public void AddMedia(params TVModel[] shows)
        {
            TVShows.AddRange(shows);
        }

        public object[] GetList(User user)
        {
            object[] model = new object[TVShows.Count];
            for (int i = 0; i < model.Length; i++)
            {
                if (TVShows[i] != null)
                    model[i] = new SeriesObject(TVShows[i], user);
            }
            return model;
        }

        public object[] DiscoverShows(DiscoveryCategory category = DiscoveryCategory.Popular)
        {
            string url = $"https://api.themoviedb.org/3/tv/{category.ToString().ToLower()}?api_key={TMDB_API}&language=en-US";
            JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url)))["results"];
            if (results == null)
            {
                return null;
            }
            List<object> model = new();
            foreach (JToken result in results.Children())
            {
                model.Add(new SeriesObject(JsonConvert.SerializeObject(result)));
            }
            return model.ToArray();
        }

        public object[] SearchForShows(string query, int year = -1)
        {
            string url;
            if (year != -1)
            {
                url = $"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={query}&year={year}";
            }
            else
            {
                url = $"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={query}";
            }

            JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url)))["results"];
            if (results == null)
            {
                return null;
            }

            List<object> model = new();
            foreach (JToken result in results.Children())
            {
                if (string.IsNullOrWhiteSpace(result["release_date"].ToString()))
                {
                    continue;
                }

                model.Add(new SeriesObject(JsonConvert.SerializeObject(result)));
            }
            return model.ToArray();
        }

        public object[] GetContinueWatchingList(User user)
        {
            MediaBase[] continueWatching = medias.Where(m => !user.GetHasWatched($"{((EpisodeModel)m).Season.Series.Title}_{((EpisodeModel)m).FriendlyName}") && user.GetWatchedDuration($"{((EpisodeModel)m).Season.Series.Title}_{((EpisodeModel)m).FriendlyName}") > 0).ToArray();
            object[] model = new object[continueWatching.Length > 10 ? 10 : continueWatching.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (continueWatching[i] != null)
                {
                    model[i] = new EpisodeObject((EpisodeModel)continueWatching[i], user);
                }
            }
            return model;
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

        public void AddGhostEpisodes()
        {
            using WebClient client = new();
            JObject json = null;
            foreach (TVModel tvModel in TVShows)
            {
                json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tvModel.TMDB}?api_key={TMDB_API}"));
                JArray seasons = (JArray)json["seasons"];
                foreach (JToken season in seasons)
                {
                    int season_number = int.Parse(season["season_number"].ToString());
                    SeasonModel seasonModel = tvModel.GetSeasonByNumber(season_number);
                    if (seasonModel == null)
                    {
                        log.Debug($"Adding Ghost Season for {tvModel.Title} Season {season_number}");
                        seasonModel = tvModel.AddSeason(season_number);
                    }
                    JObject seasonJson = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{tvModel.TMDB}/season/{season_number}?api_key={TMDB_API}"));
                    JArray episodes = (JArray)seasonJson["episodes"];
                    foreach (JToken episode in episodes)
                    {
                        int episode_number = int.Parse(episode["episode_number"].ToString());
                        if (seasonModel.GetEpisodeByNumber(episode_number) == null)
                        {
                            log.Debug($"Adding Ghost Episode for {tvModel.Title} Season {season_number} Episode {episode_number}");
                            seasonModel.AddEpisode(episode_number);
                        }
                    }
                }
            }
        }
    }
}