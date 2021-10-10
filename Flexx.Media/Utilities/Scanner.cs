using System;
using System.Collections.Generic;
using System.IO;
using Flexx.Media.Objects;
using System.Linq;
using static Flexx.Core.Data.Global;
using System.Threading;
using System.Threading.Tasks;
using Flexx.Media.Objects.Libraries;
using Newtonsoft.Json.Linq;
using System.Net;
using Newtonsoft.Json;
using TorrentTitleParser;

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
        private Timer scannerTask;

        public void ScheduleScannerTask(ScanFrequency frequency = ScanFrequency.Daily)
        {
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

        public static void ForMovies()
        {
            log.Info("Scanning for Movies");
            List<MovieModel> model = new();

            string[] files = Directory.GetFiles(config.MovieLibraryPath, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    if (Media_Extensions.Contains(f.Split(".")[^1]))
                    {
                        log.Debug($"Found file \"{f}\"");
                        if (new FileInfo(f).Length == 0)
                        {
                            log.Error($"File \"{f}\" was corrupted and will not be added");
                            //return false;
                        }
                        return true;
                    }
                    return false;
                }).ToArray();

            if (files.Length < 21)
            {
                foreach (string file in files)
                {
                    MovieLibraryModel.Instance.AddMedia(PopulateMovieAsync(file).Result);
                }
            }
            else
            {
                foreach (var i in Split(files, 3))
                {
                    Task.Run(() =>
                    {
                        foreach (var j in i)
                        {
                            MovieLibraryModel.Instance.AddMedia(PopulateMovieAsync(j).Result);
                        }
                    });
                }
            }
        }
        private static Task<MovieModel> PopulateMovieAsync(string file) => Task.Run(() => PopulateMovie(file));
        private static MovieModel PopulateMovie(string file) => new(file);

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
                            //return false;
                        }
                        return true;
                    }
                    return false;
                }).ToArray();

            //if (files.Length < 21)
            //{
            foreach (string file in files)
            {
                TvLibraryModel.Instance.AddMedia(PopulateTVAsync(file).Result);
            }
            //}
            //else
            //{
            //    foreach (var i in Split(files, 3))
            //    {
            //        Task.Run(() =>
            //        {
            //            foreach (var j in i)
            //            {
            //                TvLibraryModel.Instance.AddMedia(PopulateTVAsync(j).Result);
            //            }
            //        });
            //    }
            //}

        }
        private static Task<TVModel[]> PopulateTVAsync(params string[] files) => Task.Run(() => PopulateTV(files));
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

        /// <summary>
        /// Splits an array into several smaller arrays.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="array">The array to split.</param>
        /// <param name="size">The size of the smaller arrays.</param>
        /// <returns>An array containing smaller arrays.</returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(T[] array, int size)
        {
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }

    }
}
