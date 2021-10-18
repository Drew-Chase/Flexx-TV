using Flexx.Core.Authentication;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Libraries
{
    public class MovieLibraryModel : LibraryModel
    {
        public static MovieLibraryModel Instance = Instance ?? new MovieLibraryModel();

        protected MovieLibraryModel() : base()
        {
        }

        public override void Initialize()
        {
            Scanner.ForMovies();
            base.Initialize();
        }

        public MovieModel GetMovieByTMDB(string tmdb)
        {
            foreach (MovieModel movie in medias)
            {
                if (movie.TMDB == null)
                {
                    RemoveMedia(movie);
                    continue;
                }
                if (movie.TMDB.Equals(tmdb))
                {
                    return movie;
                }
            }
            return null;
        }

        public object[] DiscoverMovies(DiscoveryCategory category = DiscoveryCategory.Latest)
        {
            string url = $"https://api.themoviedb.org/3/movie/{category.ToString().ToLower()}?api_key={TMDB_API}";
            JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url)))["results"];
            if (results == null)
            {
                return null;
            }
            List<object> model = new();
            foreach (JToken result in results.Children())
            {
                model.Add(new MovieObject(JsonConvert.SerializeObject(result)));
            }
            return model.ToArray();
        }

        public object[] SearchForMovies(string query, int year = -1)
        {
            string url;
            if (year != -1)
            {
                url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={query}&year={year}";
            }
            else
            {
                url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={query}";
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

                model.Add(new MovieObject(JsonConvert.SerializeObject(result)));
            }
            return model.ToArray();
        }

        public object[] GetList(User user)
        {
            object[] model = new object[medias.Count];
            for (int i = 0; i < model.Length; i++)
            {
                if (medias[i] != null)
                {
                    model[i] = new MovieObject((MovieModel)medias[i], user);
                }
            }
            return model;
        }

        public object[] GetContinueWatchingList(User user)
        {
            MediaBase[] continueWatching = medias.Where(m => !user.GetHasWatched(m.Title) && user.GetWatchedDuration(m.Title) > 0).ToArray();
            object[] model = new object[continueWatching.Length > 10 ? 10 : continueWatching.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (continueWatching[i] != null)
                {
                    model[i] = new MovieObject((MovieModel)continueWatching[i], user);
                }
            }
            return model;
        }

        public object[] GetRecentlyAddedList(User user)
        {
            MediaBase[] movies = medias.OrderBy(m => m.ScannedDate).ToArray();
            object[] model = new object[movies.Length > 10 ? 10 : movies.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (movies[i] != null)
                {
                    model[i] = new MovieObject((MovieModel)movies[i], user);
                }
            }
            return model;
        }
    }
}