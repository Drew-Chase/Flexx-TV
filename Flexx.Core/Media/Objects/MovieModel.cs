using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects.Libraries;
using Flexx.Networking;
using Flexx.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using TorrentTitleParser;
using Xabe.FFmpeg;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using static Flexx.Data.Global;

namespace Flexx.Media.Objects
{
    public class MovieModel : MediaBase
    {
        #region Private Properties

        private string coverBase { get => Metadata.GetConfigByKey("cover").Value; set => Metadata.GetConfigByKey("cover").Value = value; }

        private string coverLanguageBase { get => Metadata.GetConfigByKey("coverLanguage").Value; set => Metadata.GetConfigByKey("coverLanguage").Value = value; }

        private string logoBase { get => Metadata.GetConfigByKey("logo").Value; set => Metadata.GetConfigByKey("logo").Value = value; }

        private string posterBase { get => Metadata.GetConfigByKey("poster").Value; set => Metadata.GetConfigByKey("poster").Value = value; }

        #endregion Private Properties

        #region Private Fields

        private string Metadata_Directory;

        #endregion Private Fields

        #region Public Constructors

        public MovieModel(ConfigManager metadata)
        {
            Metadata = metadata;
            Metadata_Directory = Directory.GetParent(metadata.PATH).FullName;
            if (metadata.GetConfigByKey("id") != null && metadata.GetConfigByKey("category") != null)
            {
                if (Enum.TryParse(typeof(DiscoveryCategory), metadata.GetConfigByKey("category").Value, out object category))
                {
                    Init(metadata.GetConfigByKey("id").Value, true, (DiscoveryCategory)category);
                }
            }
        }

        public MovieModel(string TMDB, DiscoveryCategory Category) : base()
        {
            Metadata = new(Path.Combine(Path.Combine(Paths.MovieData, "Prefetch"), TMDB, "prefetch.metadata"), false);
            Metadata_Directory = Path.Combine(Paths.MovieData, "Prefetch", TMDB);
            Init(TMDB, true, Category);
        }

        public MovieModel(string Initializer, bool IsTMDB = false) : base()
        {
            Init(Initializer, IsTMDB, DiscoveryCategory.None);
        }

        #endregion Public Constructors

        #region Public Properties

        public DiscoveryCategory Category { get; private set; }

        public override string CoverImage
        {
            get
            {
                string path = Path.Combine(Metadata_Directory, "cover.jpg");
                if (string.IsNullOrWhiteSpace(coverBase))
                {
                    UpdateMetaData();
                }

                return coverBase;
            }
            set
            {
                try
                {
                    log.Debug($"Optimizing Cover Image for {TMDB}");
                    string tmp = Path.Combine(Paths.TempData, $"mc_{TMDB}.jpg");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    coverBase = Transcoder.OptimizeCover(tmp);
                }
                catch { }
            }
        }

        public string CoverImageWithLanguage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(coverLanguageBase))
                {
                    return CoverImage;
                }

                return coverLanguageBase;
            }
            set
            {
                try
                {
                    log.Debug($"Optimizing Language Cover Image for {TMDB}");
                    string tmp = Path.Combine(Paths.TempData, $"mcl_{TMDB}.jpg");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    coverLanguageBase = Transcoder.OptimizeCover(tmp);
                }
                catch { }
            }
        }

        public bool HasTrailer { get; private set; }

        public string LogoImage
        {
            get
            {
                return logoBase;
            }
            set
            {
                try
                {
                    log.Debug($"Optimizing Logo Image for {TMDB}");
                    string tmp = Path.Combine(Paths.TempData, $"ml_{TMDB}.png");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    logoBase = Transcoder.OptimizeLogo(tmp);
                }
                catch { }
            }
        }

        public override string PosterImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(posterBase))
                {
                    UpdateMetaData();
                }

                return posterBase;
            }
            set
            {
                try
                {
                    log.Debug($"Optimizing Poster for {TMDB}");
                    string tmp = Path.Combine(Paths.TempData, $"mp_{TMDB}.jpg");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    posterBase = Transcoder.OptimizePoster(tmp);
                }
                catch
                {
                }
            }
        }

        public string TrailerUrl { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public override void AddToTorrentClient(bool useInternal = true)
        {
            if (ScanForDownloads(out string[] links))
            {
                foreach (string link in links)
                {
                    Console.WriteLine(link);
                }
            }
        }

        public void GetTrailer()
        {
            string trailerURL;
            string json;
            try
            {
                json = new WebClient().DownloadString($"https://api.themoviedb.org/3/movie/{TMDB}/videos?api_key={TMDB_API}");
            }
            catch (Exception e)
            {
                log.Error($"Unable to get response regarding {TMDB} trailer", e);
                return;
            }
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            JToken results = ((JObject)JsonConvert.DeserializeObject(json))["results"];
            if (results.Any())
            {
                JToken keyObject = results[0]["key"];
                string key = (string)keyObject;
                if (!string.IsNullOrWhiteSpace(key))
                {
                    try
                    {
                        IVideoStreamInfo streamInfo = new YoutubeClient().Videos.Streams.GetManifestAsync(key).Result.GetMuxedStreams().GetWithHighestVideoQuality();
                        if (streamInfo != null)
                        {
                            trailerURL = streamInfo.Url;
                        }
                        else
                        {
                            trailerURL = "";
                        }
                    }
                    catch
                    {
                        trailerURL = "";
                    }
                }
                else
                {
                    trailerURL = "";
                }
            }
            else
            {
                trailerURL = "";
            }

            TrailerUrl = trailerURL;
            HasTrailer = !string.IsNullOrWhiteSpace(TrailerUrl);
            if (HasTrailer)
            {
                log.Debug($"Found Trailer for {Title}");
            }
            else
            {
                log.Debug($"No Trailer was Found for {Title}");
            }
        }

        public override bool ScanForDownloads(out string[] links)
        {
            links = Indexer.GetMagnetList($"{Title} {ReleaseDate.Year}");
            return links.Length != 0;
        }

        public override void UpdateMetaData()
        {
            if (Metadata == null)
            {
                string path = Path.Combine(Metadata_Directory, "metadata");
                if (File.Exists(path)) File.Delete(path);
                Metadata = new(path);
            }
            log.Debug($"Getting metadata for {TMDB}");
            ScannedDate = DateTime.Now;
            JObject json = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{TMDB}?api_key={TMDB_API}");
            JObject imagesJson = (JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{TMDB}/images?api_key={TMDB_API}&include_image_language=en");
            if (imagesJson["backdrops"].Any())
            {
                CoverImageWithLanguage = $"https://image.tmdb.org/t/p/original{imagesJson["backdrops"][0]["file_path"]}";
            }

            if (imagesJson["logos"].Any())
            {
                LogoImage = $"https://image.tmdb.org/t/p/original{imagesJson["logos"][0]["file_path"]}";
            }

            if (!string.IsNullOrWhiteSpace((string)json["poster_path"]))
            {
                PosterImage = $"https://image.tmdb.org/t/p/original{json["poster_path"]}";
            }

            if (!string.IsNullOrWhiteSpace((string)json["backdrop_path"]))
                CoverImage = $"https://image.tmdb.org/t/p/original{json["backdrop_path"]}";

            if (DateTime.TryParse((string)json["release_date"], out DateTime tmp))
            {
                ReleaseDate = tmp;
            }

            Title = (string)json["title"];
            Plot = (string)json["overview"];
            try
            {
                Rating = (sbyte)(decimal.Parse((string)json["vote_average"]) * 10);
            }
            catch
            {
            }
            foreach (JToken child in ((JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{TMDB}/release_dates?api_key={TMDB_API}"))["results"].Children().ToList())
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
            Metadata.Add("id", TMDB);
            Metadata.Add("title", Title);
            Metadata.Add("plot", Plot);
            Metadata.Add("category", Category.ToString());
            if (Rating != 0)
            {
                Metadata.Add("rating", Rating);
            }

            Metadata.Add("release_date", ReleaseDate.ToString("MM-dd-yyyy"));
            Metadata.Add("scanned_date", ScannedDate.ToString("MM-dd-yyyy"));
            Metadata.Add("stills_generated", false);
            if (!string.IsNullOrWhiteSpace(MPAA))
            {
                Metadata.Add("mpaa", MPAA);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void Init(string initializer, bool isTMDB, DiscoveryCategory Category)
        {
            this.Category = Category;
            if (isTMDB)
            {
                TMDB = initializer;
            }
            else
            {
                PATH = initializer;
                Downloaded = !string.IsNullOrWhiteSpace(PATH) && File.Exists(PATH);
                if (Downloaded)
                {
                    MediaInfo = FFmpeg.GetMediaInfo(PATH).Result;
                    FullDuration = $"{MediaInfo.Duration.Hours}h {MediaInfo.Duration.Minutes}m";
                }

                Torrent torrent = new(FileName);
                string query = torrent.Name.Replace($".{torrent.Container}", "").Replace($"({torrent.Year})", "");
                string url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={query}&year={torrent.Year}";
                JArray results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
                if (results.Children().Any())
                {
                    TMDB = (string)results[0]["id"];
                }
                else
                {
                    url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={query}";
                    results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
                    if (results.Children().Any())
                    {
                        TMDB = (string)results[0]["id"];
                    }
                    else
                    {
                        url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={torrent.Title}&year={torrent.Year}";
                        results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
                        if (results.Children().Any())
                        {
                            TMDB = (string)results[0]["id"];
                        }
                        else
                        {
                            url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={torrent.Title}";
                            results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
                            if (results.Children().Any())
                            {
                                TMDB = (string)results[0]["id"];
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(TMDB))
                {
                    MovieLibraryModel.Instance.RemoveMedia(this);
                    return;
                }
            }
            if (Metadata == null)
            {
                Metadata_Directory = Path.Combine(Paths.MovieData, TMDB);
                try
                {
                    Metadata = new(Path.Combine(Metadata_Directory, "metadata"), false);
                }
                catch (Exception e)
                {
                    log.Fatal("Had Trouble setting metadata", e);
                    UpdateMetaData();
                    Cast = new("movie", TMDB);
                    return;
                }
            }
            Metadata.Add("coverLanguage", "");
            Metadata.Add("logo", "");
            Metadata.Add("poster", "");
            Metadata.Add("cover", "");
            LoadMetaData();
            if (Downloaded)
            {
                AlternativeVersions = Transcoder.GetAcceptableVersions(this);
            }
            Cast = new("movie", TMDB);
        }

        private void LoadMetaData()
        {
            if (Metadata.Size() <= 5 || Metadata.GetConfigByKey("title") == null)
            {
                log.Debug($"No cache saved for {TMDB}");
                UpdateMetaData();
            }
            else
            {
                log.Debug($"Loading metadata for {TMDB} from cache");
                try
                {
                    Title = Metadata.GetConfigByKey("title").Value;
                    Plot = Metadata.GetConfigByKey("plot").Value;
                    if (Metadata.GetConfigByKey("rating") != null)
                    {
                        Rating = Metadata.GetConfigByKey("rating").Value;
                    }

                    try
                    {
                        MPAA = Metadata.GetConfigByKey("mpaa") == null ? string.Empty : Metadata.GetConfigByKey("mpaa").Value;
                    }
                    catch { }
                    ReleaseDate = DateTime.Parse(Metadata.GetConfigByKey("release_date").Value);
                    ScannedDate = DateTime.Parse(Metadata.GetConfigByKey("scanned_date").Value);
                    Category = Enum.TryParse(typeof(DiscoveryCategory), Metadata.GetConfigByKey("category").Value, out object value) ? (DiscoveryCategory)value : DiscoveryCategory.None;
                }
                catch
                {
                    log.Error($"Unable to load {TMDB} from cache");
                    UpdateMetaData();
                }
            }
        }

        #endregion Private Methods
    }
}