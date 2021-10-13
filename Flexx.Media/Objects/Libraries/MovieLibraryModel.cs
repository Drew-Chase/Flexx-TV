using Flexx.Media.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
            //object[] model = new object[results.Count];
            List<object> model = new();
            foreach (JToken result in results.Children())
            {
                model.Add(new
                {
                    id = result["id"].ToString(),
                    title = result["title"].ToString(),
                    year = DateTime.Parse(result["release_date"].ToString()),
                    poster = $"https://image.tmdb.org/t/p/original{result["poster_path"]}",
                    cover = $"https://image.tmdb.org/t/p/original{result["backdrop_path"]}",
                    plot = result["overview"].ToString(),
                    rating = double.Parse(result["vote_average"].ToString()),
                    downloaded = GetMovieByTMDB(result["id"].ToString()) != null,
                });
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

                model.Add(new
                {
                    id = result["id"].ToString(),
                    title = result["title"].ToString(),
                    year = DateTime.Parse(result["release_date"].ToString()),
                    poster = $"https://image.tmdb.org/t/p/original{result["poster_path"]}",
                    cover = $"https://image.tmdb.org/t/p/original{result["backdrop_path"]}",
                    plot = result["overview"].ToString(),
                    rating = double.Parse(result["vote_average"].ToString()),
                    downloaded = GetMovieByTMDB(result["id"].ToString()) != null,
                });
            }
            return model.ToArray();
        }

        public object[] GetList()
        {
            object[] model = new object[medias.Count];
            for (int i = 0; i < model.Length; i++)
            {
                if (medias[i] != null)
                {
                    MovieModel movie = ((MovieModel)medias[i]);
                    model[i] = movie.ModelObject;
                }
            }
            return model;
        }

        public object[] GetContinueWatchingList()
        {
            Interfaces.IMedia[] continueWatching = medias.Where(m => !m.Watched && m.WatchedDuration > 0).ToArray();
            object[] model = new object[continueWatching.Length > 10 ? 10 : continueWatching.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (continueWatching[i] != null)
                {
                    MovieModel movie = ((MovieModel)continueWatching[i]);
                    model[i] = movie.ModelObject;
                }
            }
            return model;
        }

        public object[] GetRecentlyAddedList()
        {
            Interfaces.IMedia[] movies = medias.OrderBy(m => m.ScannedDate).ToArray();
            object[] model = new object[movies.Length > 10 ? 10 : movies.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (movies[i] != null)
                {
                    MovieModel movie = ((MovieModel)movies[i]);
                    model[i] = movie.ModelObject;
                }
            }
            return model;
        }
    }
}