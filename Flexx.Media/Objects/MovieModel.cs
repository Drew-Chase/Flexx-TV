using Flexx.Core.Networking;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using TorrentTitleParser;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects
{
    public class MovieModel : MediaBase
    {
        public string TrailerUrl { get; private set; }
        public string TMDB { get; private set; }

        public override string PosterImage
        {
            get
            {
                string path = Path.Combine(meta_directory, "poster.jpg");
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
                    string path = Path.Combine(meta_directory, "poster.jpg");
                    string tmp = Path.Combine(Paths.TempData, $"mp_{TMDB}.jpg");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    FFMpegUtil.OptimizePoster(tmp, path);
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
                string path = Path.Combine(meta_directory, "cover.jpg");
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
                    string path = Path.Combine(meta_directory, "cover.jpg");
                    string tmp = Path.Combine(Paths.TempData, $"mc_{TMDB}.jpg");
                    new System.Net.WebClient().DownloadFile(value, tmp);
                    FFMpegUtil.OptimizeCover(tmp, path);
                }
                catch { }
            }
        }

        private string meta_directory => Path.Combine(Paths.MetaData, "Movies", TMDB);

        public MovieModel(string initializer, bool isTMDB = false)
        {
            if (isTMDB)
            {
                TMDB = initializer;
            }
            else
            {
                PATH = initializer.ToString();
                Torrent torrent = new(FileName);
                string query = torrent.Name.Replace($".{torrent.Container}", "").Replace($"({torrent.Year})", "");
                string url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={query}&year={torrent.Year}";
                JArray results = (JArray)((JObject)JsonConvert.DeserializeObject(new System.Net.WebClient().DownloadString(url)))["results"];
                if (results.Children().Any())
                {
                    TMDB = results[0]["id"].ToString();
                }
                else
                {
                    url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={query}";
                    results = (JArray)((JObject)JsonConvert.DeserializeObject(new System.Net.WebClient().DownloadString(url)))["results"];
                    if (results.Children().Any())
                    {
                        TMDB = results[0]["id"].ToString();
                    }
                    else
                    {
                        url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={torrent.Title}&year={torrent.Year}";
                        results = (JArray)((JObject)JsonConvert.DeserializeObject(new System.Net.WebClient().DownloadString(url)))["results"];
                        if (results.Children().Any())
                        {
                            TMDB = results[0]["id"].ToString();
                        }
                        else
                        {
                            url = $"https://api.themoviedb.org/3/search/movie?api_key={TMDB_API}&query={torrent.Title}";
                            results = (JArray)((JObject)JsonConvert.DeserializeObject(new System.Net.WebClient().DownloadString(url)))["results"];
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
#if DEBUG
            Metadata = new(Path.Combine(Paths.MetaData, "Movies", TMDB, "metadata"), false, "FlexxTV");
#else
            Metadata = new(Path.Combine(Paths.MetaData, "Movies", TMDB, "metadata"), true, "FlexxTV");
#endif

            Metadata.Add("watched", false);
            Metadata.Add("watched_duration", (uint)0);
            LoadMetaData();
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
                    Rating = Metadata.GetConfigByKey("rating").Value;
                    try
                    {
                        TrailerUrl = Metadata.GetConfigByKey("trailer") == null ? string.Empty : Metadata.GetConfigByKey("trailer").Value;
                    }
                    catch { }
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
            Cast = new(this);
            ScannedDate = DateTime.Now;
            using (System.Net.WebClient client = new())
            {
                try
                {
                    IVideoStreamInfo streamInfo = new YoutubeClient().Videos.Streams.GetManifestAsync(((JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/movie/{TMDB}/videos?api_key={TMDB_API}")))["results"][0]["key"].ToString()).Result.GetMuxedStreams().GetWithHighestVideoQuality();
                    if (streamInfo != null)
                    {
                        TrailerUrl = streamInfo.Url;
                    }
                }
                catch
                {
                    TrailerUrl = "";
                }
                JObject json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/movie/{TMDB}?api_key={TMDB_API}"));

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
                foreach (JToken child in ((JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/movie/{TMDB}/release_dates?api_key={TMDB_API}")))["results"].Children().ToList())
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
            Metadata.Add("title", Title);
            Metadata.Add("plot", Plot);
            if (Rating != 0)
            {
                Metadata.Add("rating", Rating);
            }

            if (!string.IsNullOrWhiteSpace(TrailerUrl))
            {
                Metadata.Add("trailer", TrailerUrl);
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
    }
}