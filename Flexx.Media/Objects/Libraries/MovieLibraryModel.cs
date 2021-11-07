using Flexx.Core.Authentication;
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
    public class MovieLibraryModel : LibraryModel
    {
        public static MovieLibraryModel Instance = Instance ?? new MovieLibraryModel();

        protected MovieLibraryModel() : base()
        {
        }

        public override void Initialize()
        {
            try
            {
                Scanner.ForMovies();
            }
            catch (Exception e)
            {
                log.Fatal("Unhandled Exception was found while trying to initialize Movies Library", e);
            }
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

        public MovieObject[] DiscoverMovies(User user, DiscoveryCategory category = DiscoveryCategory.Latest)
        {
            return GetList(user).Where(m => m.Category == category).ToArray();
        }

        public void FetchAllTrailers()
        {
            log.Info($"Fetching All Movie Trailers");
            Parallel.ForEach(medias, movie =>
            {
                ((MovieModel)movie).GetTrailer();
            });
            log.Warn($"Done Fetching Movie Trailers");
        }

        public MovieObject[] SearchForMovies(string query, int year = -1)
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
            object jresult = Functions.GetJsonObjectFromURL(url);
            List<MovieObject> model = new();
            if (jresult == null)
                return model.ToArray();
            JArray results = (JArray)((JObject)jresult)["results"];
            if (results != null && results.Any())
            {
                foreach (JToken result in results)
                {
                    if (result == null || result["release_date"] == null || string.IsNullOrWhiteSpace(result["release_date"].ToString()))
                    {
                        continue;
                    }

                    model.Add(new MovieObject(JsonConvert.SerializeObject(result)));
                }
            }
            return model.ToArray();
        }

        public MovieObject[] FindSimilar(string id)
        {
            List<MovieObject> model = new();
            object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{id}/similar?api_key={TMDB_API}");
            if (jresult == null) return model.ToArray();
            JArray results = (JArray)((JObject)jresult)["results"];
            if (results != null && results.Any())
            {
                foreach (JToken result in results)
                {
                    if (result == null || result["release_date"] == null || string.IsNullOrWhiteSpace(result["release_date"].ToString()))
                    {
                        continue;
                    }

                    model.Add(new MovieObject(JsonConvert.SerializeObject(result)));
                }
            }
            return model.ToArray();
        }

        public MovieObject[] GetList(User user)
        {
            List<MovieObject> list = new();
            foreach (MovieModel movie in medias.ToArray())
            {
                list.Add(new(movie, user));
            }
            return list.OrderBy(m => m.Title).ToArray();
        }

        public MovieObject[] GetLocalList(User user)
        {
            List<MovieObject> list = new();
            foreach (MovieModel movie in medias.ToArray())
            {
                if (movie.Downloaded && !string.IsNullOrWhiteSpace(movie.PATH) && movie.MediaInfo.Size > 0)
                {
                    list.Add(new(movie, user));
                }
            }
            return list.OrderBy(m => m.Title).ToArray();
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