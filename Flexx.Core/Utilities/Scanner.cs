using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TorrentTitleParser;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Utilities
{
    public class Scanner
    {
        public static void ScheduleScannerTask()
        {
            if (!DateTime.TryParse(config.NextScheduledPrefetch, out DateTime time) || time > DateTime.Now)
            {
                ForMovies();
                ForTV();
                Prefetch(true, true); // Movies
                Prefetch(false, true); // TV Shows
                config.NextScheduledPrefetch = DateTime.Now.AddHours(24).ToString("HH:mm:ss MM-dd-yyy");
            }
        }

        #region Movies

        public static void ForMovies()
        {
            log.Debug("Scanning for Movies");
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
                            return false;
                        }
                        return true;
                    }
                    return false;
                }).ToArray();
            Parallel.ForEach(files, new() { MaxDegreeOfParallelism = 24 }, file =>
              {
                  try
                  {
                      MovieLibraryModel.Instance.AddMedia(PopulateMovieAsync(file).Result);
                  }
                  catch (Exception e)
                  {
                      log.Error($"Had trouble loading file \"{file}\"", e);
                  }
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

        //private static void PrefetchMovies(bool force = false)
        //{
        //    log.Debug($"Prefetching Movies");
        //    string prefetch_dir = Directory.CreateDirectory(Path.Combine(Paths.MovieData, "Prefetch")).FullName;
        //    if (force)
        //    {
        //        Directory.Delete(prefetch_dir, true);
        //        Directory.CreateDirectory(prefetch_dir);
        //    }
        //    Parallel.ForEach(Directory.GetFiles(prefetch_dir, "prefetch.metadata", SearchOption.AllDirectories), file =>
        //    {
        //        try
        //        {
        //            MovieLibraryModel.Instance.AddMedia(new MovieModel(new ChaseLabs.CLConfiguration.List.ConfigManager(file)));
        //        }
        //        catch (Exception e)
        //        {
        //            log.Error("Issue with Prefetching Local Movies", e);
        //        }
        //    });
        //    foreach (DiscoveryCategory category in Enum.GetValues(typeof(DiscoveryCategory)))
        //    {
        //        if (category == DiscoveryCategory.None)
        //        {
        //            continue;
        //        }

        //        log.Debug($"Prefetching {category} Movies");
        //        string url = $"https://api.themoviedb.org/3/movie/{category.ToLower()}?api_key={TMDB_API}&language=en-US";
        //        JArray results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
        //        if (results == null || !results.Any())
        //        {
        //            continue;
        //        }
        //        Parallel.ForEach(results, result =>
        //        {
        //            if (result["id"] != null && MovieLibraryModel.Instance.GetMovieByTMDB(result["id"]) == null)
        //            {
        //                try
        //                {
        //                    MovieLibraryModel.Instance.AddMedia(new MovieModel(result["id"], category));
        //                }
        //                catch (Exception e)
        //                {
        //                    log.Error("Issue with Prefetching Remote Movies", e);
        //                }
        //            }
        //        });
        //    }

        //    log.Debug($"Done Prefetching Movies");
        //}

        #endregion Movies

        #region TV

        public static void ForTV()
        {
            log.Debug("Scanning for TV Shows");

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
                            tmdb = (string)json["results"][0]["id"];
                        }
                        else
                        {
                            json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={title}"));
                            if (json["results"].Children().Any())
                            {
                                tmdb = (string)json["results"][0]["id"];
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
        #endregion TV


        public static void Prefetch(bool movies, bool force = false)
        {
            string prefetch_dir = Directory.CreateDirectory(Path.Combine(movies ? Paths.MovieData : Paths.TVData, "Prefetch")).FullName;
            if (force)
            {
                Directory.Delete(prefetch_dir, true);
                Directory.CreateDirectory(prefetch_dir);
            }
            log.Debug($"Loading Cached Prefetched Data");
            Parallel.ForEach(Directory.GetFiles(prefetch_dir, "prefetch.metadata", SearchOption.AllDirectories), file =>
            {
                try
                {
                    ChaseLabs.CLConfiguration.List.ConfigManager data = new(file);
                    if (data.GetConfigByKey("id") != null)
                    {
                        if (movies)
                        {
                            var m = new MovieModel(data);
                            log.Debug($"Found Cached Data for \"{m.Title}\"");
                            MovieLibraryModel.Instance.AddMedia(m);
                        }
                        else
                        {
                            var m = new TVModel(data);
                            log.Debug($"Found Cached Data for \"{m.Title}\"");
                            TvLibraryModel.Instance.AddMedia(m);
                        }
                    }
                    else
                    {
                        log.Debug($"Deleting \"{file}\"");
                        File.Delete(file);
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Issue with Loading {(movies ? "Movies" : "TV Shows")} Cached Prefetches", e);
                }
            });
            foreach (DiscoveryCategory category in Enum.GetValues(typeof(DiscoveryCategory)))
            {
                if (category == DiscoveryCategory.None)
                {
                    continue;
                }

                log.Debug($"Prefetching {category} {(movies ? "Movies" : "TV Shows")}");
                string url = $"https://api.themoviedb.org/3/{(movies ? "movie" : "tv")}/{category}?api_key={TMDB_API}&language={config.LanguagePreference}";

                JArray results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
                if (results == null || !results.Any())
                {
                    continue;
                }
                Parallel.ForEach(results, result =>
                {
                    if (result["id"] != null && !string.IsNullOrWhiteSpace((string)result["id"]) && movies ? (MovieLibraryModel.Instance.GetMovieByTMDB((string)result["id"]) == null) : (TvLibraryModel.Instance.GetShowByTMDB((string)result["id"]) == null))
                    {
                        object model = null;
                        try
                        {
                            model = movies ? new MovieModel((string)result["id"], category) : new TVModel((string)result["id"], category);
                        }
                        catch (Exception e)
                        {
                            log.Error($"Issue with Prefetching Remote {(movies ? "Movies" : "TV Shows")}", e);
                        }
                        if (model != null)
                        {
                            if (movies)
                            {
                                MovieLibraryModel.Instance.AddMedia((MovieModel)model);
                            }
                            else
                            {
                                TvLibraryModel.Instance.AddMedia((TVModel)model);
                            }
                        }
                    }
                });
            }
        }

    }
}