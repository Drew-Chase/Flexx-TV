using Flexx.Authentication;
using Flexx.Media.Objects.Libraries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using static Flexx.Data.Global;

namespace Flexx.Media.Objects.Extras
{
    public class EpisodeObject
    {
        #region Public Constructors

        public EpisodeObject(EpisodeModel episode, User user)
        {
            if (episode == null) return;
            ID = episode.Season.Series.TMDB;
            Title = episode.Title;
            Name = episode.FriendlyName;
            Plot = episode.Plot;
            Rating = episode.Rating;
            Downloaded = episode.Downloaded;
            ReleaseDate = episode.ReleaseDate;
            Show = episode.Season.Series.Title;
            Season = episode.Season.Season_Number;
            Episode = episode.Episode_Number;
            Watched = user.GetHasWatched(episode);
            WatchedDuration = user.GetWatchedDuration(episode);
            if (episode.Downloaded)
                WatchedPercentage = (byte)Math.Floor(WatchedDuration / episode.MediaInfo.Duration.TotalSeconds * 100);

            EpisodeModel next = null;
            EpisodeModel previous = null;

            foreach (SeasonModel s in episode.Season.Series.Seasons)
            {
                if (s.Season_Number > episode.Season.Season_Number) continue;
                if (next != null) break;
                foreach (EpisodeModel e in s.Episodes)
                {
                    if (!e.Downloaded || episode.Episode_Number >= e.Episode_Number) continue;
                    if (next != null) break;
                    next = e;
                    break;
                }
            }

            for (int i = episode.Season.Series.Seasons.Count - 1; i > 0; i--)
            {
                SeasonModel s = episode.Season.Series.Seasons[i];
                if (s.Season_Number > episode.Season.Season_Number) continue;
                if (previous != null) break;
                for (int j = s.Episodes.Count - 1; j > 0; j--)
                {
                    EpisodeModel e = s.Episodes[j];
                    if (!e.Downloaded || episode.Episode_Number <= e.Episode_Number) continue;
                    if (previous != null) break;
                    previous = e;
                    break;
                }
            }

            if (next != null)
            {
                NextEpisode = new
                {
                    Season = next.Season.Season_Number,
                    Episode = next.Episode_Number,
                    Name = next.FriendlyName,
                };
            }

            if (previous != null)
            {
                PreviousEpisode = new
                {
                    Season = previous.Season.Season_Number,
                    Episode = previous.Episode_Number,
                    Name = previous.FriendlyName,
                };
            }
            Versions = episode.AlternativeVersions;
        }

        public EpisodeObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            ID = (string)result["id"];
            Title = (string)result["name"];
            Season = int.TryParse((string)result["season_number"], out int season) ? season : 0;
            Episode = int.TryParse((string)result["episode_number"], out int episode) ? episode : 0;
            Name = $"S{(Season < 10 ? "0" + Season : Season)}E{(Episode < 10 ? "0" + Episode : Episode)}";
            Plot = (string)result["overview"];
            if (DateTime.TryParse((string)result["air_date"], out DateTime date))
            {
                ReleaseDate = date;
            }

            Rating = double.Parse((string)result["vote_average"]);
            Downloaded = false;
            Watched = false;
            WatchedDuration = 0;
            Versions = null;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool Downloaded { get; }

        public int Episode { get; }

        public string ID { get; }

        public string Name { get; }

        public object NextEpisode { get; }

        public string Plot { get; }

        public object PreviousEpisode { get; }

        public double Rating { get; }

        public DateTime ReleaseDate { get; }

        public int Season { get; }

        public string Show { get; }

        public string Title { get; }

        public string Type => "tv";

        public MediaVersion[] Versions { get; }

        public bool Watched { get; }

        public ushort WatchedDuration { get; }

        public byte WatchedPercentage { get; }

        #endregion Public Properties
    }

    public class MovieObject
    {
        #region Public Constructors

        public MovieObject(MovieModel movie, User user)
        {
            ID = movie.TMDB;
            Title = movie.Title;
            Plot = movie.Plot;
            MPAA = movie.MPAA;
            Rating = movie.Rating;
            Downloaded = movie.Downloaded;
            Year = (ushort)movie.ReleaseDate.Year;
            Watched = user.GetHasWatched(movie);
            WatchedDuration = user.GetWatchedDuration(movie);
            if (movie.Downloaded)
                WatchedPercentage = (byte)Math.Floor(WatchedDuration / movie.MediaInfo.Duration.TotalSeconds * 100);
            MainCast = movie.Cast.GetCast();
            Category = movie.Category;
            FullDuration = movie.FullDuration;
            Versions = movie.AlternativeVersions;
        }

        public MovieObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            ID = (string)result["id"];
            Title = (string)result["title"];
            Plot = (string)result["overview"];
            if (DateTime.TryParse((string)result["release_date"], out DateTime date))
            {
                Year = (ushort)date.Year;
            }

            Rating = double.Parse((string)result["vote_average"]);
            Downloaded = MovieLibraryModel.Instance.GetMovieByTMDB((string)result["id"]) != null;
            Watched = false;
            WatchedDuration = 0;
            Category = DiscoveryCategory.None;
            object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{ID}/release_dates?api_key={TMDB_API}");
            if (jresult != null)
            {
                foreach (JToken child in ((JObject)jresult)["results"].Children())
                {
                    try
                    {
                        if (((string)child["iso_3166_1"]).ToLower().Equals("us"))
                        {
                            MPAA = (string)child["release_dates"][0]["certification"];
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            else
            {
                MPAA = "NR";
            }

            MainCast = new CastListModel("movie", ID).GetCast();
        }

        #endregion Public Constructors

        #region Public Properties

        public DiscoveryCategory Category { get; }

        public bool Downloaded { get; }

        public string FullDuration { get; }

        public string ID { get; }

        public CastModel[] MainCast { get; }

        public string MPAA { get; }

        public string Plot { get; }

        public double Rating { get; }

        public string Title { get; }

        public string Type => "movie";

        public MediaVersion[] Versions { get; }

        public bool Watched { get; }

        public ushort WatchedDuration { get; }

        public byte WatchedPercentage { get; }

        public ushort Year { get; }

        #endregion Public Properties
    }

    public class SeasonObject
    {
        #region Public Constructors

        public SeasonObject(SeasonModel season, User user)
        {
            season.Episodes = season.Episodes.OrderBy(e => e.Episode_Number).ToList();
            Name = season.Title;
            Plot = season.Plot;
            Season = season.Season_Number;
            Episodes = season.Episodes.Count;
            ReleaseDate = season.StartDate;
            Show = season.Series.Title;
            Watched = !season.Episodes.Where(e =>
            {
                if (!user.GetHasWatched(e))
                {
                    return true;
                }

                return false;
            }).Any();
            EpisodeModel next = null;
            foreach (EpisodeModel e in season.Episodes)
            {
                if (!e.Downloaded || user.GetHasWatched(e)) continue;
                next = e;
                break;
            }
            if (next == null)
            {
                foreach (EpisodeModel e in season.Episodes)
                {
                    if (!e.Downloaded) continue;
                    next = e;
                    break;
                }
            }
            if (next != null)
            {
                UpNext = new
                {
                    next.Season.Season_Number,
                    Episode = next.Episode_Number,
                    Name = next.FriendlyName,
                };
            }
        }

        public SeasonObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            Name = (string)result["name"];
            Plot = (string)result["overview"];
            if (DateTime.TryParse((string)result["air_date"], out DateTime date))
            {
                ReleaseDate = date;
            }

            Watched = false;
            if (result["episode_count"] != null)
            {
                Episodes = int.TryParse((string)result["episode_count"], out int episode) ? episode : 0;
            }

            if (result["season_number"] != null)
            {
                Season = int.TryParse((string)result["season_number"], out int season) ? season : 0;
            }
        }

        #endregion Public Constructors

        #region Public Properties

        public int Episodes { get; }

        public string Name { get; }

        public string Plot { get; }

        public DateTime ReleaseDate { get; }

        public int Season { get; }

        public string Show { get; }

        public string Type => "tv";

        public object UpNext { get; }

        public bool Watched { get; }

        #endregion Public Properties
    }

    public class SeriesObject
    {
        #region Public Constructors

        public SeriesObject(TVModel show, User user)
        {
            if (show == null) return;
            ID = show.TMDB;
            Title = show.Title;
            Plot = show.Plot;
            ReleaseDate = show.StartDate;
            Year = ReleaseDate.Year;
            Watched = !show.Seasons.Where(s =>
            {
                return s.Episodes.Where(e =>
                {
                    if (!user.GetHasWatched(e))
                    {
                        return true;
                    }

                    return false;
                }).Any();
            }).Any();
            Added = show.Added;
            Category = show.Category;

            Seasons = show.Seasons.Count;

            EpisodeModel next = null;
            if (show.Added)
            {
                foreach (var season in show.Seasons)
                {
                    Episodes += season.Episodes.Count;
                    foreach (var episode in season.Episodes)
                    {
                        if (next != null) break;
                        if (!episode.Downloaded || user.GetHasWatched(episode)) continue;
                        next = episode;
                        break;
                    }
                }
            }

            if (next != null)
            {
                UpNext = new
                {
                    Season = next.Season.Season_Number,
                    Episode = next.Episode_Number,
                    Name = next.FriendlyName,
                };
            }

            MPAA = show.MPAA;
            Rating = show.Rating;
            MainCast = show.MainCast.GetCast().Take(10).ToArray();
        }

        public SeriesObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            ID = (string)result["id"];
            Title = (string)result["name"];
            Plot = (string)result["overview"];
            if (DateTime.TryParse((string)result["first_air_date"], out DateTime date))
            {
                ReleaseDate = date;
                Year = ReleaseDate.Year;
            }

            Watched = false;
            Added = TvLibraryModel.Instance.GetShowByTMDB(ID) != null && TvLibraryModel.Instance.GetShowByTMDB(ID).Added;
            Rating = (string)result["vote_average"];
            object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{ID}/content_ratings?api_key={TMDB_API}&language=en-US");
            if (jresult != null)
            {
                foreach (JToken token in (JArray)((JObject)jresult)["results"])
                {
                    if (((string)token["iso_3166_1"]).Equals("US"))
                    {
                        MPAA = (string)token["rating"];
                        break;
                    }
                }
            }
            else
            {
                MPAA = "NR";
            }

            MainCast = new CastListModel("tv", ID).GetCast().Take(10).ToArray();
            Category = DiscoveryCategory.None;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool Added { get; }

        public DiscoveryCategory Category { get; }

        public int Episodes { get; }

        public string ID { get; }

        public CastModel[] MainCast { get; }

        public string MPAA { get; }

        public string Plot { get; }

        public string Rating { get; }

        public DateTime ReleaseDate { get; }

        public int Seasons { get; }

        public string Title { get; }

        public string Type => "tv";

        public object UpNext { get; }

        public bool Watched { get; }

        public int Year { get; }

        #endregion Public Properties
    }
}