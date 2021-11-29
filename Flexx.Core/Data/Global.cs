using ChaseLabs.CLLogger;
using ChaseLabs.CLLogger.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;

namespace Flexx.Core.Data;

public static class Global
{
    public static string TMDB_API => "378ae44c6e7f5dde094cd8c8456378e0";
#if DEBUG
    public static ILog log => LogManager.Singleton.SetLogDirectory(Path.Combine(Paths.Log, "latest.log")).SetDumpMethod(DumpType.NoDump).SetMinimumLogType(Lists.LogTypes.All);
#else
        public static ILog log => LogManager.Init().SetLogDirectory(Path.Combine(Paths.Log, "latest.log")).SetDumpMethod(3000).SetMinimumLogType(Lists.LogTypes.Info);
#endif
    public static Configuration config = new();
    public static string[] Media_Extensions => new string[] { "mpegg", "mpeg", "mp4", "mkv", "m4a", "m4v", "f4v", "f4a", "m4b", "m4r", "f4b", "mov", "3gp", "3gp2", "3g2", "3gpp", "3gpp2", "ogg", "oga", "ogv", "ogx", "wmv", "wma", "flv", "avi" };

    public enum DiscoveryCategory
    {
        None,
        Latest,
        Popular,
        Top_Rated,
        Upcoming
    }

    public static class Paths
    {
        public static string Root => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LFInteractive", "Flexx")).FullName;
        public static string Resources => Directory.CreateDirectory(Path.Combine(Root, "Resources")).FullName;
        public static string Log => Directory.CreateDirectory(Path.Combine(Resources, "Logs")).FullName;
        public static string Configs => Directory.CreateDirectory(Path.Combine(Resources, "Config")).FullName;
        public static string UserData => Directory.CreateDirectory(Path.Combine(Resources, "UserData")).FullName;
        public static string FFMpeg => Directory.CreateDirectory(Path.Combine(Resources, "FFMpeg")).FullName;
        public static string TempData => Directory.CreateDirectory(Path.Combine(Resources, ".tmp")).FullName;

        public static string TranscodedData(string username)
        {
            return Directory.CreateDirectory(Path.Combine(TempData, "transcoded", username)).FullName;
        }

        public static string MetaData => Directory.CreateDirectory(Path.Combine(Resources, "Metadata")).FullName;
        public static string MovieData => Directory.CreateDirectory(Path.Combine(MetaData, "Movies")).FullName;
        public static string TVData => Directory.CreateDirectory(Path.Combine(MetaData, "TV")).FullName;

        public static string MissingPoster
        {
            get
            {
                string path = Path.Combine(MetaData, "missing_poster.jpg");
                if (!File.Exists(path))
                {
                    HttpClient client = new();
                    using HttpResponseMessage response = client.GetAsync($"https://flexx-tv.tk/assets/images/missing_poster.jpg").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        using FileStream fs = new(Path.Combine(TempData, "poster.jpg"), FileMode.CreateNew, FileAccess.ReadWrite);
                        response.Content.CopyToAsync(fs).Wait();
                    }
                }
                return path;
            }
        }

        public static string MissingCover
        {
            get
            {
                string path = Path.Combine(MetaData, "missing_cover.jpg");
                if (!File.Exists(path))
                {
                    HttpClient client = new();
                    using HttpResponseMessage response = client.GetAsync($"https://flexx-tv.tk/assets/images/missing_cover.jpg").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        using FileStream fs = new(path, FileMode.CreateNew, FileAccess.ReadWrite);
                        response.Content.CopyToAsync(fs).Wait();
                    }
                }
                return path;
            }
        }

        public static string GetVersionPath(string metadata_directory, string title, int resolution, int bitrate) => Path.Combine(Directory.CreateDirectory(Path.Combine(metadata_directory, "versions")).FullName, $"{title}-{resolution}-{bitrate}K.mp4");
    }

    public static class Functions
    {
        public static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        public static object GetJsonObjectFromURL(string url)
        {
            string json = "";
            HttpClient client = new();
            client.Timeout = new(0, 0, 20);
            using (HttpResponseMessage response = client.GetAsync(url).Result)
            {
                try
                {
                    json = response.Content.ReadAsStringAsync().Result;
                }
                catch (Exception e)
                {
                    log.Error($"Unable to fetch Json from url \"{url}\"", e);
                    return new { };
                }
            }
            if (string.IsNullOrWhiteSpace(json))
            {
                log.Error($"Unable to fetch Json from url \"{url}\"");
                return new { };
            }
            return JsonConvert.DeserializeObject(json);
        }
    }
}