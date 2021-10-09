using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Flexx.Media.Interfaces;
using Flexx.Media.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Libraries
{
    public class TvLibraryModel : LibraryModel
    {
        public static TvLibraryModel Instance = Instance ?? new TvLibraryModel();
        public List<TVModel> TVShows { get; private set; }
        protected TvLibraryModel() : base()
        {

        }
        public override void Initialize()
        {
            TVShows = new();
            Scanner.ForTV();
            base.Initialize();
        }
        public TVModel GetTVShowByName(string name)
        {
            foreach (TVModel model in TVShows)
            {
                if (model.Title.Equals(name)) return model;
            }
            return null;
        }
        public TVModel GetShowByTMDB(string tmdb)
        {
            foreach (TVModel model in TVShows)
            {
                if (model.TMDB.Equals(tmdb)) return model;
            }
            return null;
        }
        public void AddMedia(params TVModel[] shows)
        {
            TVShows.AddRange(shows);
        }


        public object[] GetList()
        {
            object[] model = new object[TVShows.Count];
            for (int i = 0; i < model.Length; i++)
            {
                if (TVShows[i] != null)
                {
                    TVModel show = TVShows[i];
                    model[i] = new
                    {
                        id = show.TMDB,
                        title = show.Title,
                        year = show.StartDate.Year,
                        seasons = show.Seasons.Count,
                    };

                }
            }
            return model;
        }


        public object[] DiscoverShows(DiscoveryCategory category = DiscoveryCategory.Popular)
        {
            string url = $"https://api.themoviedb.org/3/tv/{category.ToString().ToLower()}?api_key={TMDB_API}";
            JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url)))["results"];
            if (results == null)
                return null;
            //object[] model = new object[results.Count];
            List<object> model = new();
            foreach (JToken result in results.Children())
            {
                model.Add(new
                {
                    id = result["id"].ToString(),
                    title = result["name"].ToString(),
                    year = DateTime.Parse(result["first_air_date"].ToString()).Year,
                    poster = $"https://image.tmdb.org/t/p/original{result["poster_path"]}",
                    cover = $"https://image.tmdb.org/t/p/original{result["backdrop_path"]}",
                    plot = result["overview"].ToString(),
                    rating = double.Parse(result["vote_average"].ToString()),
                    downloaded = GetShowByTMDB(result["id"].ToString()) != null,
                });
            }
            return model.ToArray();
        }

        public object[] SearchForShows(string query, int year = -1)
        {
            string url;
            if (year != -1)
                url = $"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={query}&year={year}";
            else
                url = $"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={query}";
            JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url)))["results"];
            if (results == null) return null;
            List<object> model = new();
            foreach (JToken result in results.Children())
            {
                if (string.IsNullOrWhiteSpace(result["release_date"].ToString()))
                    continue;
                model.Add(new
                {
                    id = result["id"].ToString(),
                    title = result["name"].ToString(),
                    year = DateTime.Parse(result["first_air_date"].ToString()).Year,
                    poster = $"https://image.tmdb.org/t/p/original{result["poster_path"]}",
                    cover = $"https://image.tmdb.org/t/p/original{result["backdrop_path"]}",
                    plot = result["overview"].ToString(),
                    rating = double.Parse(result["vote_average"].ToString()),
                    downloaded = GetShowByTMDB(result["id"].ToString()) != null,
                });
            }
            return model.ToArray();
        }

        public object[] GetContinueWatchingList()
        {
            var continueWatching = medias.Where(m => !m.Watched && m.WatchedDuration > 0).ToArray();
            object[] model = new object[continueWatching.Length > 10 ? 10 : continueWatching.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (continueWatching[i] != null)
                {
                    EpisodeModel episode = ((EpisodeModel)continueWatching[i]);
                    model[i] = episode.ModelObject;
                }
            }
            return model;
        }
        public object[] GetRecentlyAddedList()
        {
            var shows = medias.OrderBy(m => m.ScannedDate).ToArray();
            object[] model = new object[shows.Length > 10 ? 10 : shows.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (shows[i] != null)
                {
                    EpisodeModel episode = ((EpisodeModel)shows[i]);
                    model[i] = episode.ModelObject;
                }
            }
            return model;
        }

    }
}
