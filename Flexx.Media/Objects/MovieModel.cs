using ChaseLabs.CLConfiguration.List;
using Flexx.Core.Networking;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
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
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects
{
    public class MovieModel : MediaBase
    {
        public string TrailerUrl { get; private set; }
        public bool HasTrailer { get; private set; }
        public DiscoveryCategory Category { get; private set; }

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
                try
                {
                    log.Debug($"Optimizing Poster for {TMDB}");
                    string path = Path.Combine(Metadata_Directory, "poster.jpg");
                    string tmp = Path.Combine(Paths.TempData, $"mp_{TMDB}.jpg");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    Transcoder.OptimizePoster(tmp, path);
                }
                catch
                {
                }
            }
        }

        public override string CoverImage
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
                try
                {
                    log.Debug($"Optimizing Cover Image for {TMDB}");
                    string path = Path.Combine(Metadata_Directory, "cover.jpg");
                    string tmp = Path.Combine(Paths.TempData, $"mc_{TMDB}.jpg");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    Transcoder.OptimizeCover(tmp, path);
                }
                catch { }
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
                    string tmp = Path.Combine(Paths.TempData, $"mcl_{TMDB}.jpg");
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

        private string Metadata_Directory;

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
#if DEBUG
            Metadata = new(Path.Combine(Path.Combine(Paths.MovieData, "Prefetch"), TMDB, "prefetch.metadata"), false, "FlexxTV");
#else
            Metadata = new(Path.Combine(Path.Combine(Paths.MovieData, "Prefetch"), TMDB, "prefetch.metadata"), true, "FlexxTV");
#endif
            Metadata_Directory = Path.Combine(Paths.MovieData, "Prefetch", TMDB);
            Init(TMDB, true, Category);
        }

        public MovieModel(string Initializer, bool IsTMDB = false) : base()
        {
            Init(Initializer, IsTMDB, DiscoveryCategory.None);
        }

        private void Init(string initializer, bool isTMDB, DiscoveryCategory Category)
        {
            this.Category = Category;
            if (isTMDB)
            {
                TMDB = initializer;
            }
            else
            {
                PATH = initializer.ToString();
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
                    TMDB = results[0]["id"].ToString();
                }
                else
                {
                    url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={query}";
                    results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
                    if (results.Children().Any())
                    {
                        TMDB = results[0]["id"].ToString();
                    }
                    else
                    {
                        url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={torrent.Title}&year={torrent.Year}";
                        results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
                        if (results.Children().Any())
                        {
                            TMDB = results[0]["id"].ToString();
                        }
                        else
                        {
                            url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={torrent.Title}";
                            results = (JArray)((JObject)Functions.GetJsonObjectFromURL(url))["results"];
                            if (results.Children().Any())
                            {
                                TMDB = results[0]["id"].ToString();
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
#if DEBUG
                    Metadata = new(Path.Combine(Metadata_Directory, "metadata"), false, "FlexxTV");
#else
                    Metadata = new(Path.Combine(meta_directory, "metadata"), true, "FlexxTV");
#endif
                }
                catch (Exception e)
                {
                    log.Fatal("Had Trouble setting metadata", e);
                    UpdateMetaData();
                    Cast = new("movie", TMDB);
                    return;
                }
            }
            LoadMetaData();
            Cast = new("movie", TMDB);
            if (Downloaded)
                AlternativeVersions = Transcoder.CreateVersion(this);
        }

        private void LoadMetaData()
        {
            if (Metadata.Size() <= 5 || Metadata.GetConfigByKey("title") == null)
            {
                log.Warn($"No cache saved for {TMDB}");
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
                }
                catch
                {
                    log.Error($"Unable to load {TMDB} from cache");
                    UpdateMetaData();
                }
            }
        }

        public override void UpdateMetaData()
        {
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

            PosterImage = $"https://image.tmdb.org/t/p/original{json["poster_path"]}";
            CoverImage = $"https://image.tmdb.org/t/p/original{json["backdrop_path"]}";

            if (DateTime.TryParse(json["release_date"].ToString(), out DateTime tmp))
            {
                ReleaseDate = tmp;
            }

            Title = json["title"].ToString();
            Plot = json["overview"].ToString();
            try
            {
                Rating = (sbyte)(decimal.Parse(json["vote_average"].ToString()) * 10);
            }
            catch
            {
            }
            foreach (JToken child in ((JObject)Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/movie/{TMDB}/release_dates?api_key={TMDB_API}"))["results"].Children().ToList())
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
            if (!string.IsNullOrWhiteSpace(MPAA))
            {
                Metadata.Add("mpaa", MPAA);
            }
        }

        public override bool ScanForDownloads(out string[] links)
        {
            links = Indexer.GetMagnetList($"{Title} {ReleaseDate.Year}");
            return links.Length != 0;
        }

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
                log.Error($"Unable to get reponse regarding {TMDB} trailer", e);
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
                string key = keyObject.ToString();
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
    }
}