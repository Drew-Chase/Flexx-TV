﻿using Flexx.Core.Authentication;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Libraries
{
    public class TvLibraryModel : LibraryModel
    {
        public static TvLibraryModel Instance = Instance ?? new TvLibraryModel();
        public List<TVModel> TVShows { get; private set; }

        protected TvLibraryModel() : base()
        {
            Instance = this;
        }

        public override void Initialize()
        {
            try
            {
                TVShows = new();
                Scanner.ForTV();
            }
            catch (Exception e)
            {
                log.Fatal("Unhandled Exception was found while trying to initialize TV Shows Library", e);
                Environment.Exit(0);
            }
            base.Initialize();
        }

        public TVModel GetTVShowByName(string name)
        {
            foreach (TVModel model in TVShows)
            {
                if (model.Title.Equals(name))
                {
                    return model;
                }
            }
            return null;
        }

        public TVModel GetShowByTMDB(string tmdb)
        {
            foreach (TVModel model in TVShows)
            {
                if (model.TMDB.Equals(tmdb))
                {
                    return model;
                }
            }
            return null;
        }

        public void AddMedia(params TVModel[] shows)
        {
            TVShows.AddRange(shows);
        }

        public SeriesObject[] GetLocalList(User user) => GetList(user).Where(t => t.Added).ToArray();
        public SeriesObject[] GetList(User user)
        {
            List<SeriesObject> model = new();
            foreach (var show in TVShows)
            {
                model.Add(new(show, user));
            }
            return model.ToArray();
        }

        public SeriesObject[] DiscoverShows(User user, DiscoveryCategory category = DiscoveryCategory.Popular)
        {
            return GetList(user).Where(t => t.Category == category).ToArray();
        }

        public object[] SearchForShows(string query, int year = -1)
        {
            string url;
            if (year != -1)
            {
                url = $"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={query}&year={year}&language=en-US";
            }
            else
            {
                url = $"https://api.themoviedb.org/3/search/tv?api_key={TMDB_API}&query={query}&language=en-US";
            }

            JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url)))["results"];
            if (results == null)
            {
                return null;
            }

            List<object> model = new();
            foreach (JToken result in results)
            {
                model.Add(new SeriesObject(JsonConvert.SerializeObject(result)));
            }
            return model.ToArray();
        }

        public object[] GetContinueWatchingList(User user)
        {
            MediaBase[] continueWatching = medias.Where(m => !user.GetHasWatched($"{((EpisodeModel)m).Season.Series.Title}_{((EpisodeModel)m).FriendlyName}") && user.GetWatchedDuration($"{((EpisodeModel)m).Season.Series.Title}_{((EpisodeModel)m).FriendlyName}") > 0).ToArray();
            object[] model = new object[continueWatching.Length > 10 ? 10 : continueWatching.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (continueWatching[i] != null)
                {
                    model[i] = new EpisodeObject((EpisodeModel)continueWatching[i], user);
                }
            }
            return model;
        }

        public object[] GetRecentlyAddedList(User user)
        {
            MediaBase[] episode = medias.OrderBy(m => m.ScannedDate).ToArray();
            object[] model = new object[episode.Length > 10 ? 10 : episode.Length];
            for (int i = 0; i < model.Length; i++)
            {
                if (episode[i] != null)
                {
                    model[i] = new EpisodeObject((EpisodeModel)episode[i], user);
                }
            }
            return model;
        }

        public void AddGhostEpisodes(TVModel show = null)
        {
            try
            {
                JObject json = null;
                if (show == null)
                {

                    Parallel.ForEach(TVShows.Where(t => t.Category == DiscoveryCategory.None), tvModel =>
                        {
                            try
                            {
                                json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tvModel.TMDB}?api_key={TMDB_API}"));
                                JArray seasons = (JArray)json["seasons"];
                                Parallel.ForEach(seasons, season =>
                                {
                                    int season_number = int.Parse(season["season_number"].ToString());
                                    try
                                    {
                                        SeasonModel seasonModel = tvModel.GetSeasonByNumber(season_number);
                                        if (seasonModel == null)
                                        {
                                            log.Debug($"Adding Ghost Season for {tvModel.Title} Season {season_number}");
                                            seasonModel = tvModel.AddSeason(season_number);
                                        }
                                        JObject seasonJson = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{tvModel.TMDB}/season/{season_number}?api_key={TMDB_API}"));
                                        JArray episodes = (JArray)seasonJson["episodes"];
                                        Parallel.ForEach(episodes, episode =>
                                        {
                                            int episode_number = int.Parse(episode["episode_number"].ToString());
                                            try
                                            {
                                                if (seasonModel.GetEpisodeByNumber(episode_number) == null)
                                                {
                                                    log.Debug($"Adding Ghost Episode for {tvModel.Title} Season {season_number} Episode {episode_number}");
                                                    seasonModel.AddEpisode(episode_number);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                log.Error($"Had issues trying to load Ghost Episodes for \"{tvModel.Title}\" Season {season_number} Episode {episode_number}", e);
                                            }
                                        });
                                    }
                                    catch (Exception e)
                                    {
                                        log.Error($"Had issues trying to load Ghost Episodes for \"{tvModel.Title}\" Season {season_number}", e);
                                    }
                                });
                            }
                            catch (Exception e)
                            {
                                log.Error($"Had issues trying to load Ghost Episodes for \"{tvModel.Title}\"", e);
                            }
                        });
                }
                else
                {

                    try
                    {
                        json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{show.TMDB}?api_key={TMDB_API}"));
                        JArray seasons = (JArray)json["seasons"];
                        Parallel.ForEach(seasons, season =>
                        {
                            int season_number = int.Parse(season["season_number"].ToString());
                            try
                            {
                                SeasonModel seasonModel = show.GetSeasonByNumber(season_number);
                                if (seasonModel == null)
                                {
                                    log.Debug($"Adding Ghost Season for {show.Title} Season {season_number}");
                                    seasonModel = show.AddSeason(season_number);
                                }
                                JObject seasonJson = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{show.TMDB}/season/{season_number}?api_key={TMDB_API}"));
                                JArray episodes = (JArray)seasonJson["episodes"];
                                Parallel.ForEach(episodes, episode =>
                                {
                                    int episode_number = int.Parse(episode["episode_number"].ToString());
                                    try
                                    {
                                        if (seasonModel.GetEpisodeByNumber(episode_number) == null)
                                        {
                                            log.Debug($"Adding Ghost Episode for {show.Title} Season {season_number} Episode {episode_number}");
                                            seasonModel.AddEpisode(episode_number);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        log.Error($"Had issues trying to load Ghost Episodes for \"{show.Title}\" Season {season_number} Episode {episode_number}", e);
                                    }
                                });
                            }
                            catch (Exception e)
                            {
                                log.Error($"Had issues trying to load Ghost Episodes for \"{show.Title}\" Season {season_number}", e);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        log.Error($"Had issues trying to load Ghost Episodes for \"{show.Title}\"", e);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"Had Issues trying to load Ghost Episodes", e);
            }
        }
    }
}