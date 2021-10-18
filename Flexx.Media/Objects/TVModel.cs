﻿using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects.Extras;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects
{
    public class TVModel
    {
        public string TMDB { get; private set; }
        public string Title { get; private set; }
        public string Plot { get; private set; }
        public string Studio { get; private set; }
        public bool InProduction { get; private set; }
        public DateTime StartDate { get; private set; }
        public CastListModel MainCast { get; private set; }
        public List<SeasonModel> Seasons { get; private set; }

        public string PosterImage
        {
            get
            {
                string path = Path.Combine(Metadata_Directory, "poster.jpg");
                if (!File.Exists(path))
                {
                    UpdateMetaData();
                }

                return path;
            }
            set
            {
                string path = Path.Combine(Metadata_Directory, "poster.jpg");
                new WebClient().DownloadFile(value, path);
            }
        }

        public string CoverImage
        {
            get
            {
                string path = Path.Combine(Metadata_Directory, "cover.jpg");
                if (!File.Exists(path))
                {
                    UpdateMetaData();
                }

                return path;
            }
            set
            {
                string path = Path.Combine(Metadata_Directory, "cover.jpg");
                new WebClient().DownloadFile(value, path);
            }
        }

        public ConfigManager Metadata { get; set; }

        public string Metadata_Directory => Path.Combine(Paths.MetaData, "TV", TMDB);

        public TVModel(string tmdb)
        {
            TMDB = tmdb;
            Seasons = new();
#if DEBUG
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"), false, "FlexxTV");
#else
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"), true, "FlexxTV");
#endif
            LoadMetaData();
        }

        private void LoadMetaData()
        {
            if (Metadata.Size() == 0 || Metadata.GetConfigByKey("title") == null || Metadata.GetConfigByKey("plot") == null)
            {
                UpdateMetaData();
            }
            else
            {
                Title = Metadata.GetConfigByKey("title").Value;
                Plot = Metadata.GetConfigByKey("plot").Value;
                if (Metadata.GetConfigByKey("studio") != null)
                {
                    Studio = Metadata.GetConfigByKey("studio").Value;
                }

                if (Metadata.GetConfigByKey("in_production") != null)
                {
                    InProduction = Metadata.GetConfigByKey("in_production").Value;
                }

                if (Metadata.GetConfigByKey("start_date") != null)
                {
                    StartDate = DateTime.Parse(Metadata.GetConfigByKey("start_date").Value);
                }
            }
        }

        public void UpdateMetaData()
        {
            JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{TMDB}?api_key={TMDB_API}"));

            Title = json["name"].ToString();
            Plot = json["overview"].ToString();
            try
            {
                Studio = json["networks"][0]["name"].ToString();
            }
            catch { }
            try
            {
                InProduction = (bool)json["in_production"];
            }
            catch { }
            try
            {
                StartDate = DateTime.Parse(json["first_air_date"].ToString());
            }
            catch { }
            PosterImage = $"https://image.tmdb.org/t/p/original{json["poster_path"]}";
            CoverImage = $"https://image.tmdb.org/t/p/original{json["backdrop_path"]}";

            Metadata.Add("title", Title);
            Metadata.Add("plot", Plot);
            Metadata.Add("studio", Studio);
            Metadata.Add("in_production", InProduction);
            Metadata.Add("start_date", StartDate.ToString("yyyy-MM-dd"));
        }

        public SeasonModel GetSeasonByNumber(int season)
        {
            foreach (SeasonModel model in Seasons)
            {
                if (model.Season_Number == season)
                {
                    return model;
                }
            }
            return null;
        }

        public SeasonModel AddSeason(int season)
        {
            SeasonModel model = new(season, this);
            Seasons.Add(model);
            Seasons = Seasons.OrderBy(s => s.Season_Number).ToList();
            return model;
        }
    }

    public class SeasonModel
    {
        public TVModel Series { get; private set; }

        public string Title { get; private set; }
        public string Plot { get; private set; }
        public DateTime StartDate { get; private set; }

        public int Season_Number { get; private set; }
        public List<EpisodeModel> Episodes { get; private set; }

        public string Metadata_Directory => Path.Combine(Series.Metadata_Directory, Season_Number.ToString());

        public string PosterImage
        {
            get
            {
                string path = Path.Combine(Metadata_Directory, "poster.jpg");
                if (!File.Exists(path))
                {
                    UpdateMetaData();
                }

                return path;
            }
            set
            {
                string path = Path.Combine(Metadata_Directory, "poster.jpg");
                if (File.Exists(value))
                {
                    File.Copy(value, path, true);
                }
                else
                {
                    new WebClient().DownloadFile(value, path);
                }
            }
        }

        public ConfigManager Metadata { get; set; }

        public SeasonModel(int season_number, TVModel series)
        {
            Series = series;
            Season_Number = season_number;
            Episodes = new();

#if DEBUG
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"));
#else
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"), true);
#endif
            LoadMetaData();
        }

        public EpisodeModel GetEpisodeByNumber(int episode)
        {
            foreach (EpisodeModel model in Episodes)
            {
                if (model.Episode_Number == episode)
                {
                    return model;
                }
            }
            return null;
        }

        public EpisodeModel AddEpisode(int episode)
        {
            EpisodeModel model = new(episode, this);
            Episodes.Add(model);
            Episodes = Episodes.OrderBy(e => e.Episode_Number).ToList();
            return model;
        }

        public EpisodeModel AddEpisode(string file, int episode)
        {
            EpisodeModel model = new(file, episode, this);
            Episodes.Add(model);
            Episodes = Episodes.OrderBy(e => e.Episode_Number).ToList();
            return model;
        }

        private void LoadMetaData()
        {
            if (Metadata.Size() == 0 || Metadata.GetConfigByKey("title") == null || Metadata.GetConfigByKey("plot") == null)
            {
                UpdateMetaData();
            }
            else
            {
                Title = Metadata.GetConfigByKey("title").Value;
                Plot = Metadata.GetConfigByKey("plot").Value;
                if (Metadata.GetConfigByKey("start_date") != null)
                {
                    StartDate = DateTime.Parse(Metadata.GetConfigByKey("start_date").Value);
                }
            }
        }

        public void UpdateMetaData()
        {
            try
            {
                JObject json = null;
                try
                {
                    json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{Series.TMDB}/season/{Season_Number}?api_key={TMDB_API}"));
                }
                catch
                {
                    Title = $"Season {Season_Number}";
                    Plot = Series.Plot;
                    StartDate = Series.StartDate;
                    PosterImage = Series.PosterImage;
                }
                if (json != null)
                {
                    Title = json["name"].ToString();
                    Plot = json["overview"].ToString();
                    try
                    {
                        PosterImage = $"https://image.tmdb.org/t/p/original{json["poster_path"]}";
                        StartDate = DateTime.Parse(json["air_date"].ToString());
                    }
                    catch
                    {
                        StartDate = Series.StartDate;
                        PosterImage = Series.PosterImage;
                    }
                }
            }
            catch
            {
                if (string.IsNullOrWhiteSpace(Title))
                {
                    Title = $"Season {Season_Number}";
                }

                if (string.IsNullOrWhiteSpace(Plot))
                {
                    Plot = Series.Plot;
                }
            }
            Metadata.Add("title", Title);
            Metadata.Add("plot", Plot);
            Metadata.Add("start_date", StartDate.ToString("yyyy-MM-dd"));
        }
    }

    public class EpisodeModel : MediaBase
    {
        public override string PosterImage
        {
            get
            {
                string path = Path.Combine(Metadata_Directory, "poster.jpg");
                if (!File.Exists(path))
                {
                    UpdateMetaData();
                }

                return path;
            }
            set
            {
                string path = Path.Combine(Metadata_Directory, "poster.jpg");
                if (File.Exists(value))
                {
                    File.Copy(value, path, true);
                }
                else
                {
                    new WebClient().DownloadFile(value, path);
                }
            }
        }

        public string MetaDataKey => $"{Season.Series.Title}_{FriendlyName}";

        public SeasonModel Season { get; private set; }
        public int Episode_Number { get; private set; }
        public string FriendlyName => $"S{(Season.Season_Number < 10 ? "0" + Season.Season_Number : Season.Season_Number)}E{(Episode_Number < 10 ? "0" + Episode_Number : Episode_Number)}";
        public string Metadata_Directory => Path.Combine(Season.Metadata_Directory, Episode_Number.ToString());

        public object ModelObject => new
        {
            id = Season.Series.TMDB,
            title = Title,
            name = FriendlyName,
            plot = Plot,
            season = Season.Season_Number,
            episode = Episode_Number,
            poster = Season.Series.PosterImage,
            cover = Season.Series.CoverImage,
        };

        public EpisodeModel(int number, SeasonModel season)
        {
            Season = season;
            Episode_Number = number;
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"), false);
            LoadMetaData();
        }

        public EpisodeModel(string path, int number, SeasonModel season)
        {
            PATH = path;
            Season = season;
            Episode_Number = number;
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"), false);
            LoadMetaData();
        }

        private void LoadMetaData()
        {
            if (Metadata.Size() == 0 || Metadata.GetConfigByKey("title") == null || Metadata.GetConfigByKey("plot") == null)
            {
                UpdateMetaData();
            }
            else
            {
                Title = Metadata.GetConfigByKey("title").Value;
                Plot = Metadata.GetConfigByKey("plot").Value;
                if (Metadata.GetConfigByKey("release_date") != null)
                {
                    ReleaseDate = DateTime.Parse(Metadata.GetConfigByKey("release_date").Value);
                }

                if (Metadata.GetConfigByKey("scanned_date") != null)
                {
                    ScannedDate = DateTime.Parse(Metadata.GetConfigByKey("scanned_date").Value);
                }
            }
        }

        public override void UpdateMetaData()
        {
            try
            {
                ScannedDate = DateTime.Now;
                JObject json = null;
                try
                {
                    string url = $"https://api.themoviedb.org/3/tv/{Season.Series.TMDB}/season/{Season.Season_Number}/episode/{Episode_Number}?api_key={TMDB_API}";
                    json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString(url));
                }
                catch
                {
                    Title = $"Episode {Episode_Number}";
                    Plot = Season.Series.Plot;
                    ReleaseDate = Season.StartDate;
                    PosterImage = Season.PosterImage;
                }
                if (json != null)
                {
                    Title = json["name"].ToString();
                    Plot = json["overview"].ToString();
                    try
                    {
                        PosterImage = $"https://image.tmdb.org/t/p/original{json["still_path"]}";
                        ReleaseDate = DateTime.Parse(json["air_date"].ToString());
                    }
                    catch
                    {
                        ReleaseDate = Season.StartDate;
                        PosterImage = Season.PosterImage;
                    }
                }
            }
            catch
            {
                if (string.IsNullOrWhiteSpace(Title))
                {
                    Title = $"Episode {Episode_Number}";
                }

                if (string.IsNullOrWhiteSpace(Plot))
                {
                    Plot = Season.Plot;
                }
            }
            Metadata.Add("title", Title);
            Metadata.Add("plot", Plot);
            Metadata.Add("release_date", ReleaseDate.ToString("MM-dd-yyyy"));
            Metadata.Add("scanned_date", ScannedDate.ToString("MM-dd-yyyy"));
        }

        public override bool ScanForDownloads(out string[] links)
        {
            throw new NotImplementedException();
        }

        public override void AddToTorrentClient(bool useInternal = true)
        {
            throw new NotImplementedException();
        }
    }
}