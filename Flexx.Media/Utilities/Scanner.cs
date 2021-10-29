using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TorrentTitleParser;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Utilities
{
    public enum ScanFrequency
    {
        Daily,
        Weekly,
        WeekDays,
        WeekEnds,
    }

    public class Scanner
    {

        public static void ScheduleScannerTask(ScanFrequency frequency = ScanFrequency.Daily)
        {
            Timer scannerTask;
            TimeSpan span = TimeSpan.FromHours(24);

            switch (frequency)
            {
                case ScanFrequency.Daily:
                    span = TimeSpan.FromHours(24);
                    break;

                case ScanFrequency.Weekly:
                    span = TimeSpan.FromDays(7);
                    break;
            }
            scannerTask = new(s =>
            {
                if (!((!(DateTime.Now.DayOfWeek.Equals(DayOfWeek.Saturday) || DateTime.Now.DayOfWeek.Equals(DayOfWeek.Sunday)) && frequency == ScanFrequency.WeekEnds) || ((DateTime.Now.DayOfWeek.Equals(DayOfWeek.Saturday) || DateTime.Now.DayOfWeek.Equals(DayOfWeek.Sunday)) && frequency == ScanFrequency.WeekDays)))
                {
                    ForMovies();
                }
            }, null, TimeSpan.Zero, span);
        }

        #region Movies

        public static void ForMovies()
        {
            log.Info("Scanning for Movies");
            List<MovieModel> model = new();
            log.Warn(config.MovieLibraryPath);
            string[] files = Directory.GetFiles(config.MovieLibraryPath, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    if (Media_Extensions.Contains(f.Split(".")[^1]))
                    {
                        log.Debug($"Found file \"{f}\"");
                        if (new FileInfo(f).Length == 0)
                        {
                            log.Error($"File \"{f}\" was corrupted and will not be added");
                            return false;
                        }
                        return true;
                    }
                    return false;
                }).ToArray();
            Parallel.ForEach(files, file =>
            {
                MovieLibraryModel.Instance.AddMedia(PopulateMovieAsync(file).Result);
            });
            Task.Run(() => PrefetchMovies()).ContinueWith(a =>
            {
                Task.Run(() => MovieLibraryModel.Instance.FetchAllTrailers());
            });
        }

        private static Task<MovieModel> PopulateMovieAsync(string file)
        {
            return Task.Run(() => PopulateMovie(file));
        }

        private static MovieModel PopulateMovie(string file)
        {
            return new(file);
        }
        private static void PrefetchMovies()
        {
            log.Info($"Prefetching Movies");
            Parallel.ForEach(Directory.GetFiles(Paths.MovieData, "prefetch.metadata", SearchOption.AllDirectories), file =>
            {
                try
                {
                    MovieLibraryModel.Instance.AddMedia(new MovieModel(new ChaseLabs.CLConfiguration.List.ConfigManager(file)));
                }
                catch (Exception e)
                {
                    log.Error("Issue with Prefetching Local Movies", e);
                }
            });
            foreach (DiscoveryCategory category in Enum.GetValues(typeof(DiscoveryCategory)))
            {
                if (category == DiscoveryCategory.None) continue;
                log.Debug($"Prefetching {category} Movies");
                string url = $"https://api.themoviedb.org/3/movie/{category.ToString().ToLower()}?api_key={TMDB_API}&language=en-US";
                JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url)))["results"];
                if (results == null || !results.Any())
                {
                    continue;
                }
                Parallel.ForEach(results, result =>
                {
                    if (result["id"] != null && MovieLibraryModel.Instance.GetMovieByTMDB(result["id"].ToString()) == null)
                        try
                        {
                            MovieLibraryModel.Instance.AddMedia(new MovieModel(result["id"].ToString(), category));
                        }
                        catch (Exception e)
                        {
                            log.Error("Issue with Prefetching Remote Movies", e);
                        }
                });
            }

            log.Warn($"Done Prefetching Movies");
        }
        #endregion
        #region TV
        public static void ForTV()
        {
            log.Info("Scanning for TV Shows");

            string[] files = Directory.GetFiles(config.TVLibraryPath, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    if (Media_Extensions.Contains(f.Split(".")[^1]))
                    {
                        log.Debug($"Found file \"{f}\"");
                        if (new FileInfo(f).Length == 0)
                        {
                            log.Error($"File was corrupted!");
                            return false;
                        }
                        return true;
                    }
                    return false;
                }).ToArray();
            foreach (string file in files)
            {
                TvLibraryModel.Instance.AddMedia(PopulateTVAsync(file).Result);
            }
            Task.Run(() => PrefetchTV()).ContinueWith(a =>
            {
                Task.Run(() => TvLibraryModel.Instance.AddGhostEpisodes());
            });
        }

        private static Task<TVModel[]> PopulateTVAsync(params string[] files)
        {
            return Task.Run(() => PopulateTV(files));
        }

        private static TVModel[] PopulateTV(params string[] files)
        {
            List<TVModel> models = new();
            foreach (string file in files)
            {
                TVModel show = null;
                SeasonModel season = null;
                Torrent torrentData = new(new FileInfo(file).Name);
                string tmdb = string.Empty;
                string title = torrentData.Title;
                int year = torrentData.Year;
                int season_number = torrentData.Season;
                int episode_number = torrentData.Episode;
                JObject json = null;
                show = TvLibraryModel.Instance.GetTVShowByName(title);
                if (show == null)
                {
                    using (WebClient client = new())
                    {
                        json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={title}&first_air_date_year={year}"));
                        if (json["results"].Children().Any())
                        {
                            tmdb = json["results"][0]["id"].ToString();
                        }
                        else
                        {
                            json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={title}"));
                            if (json["results"].Children().Any())
                            {
                                tmdb = json["results"][0]["id"].ToString();
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(tmdb))
                    {
                        show = new(tmdb);
                        TvLibraryModel.Instance.AddMedia(show);
                    }
                }
                if (show != null)
                {
                    season = show.GetSeasonByNumber(season_number);
                    if (season == null)
                    {
                        season = show.AddSeason(season_number);
                    }
                    else
                    {
                        TvLibraryModel.Instance.AddMedia(season.AddEpisode(file, episode_number));
                    }
                }
            }
            return models.ToArray();
        }

        private static void PrefetchTV()
        {
            log.Info($"Prefetching TV Shows");
            Parallel.ForEach(Directory.GetFiles(Paths.TVData, "prefetch.metadata", SearchOption.AllDirectories), file =>
            {
                try
                {
                    TvLibraryModel.Instance.AddMedia(new TVModel(new ChaseLabs.CLConfiguration.List.ConfigManager(file)));
                }
                catch (Exception e)
                {
                    log.Error("Issue with Prefetching Local TV Shows", e);
                }
            });
            foreach (DiscoveryCategory category in Enum.GetValues(typeof(DiscoveryCategory)))
            {
                if (category == DiscoveryCategory.None) continue;
                log.Debug($"Prefetching {category} TV Shows");
                string url = $"https://api.themoviedb.org/3/tv/{category.ToString().ToLower()}?api_key={TMDB_API}&language=en-US";
                JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url)))["results"];
                if (results == null || !results.Any())
                {
                    continue;
                }
                Parallel.ForEach(results, result =>
                {
                    if (result["id"] != null && TvLibraryModel.Instance.GetShowByTMDB(result["id"].ToString()) == null)
                    {
                        TVModel model = null;
                        try
                        {
                            model = new(result["id"].ToString(), category);
                        }
                        catch (Exception e)
                        {
                            log.Error("Issue with Prefetching Remote TV Shows", e);
                        }
                        if (model != null)
                            TvLibraryModel.Instance.AddMedia(model);
                    }
                });

            }

            log.Warn($"Done Prefetching TV Shows");
        }
        #endregion

        /// <summary>
        /// Splits an array into several smaller arrays.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="array">The array to split.</param>
        /// <param name="size">The size of the smaller arrays.</param>
        /// <returns>An array containing smaller arrays.</returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(T[] array, int size)
        {
            for (int i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }
    }
}