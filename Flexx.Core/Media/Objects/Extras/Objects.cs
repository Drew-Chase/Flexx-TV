﻿using Flexx.Authentication;
using Flexx.Media.Objects.Libraries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Extras
{
    public class MovieObject
    {
        public string ID { get; }
        public string Title { get; }
        public string Plot { get; }
        public string MPAA { get; }
        public double Rating { get; }
        public bool Downloaded { get; }
        public ushort Year { get; }
        public string Poster { get; }
        public string Cover { get; }
        public bool Watched { get; }
        public ushort WatchedDuration { get; }
        public byte WatchedPercentage { get; }
        public string FullDuration { get; }
        public CastModel[] MainCast { get; }
        public DiscoveryCategory Category { get; }
        public MediaVersion[] Versions { get; }

        public MovieObject(MovieModel movie, User user)
        {
            ID = movie.TMDB;
            Title = movie.Title;
            Plot = movie.Plot;
            MPAA = movie.MPAA;
            Rating = movie.Rating;
            Downloaded = movie.Downloaded;
            Year = (ushort)movie.ReleaseDate.Year;
            Watched = user.GetHasWatched(movie.Title);
            WatchedDuration = user.GetWatchedDuration(movie.Title);
            if (movie.Downloaded)
                WatchedPercentage = (byte)Math.Floor(WatchedDuration / movie.MediaInfo.Duration.TotalSeconds * 100);
            MainCast = movie.Cast.GetCast().ToArray();
            Category = movie.Category;
            FullDuration = movie.FullDuration;
            Versions = movie.AlternativeVersions;
        }

        public MovieObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            ID = result["id"].ToString();
            Title = result["title"].ToString();
            Plot = result["overview"].ToString();
            if (DateTime.TryParse(result["release_date"].ToString(), out DateTime date))
            {
                Year = (ushort)date.Year;
            }

            Poster = $"https://image.tmdb.org/t/p/original{result["poster_path"]}";
            Cover = $"https://image.tmdb.org/t/p/original{result["backdrop_path"]}";
            Rating = double.Parse(result["vote_average"].ToString());
            Downloaded = MovieLibraryModel.Instance.GetMovieByTMDB(result["id"].ToString()) != null;
            Watched = false;
            WatchedDuration = 0;
            Category = DiscoveryCategory.None;
            object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{ID}/release_dates?api_key={TMDB_API}");
            if (jresult != null)
            {
                foreach (JToken child in ((JObject)jresult)["results"].Children().ToList())
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
            else
            {
                MPAA = "NR";
            }

            MainCast = new CastListModel("movie", ID).GetCast().ToArray();
        }
    }

    public class SeriesObject
    {
        public string ID { get; }
        public string Title { get; }
        public string Plot { get; }
        public DateTime ReleaseDate { get; }
        public int Year { get; }
        public bool Watched { get; }
        public bool Added { get; }
        public string MPAA { get; }
        public string Rating { get; }
        public DiscoveryCategory Category { get; }

        public object UpNext { get; }
        public CastModel[] MainCast { get; }
        public int Seasons { get; }
        public int Episodes { get; }

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
                    if (!user.GetHasWatched(e.MetaDataKey))
                    {
                        return true;
                    }

                    return false;
                }).Any();
            }).Any();
            Added = show.Added;
            Category = show.Category;
            if (show.Added)
            {
                SeasonModel[] UnwatchedSeasons = show.Seasons.Where(s => !new SeasonObject(s, user).Watched && new SeasonObject(s, user).Season != 0).ToArray();
                SeasonModel UnwatchedSeason = UnwatchedSeasons.Length == 0 ? show.Seasons.ElementAt(0) : UnwatchedSeasons[0];
                EpisodeModel[] UnwatchedEpisodes = UnwatchedSeason.Episodes.Where(e => !new EpisodeObject(e, user).Watched && new EpisodeObject(e, user).Downloaded).ToArray();
                EpisodeModel UnwatchedEpisode = UnwatchedEpisodes.Length == 0 ? UnwatchedSeason.Episodes.ElementAt(0) : UnwatchedEpisodes[0];
                UpNext = new
                {
                    Season = UnwatchedEpisode.Season.Season_Number,
                    Episode = UnwatchedEpisode.Episode_Number,
                    Name = UnwatchedEpisode.FriendlyName,
                };
            }
            Seasons = show.Seasons.Count;
            foreach (var season in show.Seasons)
            {
                Episodes += season.Episodes.Count;
            }
            MPAA = show.MPAA;
            Rating = show.Rating;
            MainCast = show.MainCast.GetCast().Take(10).ToArray();
        }

        public SeriesObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            ID = result["id"].ToString();
            Title = result["name"].ToString();
            Plot = result["overview"].ToString();
            if (DateTime.TryParse(result["first_air_date"].ToString(), out DateTime date))
            {
                ReleaseDate = date;
                Year = ReleaseDate.Year;
            }

            Watched = false;
            Added = TvLibraryModel.Instance.GetShowByTMDB(ID) != null && TvLibraryModel.Instance.GetShowByTMDB(ID).Added;
            Rating = result["vote_average"].ToString();
            object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{ID}/content_ratings?api_key={TMDB_API}&language=en-US");
            if (jresult != null)
            {
                foreach (JToken token in (JArray)((JObject)jresult)["results"])
                {
                    if (token["iso_3166_1"].ToString().Equals("US"))
                    {
                        MPAA = token["rating"].ToString();
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
    }

    public class SeasonObject
    {
        public string Name { get; }
        public string Plot { get; }
        public DateTime ReleaseDate { get; }
        public int Season { get; }
        public int Episodes { get; }
        public string Show { get; }
        public bool Watched { get; }
        public object UpNext { get; }

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
                if (!user.GetHasWatched(e.MetaDataKey))
                {
                    return true;
                }

                return false;
            }).Any();
            EpisodeModel[] UnwatchedEpisodes = season.Episodes.Where(e => !new EpisodeObject(e, user).Watched).ToArray();
            EpisodeModel UnwatchedEpisode = UnwatchedEpisodes.Length == 0 ? season.Episodes.ElementAt(0) : UnwatchedEpisodes[0];
            UpNext = new
            {
                Season = UnwatchedEpisode.Season.Season_Number,
                Episode = UnwatchedEpisode.Episode_Number,
                Name = UnwatchedEpisode.FriendlyName,
            };
        }

        public SeasonObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            Name = result["name"].ToString();
            Plot = result["overview"].ToString();
            if (DateTime.TryParse(result["air_date"].ToString(), out DateTime date))
            {
                ReleaseDate = date;
            }

            Watched = false;
            if (result["episode_count"] != null)
            {
                Episodes = int.TryParse(result["episode_count"].ToString(), out int episode) ? episode : 0;
            }

            if (result["season_number"] != null)
            {
                Season = int.TryParse(result["season_number"].ToString(), out int season) ? season : 0;
            }
        }
    }

    public class EpisodeObject
    {
        public string ID { get; }
        public string Title { get; }
        public string Name { get; }
        public string Plot { get; }
        public double Rating { get; }
        public bool Downloaded { get; }
        public DateTime ReleaseDate { get; }
        public string Poster { get; }
        public int Season { get; }
        public int Episode { get; }
        public string Show { get; }
        public bool Watched { get; }
        public ushort WatchedDuration { get; }
        public object NextEpisode { get; }
        public MediaVersion[] Versions { get; }

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

            int currentIndex = episode.Season.Episodes.IndexOf(episode);
            int seasonIndex = episode.Season.Series.Seasons.IndexOf(episode.Season);
            EpisodeModel next = episode.Season.Episodes.Count <= currentIndex ? episode.Season.Episodes.ElementAt(currentIndex + 1) : episode.Season.Series.Seasons.Count <= seasonIndex ? episode.Season.Series.Seasons.ElementAt(seasonIndex + 1).Episodes.ElementAt(0) : null;
            if (next != null)
            {
                NextEpisode = new
                {
                    Season = next.Season.Season_Number,
                    Episode = next.Episode_Number,
                    Name = next.FriendlyName,
                };
            }
            Versions = episode.AlternativeVersions;
        }

        public EpisodeObject(string json)
        {
            JObject result = (JObject)JsonConvert.DeserializeObject(json);
            ID = result["id"].ToString();
            Title = result["name"].ToString();
            Season = int.TryParse(result["season_number"].ToString(), out int season) ? season : 0;
            Episode = int.TryParse(result["episode_number"].ToString(), out int episode) ? episode : 0;
            Name = $"S{(Season < 10 ? "0" + Season : Season)}E{(Episode < 10 ? "0" + Episode : Episode)}";
            Plot = result["overview"].ToString();
            if (DateTime.TryParse(result["air_date"].ToString(), out DateTime date))
            {
                ReleaseDate = date;
            }

            Rating = double.Parse(result["vote_average"].ToString());
            Downloaded = false;
            Watched = false;
            WatchedDuration = 0;
            Versions = null;
        }
    }
}