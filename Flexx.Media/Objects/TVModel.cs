using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects.Extras;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects
{
    public class TVModel
    {
        public string TMDB { get; private set; }
        public string Title { get; private set; }
        public string Plot { get; private set; }
        public string Studio { get; private set; }
        public string Rating { get; private set; }
        public string MPAA { get; private set; }
        public bool InProduction { get; private set; }
        public DateTime StartDate { get; private set; }
        public CastListModel MainCast { get; private set; }
        public List<SeasonModel> Seasons { get; private set; }
        public bool Added { get; private set; }

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
                string tmp = Path.Combine(Paths.TempData, $"sp_{TMDB}.jpg");
                new System.Net.WebClient().DownloadFile(value, tmp);
                Transcoder.OptimizePoster(tmp, path);
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
                string tmp = Path.Combine(Paths.TempData, $"sc_{TMDB}.jpg");
                new System.Net.WebClient().DownloadFile(value, tmp);
                Transcoder.OptimizeCover(tmp, path);
            }
        }

        public string CoverImageWithLanguage
        {
            get
            {
                string path = Path.Combine(Metadata_Directory, "cover-lang.jpg");
                if (!File.Exists(path))
                {
                    return CoverImage;
                }

                return path;
            }
            set
            {
                try
                {
                    log.Debug($"Optimizing Language Cover Image for {TMDB}");
                    string path = Path.Combine(Metadata_Directory, "cover-lang.jpg");
                    string tmp = Path.Combine(Paths.TempData, $"scl_{TMDB}.jpg");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    Transcoder.OptimizeCover(tmp, path);
                }
                catch { }
            }
        }

        public string LogoImage
        {
            get
            {
                string path = Path.Combine(Metadata_Directory, "logo.png");
                return path;
            }
            set
            {
                try
                {
                    log.Debug($"Optimizing Logo Image for {TMDB}");
                    string path = Path.Combine(Metadata_Directory, "logo.png");
                    string tmp = Path.Combine(Paths.TempData, $"ml_{TMDB}.png");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    Transcoder.OptimizeLogo(tmp, path);
                }
                catch { }
            }
        }

        public ConfigManager Metadata { get; set; }

        public string Metadata_Directory;
        public DiscoveryCategory Category { get; set; }

        public TVModel(string tmdb, DiscoveryCategory category = DiscoveryCategory.None)
        {
            TMDB = tmdb;
            Seasons = new();
            Category = category;
            if (category == DiscoveryCategory.None)
            {
                Metadata_Directory = Path.Combine(Paths.TVData, TMDB);
#if DEBUG
                Metadata = new(Path.Combine(Metadata_Directory, "metadata"), false, "FlexxTV");
#else
                Metadata = new(Path.Combine(Metadata_Directory, "metadata"), true, "FlexxTV");
#endif
                Added = true;
            }
            else
            {
                Metadata_Directory = Path.Combine(Paths.TVData, "Prefetch", TMDB);
#if DEBUG
                Metadata = new(Path.Combine(Metadata_Directory, "prefetch.metadata"), false, "FlexxTV");
#else
                Metadata = new(Path.Combine(Metadata_Directory, "prefetch.metadata"), true, "FlexxTV");
#endif
                Added = false;
            }
            LoadMetaData();
            MainCast = new("tv", TMDB);
        }

        public TVModel(ConfigManager metadata)
        {
            Metadata = metadata;
            Metadata_Directory = Directory.GetParent(metadata.PATH).FullName;
            Seasons = new();
            Category = Metadata.GetConfigByKey("category") != null ? (Enum.TryParse(typeof(DiscoveryCategory), Metadata.GetConfigByKey("category").Value, out object cat) ? (DiscoveryCategory)cat : DiscoveryCategory.None) : DiscoveryCategory.None;
            if (Metadata.GetConfigByKey("id") == null)
            {
                UpdateMetaData();
            }
            else
            {
                TMDB = Metadata.GetConfigByKey("id").Value;
                LoadMetaData();
            }
            MainCast = new("tv", TMDB);
        }

        private void LoadMetaData()
        {
            if (Metadata.Size() == 0 || Metadata.GetConfigByKey("id") == null || Metadata.GetConfigByKey("title") == null || Metadata.GetConfigByKey("plot") == null)
            {
                UpdateMetaData();
            }
            else
            {
                Title = Metadata.GetConfigByKey("title").Value;
                Plot = Metadata.GetConfigByKey("plot").Value;
                Added = Metadata.GetConfigByKey("added").Value;
                if (Metadata.GetConfigByKey("studio") != null)
                {
                    Studio = Metadata.GetConfigByKey("studio").Value;
                }
                if (Metadata.GetConfigByKey("mpaa") != null)
                {
                    MPAA = Metadata.GetConfigByKey("mpaa").Value;
                }
                if (Metadata.GetConfigByKey("rating") != null)
                {
                    Rating = Metadata.GetConfigByKey("rating").Value;
                }

                if (Metadata.GetConfigByKey("in_production") != null)
                {
                    InProduction = Metadata.GetConfigByKey("in_production").Value;
                }

                if (Metadata.GetConfigByKey("start_date") != null)
                {
                    StartDate = DateTime.Parse(Metadata.GetConfigByKey("start_date").Value);
                }
                if (Metadata.GetConfigByKey("category") != null)
                {
                    Category = Enum.TryParse(typeof(DiscoveryCategory), Metadata.GetConfigByKey("category").Value, out object cat) ? (DiscoveryCategory)cat : DiscoveryCategory.None;
                }
            }
        }

        public void UpdateMetaData()
        {
            JObject json = null;
            try
            {
                json = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{TMDB}?api_key={TMDB_API}");
            }
            catch (Exception e)
            {
                log.Error($"Unable to gather metadata for TV Show with ID of {TMDB}", e);
                return;
            }
            try
            {
                JObject imagesJson = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{TMDB}/images?api_key={TMDB_API}&include_image_language=en");
                if (imagesJson["backdrops"].Any())
                {
                    CoverImageWithLanguage = $"https://image.tmdb.org/t/p/original{imagesJson["backdrops"][0]["file_path"]}";
                }

                if (imagesJson["logos"].Any())
                {
                    LogoImage = $"https://image.tmdb.org/t/p/original{imagesJson["logos"][0]["file_path"]}";
                }
            }
            catch (Exception e)
            {
                log.Error($"Unable to get Image metadata for TV Show with ID of {TMDB}", e);
            }

            Title = json["name"].ToString();
            Plot = json["overview"].ToString();
            Rating = json["vote_average"].ToString();
            try
            {
                string j = new WebClient().DownloadString($"https://api.themoviedb.org/3/tv/{TMDB}/content_ratings?api_key={TMDB_API}");
                foreach (JToken token in (JArray)((JObject)JsonConvert.DeserializeObject(j))["results"])
                {
                    if (token["iso_3166_1"].ToString().Equals("US"))
                    {
                        MPAA = token["rating"].ToString();
                        break;
                    }
                }
            }
            catch
            {
                MPAA = "NR";
            }
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
            if (json["poster_path"] != null && !string.IsNullOrWhiteSpace(json["poster_path"].ToString()))
            {
                PosterImage = $"https://image.tmdb.org/t/p/original{json["poster_path"]}";
            }

            if (json["backdrop_path"] != null && !string.IsNullOrWhiteSpace(json["backdrop_path"].ToString()))
            {
                CoverImage = $"https://image.tmdb.org/t/p/original{json["backdrop_path"]}";
            }

            Metadata.Add("id", TMDB);
            Metadata.Add("title", Title);
            Metadata.Add("added", Added);
            Metadata.Add("plot", Plot);
            Metadata.Add("category", Category.ToString());
            Metadata.Add("studio", Studio);
            Metadata.Add("mpaa", MPAA);
            Metadata.Add("rating", Rating);
            Metadata.Add("in_production", InProduction);
            Metadata.Add("start_date", StartDate.ToString("yyyy-MM-dd"));
        }

        public void AddToFlexx()
        {
            Added = true;
            if (Metadata.GetConfigByKey("added") == null)
            {
                Metadata.Add("added", true);
            }
            else
            {
                Metadata.GetConfigByKey("added").Value = true;
            }

            TvLibraryModel.Instance.AddGhostEpisodes(this);
            ScanForMissing();
        }

        public void ScanForMissing()
        {
            Parallel.ForEach(Seasons, season =>
            {
                season.ScanForMissing();
            });
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
        private readonly List<EpisodeModel> _episodes;
        public List<EpisodeModel> Episodes { get; set; }

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
            _episodes = new();
            Episodes = new();

#if DEBUG
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"));
#else
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"), true);
#endif
            LoadMetaData();
            Episodes = Episodes.OrderBy(e => e.Episode_Number).ToList();
        }

        public EpisodeModel GetEpisodeByNumber(int episode)
        {
            Episodes = Episodes.OrderBy(e => e.Episode_Number).ToList();
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
                    json = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/tv/{Series.TMDB}/season/{Season_Number}?api_key={TMDB_API}");
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

        public void ScanForMissing()
        {
            Parallel.ForEach(Episodes, episode =>
            {
                episode.ScanForMissing();
            });
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

        public EpisodeModel(int number, SeasonModel season) : base()
        {
            Season = season;
            Episode_Number = number;
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"), false);
            LoadMetaData();
        }

        public EpisodeModel(string path, int number, SeasonModel season) : base()
        {
            PATH = path;
            Season = season;
            Episode_Number = number;
            Metadata = new(Path.Combine(Metadata_Directory, "metadata"), false);
            LoadMetaData();
        }

        private void LoadMetaData()
        {
            TMDB = Season.Series.TMDB;
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

            Downloaded = !string.IsNullOrWhiteSpace(PATH) && File.Exists(PATH);
            if (Downloaded)
            {
                MediaInfo = FFmpeg.GetMediaInfo(PATH).Result;
                FullDuration = $"{MediaInfo.Duration.Hours}h {MediaInfo.Duration.Minutes}m";
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
                    string url = $"https://api.themoviedb.org/3/tv/{TMDB}/season/{Season.Season_Number}/episode/{Episode_Number}?api_key={TMDB_API}";
                    json = (JObject)Functions.GetJsonObjectFromURL(url);
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

        public void ScanForMissing()
        {
        }
    }
}