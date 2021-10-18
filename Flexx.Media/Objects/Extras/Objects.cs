using Flexx.Core.Authentication;
using Flexx.Media.Objects.Libraries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Extras
{
    public class MovieObject
    {
        public string ID { get; private set; }
        public string Title { get; private set; }
        public string Plot { get; private set; }
        public string MPAA { get; private set; }
        public double Rating { get; private set; }
        public bool Downloaded { get; private set; }
        public ushort Year { get; private set; }
        public string Poster { get; private set; }
        public string Cover { get; private set; }
        public bool Watched { get; private set; }
        public ushort WatchedDuration { get; private set; }

        public MovieObject(MovieModel movie, User user)
        {
            ID = movie.TMDB;
            Title = movie.Title;
            Plot = movie.Plot;
            MPAA = movie.MPAA;
            Rating = movie.Rating;
            Downloaded = movie.Downloaded;
            Year = (ushort)movie.ReleaseDate.Year;
            Poster = movie.PosterImage;
            Cover = movie.CoverImage;
            Watched = user.GetHasWatched(movie.Title);
            WatchedDuration = user.GetWatchedDuration(movie.Title);
        }

        public MovieObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            ID = result["id"].ToString();
            Title = result["title"].ToString();
            Plot = result["overview"].ToString();
            Year = (ushort)DateTime.Parse(result["release_date"].ToString()).Year;
            Poster = $"https://image.tmdb.org/t/p/original{result["poster_path"]}";
            Cover = $"https://image.tmdb.org/t/p/original{result["backdrop_path"]}";
            Rating = double.Parse(result["vote_average"].ToString());
            Downloaded = MovieLibraryModel.Instance.GetMovieByTMDB(result["id"].ToString()) != null;
            Watched = false;
            WatchedDuration = 0;

            foreach (JToken child in ((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{ID}/release_dates?api_key={TMDB_API}")))["results"].Children().ToList())
            {
                try
                {
                    if (child["iso_3166_1"].ToString().ToLower().Equals("us"))
                    {
                        MPAA = child["release_dates"][0]["certification"].ToString();
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        public class SeriesObject
        {
            public string ID { get; private set; }
            public string Title { get; private set; }
            public string Plot { get; private set; }
            public DateTime ReleaseDate { get; private set; }
            public bool Watched { get; private set; }
            public bool Added { get; private set; }

            public SeriesObject(TVModel show, User user)
            {
                ID = show.TMDB;
                Title = show.Title;
                Plot = show.Plot;
                ReleaseDate = show.StartDate;
                Watched = !show.Seasons.Where(s =>
                {
                    return s.Episodes.Where(e =>
                    {
                        if (!user.GetHasWatched(e.MetaDataKey))
                        {
                            return true;
                        }

                        return false;
                    }).Any();
                }).Any();
                Added = true;
            }

            public SeriesObject(string json)
            {
                JObject result = (JObject)JsonConvert.DeserializeObject(json);
                ID = result["id"].ToString();
                Title = result["name"].ToString();
                Plot = result["overview"].ToString();
                ReleaseDate = DateTime.Parse(result["first_air_date"].ToString());
                Watched = false;
                Added = false;
            }
        }

        public class SeasonObject
        {
            public string Name { get; private set; }
            public string Plot { get; private set; }
            public DateTime ReleaseDate { get; private set; }
            public int Season { get; private set; }
            public int Episodes { get; private set; }
            public string Show { get; private set; }
            public bool Watched { get; private set; }

            public SeasonObject(SeasonModel season, User user)
            {
                Name = season.Title;
                Plot = season.Plot;
                Season = season.Season_Number;
                Episodes = season.Episodes.Count;
                ReleaseDate = season.StartDate;
                Show = season.Series.Title;
                Watched = !season.Episodes.Where(e =>
                {
                    if (!user.GetHasWatched(e.MetaDataKey))
                    {
                        return true;
                    }

                    return false;
                }).Any();
            }

            public SeasonObject(string json)
            {
                JObject result = (JObject)JsonConvert.DeserializeObject(json);
                Name = result["name"].ToString();
                Plot = result["overview"].ToString();
                ReleaseDate = DateTime.Parse(result["air_date"].ToString());
                Watched = false;
                Episodes = 0;
            }
        }

        public class EpisodeObject
        {
            public string ID { get; private set; }
            public string Title { get; private set; }
            public string Name { get; private set; }
            public string Plot { get; private set; }
            public double Rating { get; private set; }
            public bool Downloaded { get; private set; }
            public DateTime ReleaseDate { get; private set; }
            public string Poster { get; private set; }
            public int Season { get; private set; }
            public int Episode { get; private set; }
            public string Show { get; private set; }
            public bool Watched { get; private set; }
            public ushort WatchedDuration { get; private set; }

            public EpisodeObject(EpisodeModel episode, User user)
            {
                ID = episode.Season.Series.TMDB;
                Title = episode.Title;
                Name = episode.FriendlyName;
                Plot = episode.Plot;
                Rating = episode.Rating;
                Downloaded = episode.Downloaded;
                ReleaseDate = episode.ReleaseDate;
                Poster = episode.PosterImage;
                Show = episode.Season.Series.Title;
                Season = episode.Season.Season_Number;
                Episode = episode.Episode_Number;
                Watched = user.GetHasWatched(episode.MetaDataKey);
                WatchedDuration = user.GetWatchedDuration(episode.MetaDataKey);
            }

            public EpisodeObject(string json)
            {
                JObject result = (JObject)JsonConvert.DeserializeObject(json);
                ID = result["id"].ToString();
                Title = result["name"].ToString();
                Name = "";
                Plot = result["overview"].ToString();
                ReleaseDate = DateTime.Parse(result["air_date"].ToString());
                Rating = double.Parse(result["vote_average"].ToString());
                Downloaded = false;
                Watched = false;
                WatchedDuration = 0;
            }
        }
    }
}