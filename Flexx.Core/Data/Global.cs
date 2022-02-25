using ChaseLabs.CLLogger;
using ChaseLabs.CLLogger.Interfaces;
using Flexx.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace Flexx.Data;

/// <summary>
/// All Globally available fields and functions
/// </summary>
public static class Global
{
    #region Public Fields

    public static Configuration config = new();

    #endregion Public Fields

    #region Public Enums

    /// <summary>
    /// A list of all categories that the media prefetcher will grab from.
    /// </summary>
    public enum DiscoveryCategory
    {
        None,

        latest,

        popular,

        top_rated,
    }

    /// <summary>
    /// The update channel that will be polled for most recent update.
    /// </summary>
    public enum UpdateChannels
    {
        Development,

        Alpha,

        Beta,

        Release,
    }

    #endregion Public Enums

    #region Public Properties

    /// <summary>
    /// Primary logger
    /// </summary>
    public static ILog log => LogManager.Singleton.SetLogDirectory(Path.Combine(Paths.Log, "latest.log")).SetDumpMethod(3000).SetMinimumLogType(Lists.LogTypes.All);

    /// <summary>
    /// List of all accepted media container extensions.
    /// </summary>
    public static string[] Media_Extensions => new string[] { "mpegg", "mpeg", "mp4", "mkv", "m4a", "m4v", "f4v", "f4a", "m4b", "m4r", "f4b", "mov", "3gp", "3gp2", "3g2", "3gpp", "3gpp2", "ogg", "oga", "ogv", "ogx", "wmv", "wma", "flv", "avi" };

    /// <summary>
    /// The Movie Database API Key. <br/><i> PS: This should be stored somewhere else... <b> But who
    /// has the time for that?!?!?! </b></i>
    /// </summary>
    public static string TMDB_API => "378ae44c6e7f5dde094cd8c8456378e0";

    #endregion Public Properties

    #region Public Classes

    public static class Functions
    {
        #region Public Methods

        /// <summary>
        /// Exits the application and disposes of necessary elements
        /// </summary>
        public static void ExitApplication()
        {
            Process.GetCurrentProcess().Kill(true);  // Do Last
        }

        /// <summary>
        /// Checks if file is locked by another process
        /// </summary>
        /// <param name="file"> </param>
        /// <returns> </returns>
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

        /// <summary>
        /// Closes the Application and restarts it with optional arguments
        /// </summary>
        /// <param name="args"> Application Arguments </param>
        public static void RestartApplication(string args = "")
        {
            Process process = new()
            {
                StartInfo = new()
                {
                    Arguments = args,
                    FileName = Paths.ExecutingBinary,
                    UseShellExecute = true,
                },
                EnableRaisingEvents = true,
            };
            process.Start();
            ExitApplication();
        }

        /// <summary>
        /// Attempts to Get a Json JToken object from url
        /// </summary>
        /// <typeparam name="T"> Json Type </typeparam>
        /// <param name="url">  The url with a json return </param>
        /// <param name="json"> the output json object </param>
        /// <returns> </returns>
        public static bool TryGetJsonObjectFromURL<T>(string url, out T json) where T : JToken
        {
            json = null;
            try
            {
                string jstring = "";
                HttpClient client = new();
                client.Timeout = new(0, 0, 20);
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            jstring = response.Content.ReadAsStringAsync().Result;
                        }
                        catch (Exception e)
                        {
                            log.Error($"Unable to fetch JSON from URL \"{url}\"", e);
                            return false;
                        }
                    }
                    else
                        return false;
                }
                JObject jsonObject = (JObject) JsonConvert.DeserializeObject(jstring);
                if (string.IsNullOrWhiteSpace(jstring) || jsonObject["success"] != null)
                {
                    log.Error($"Unable to fetch JSON from URL \"{url}\"");
                    return false;
                }
                json = JsonConvert.DeserializeObject<T>(jstring);
                return true;
            }
            catch (Exception e)
            {
                log.Error($"Unable to create Json Object from URL", e);
                return false;
            }
        }

        #endregion Public Methods
    }

    public static class Paths
    {
        #region Public Properties

        /// <summary>
        /// Directory where all configuration files are stored
        /// </summary>
        public static string Configs => Directory.CreateDirectory(Path.Combine(Resources, "Config")).FullName;

        /// <summary>
        /// Returns absolute path to the Applications Executable <br/>
        /// Ex: C:\path\to\fml_console.exe - on Windows
        /// </summary>
        public static string ExecutingBinary => Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName, $"{AppDomain.CurrentDomain.FriendlyName}{(OperatingSystem.IsWindows() ? ".exe" : "")}");

        /// <summary>
        /// Directory where ffmpeg will be stored
        /// </summary>
        public static string FFMpeg => Directory.CreateDirectory(Path.Combine(Resources, "FFMpeg")).FullName;

        /// <summary>
        /// Directory where all log files are stored
        /// </summary>
        public static string Log => Directory.CreateDirectory(Path.Combine(Resources, "Logs")).FullName;

        /// <summary>
        /// Directory where the root of Movie and TV metadata will be stored
        /// </summary>
        public static string MetaData => Directory.CreateDirectory(Path.Combine(Resources, "Metadata")).FullName;

        /// <summary>
        /// Returns the absolute path to the missing cover image. <br/> If none is found it will
        /// download from flexx server.
        /// </summary>
        public static string MissingCover
        {
            get
            {
                string path = Path.Combine(MetaData, "missing_cover.jpg");
                if (!File.Exists(path))
                {
                    log.Debug($"Downloading Missing Cover Artwork");
                    HttpClient client = new();
                    using HttpResponseMessage response = client.GetAsync($"https://flexx-tv.tk/assets/images/missing_cover.jpg").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string temp = Path.Combine(TempData, "cover.jpg");
                        if (File.Exists(temp))
                            File.Delete(temp);
                        using FileStream fs = new(temp, FileMode.CreateNew, FileAccess.ReadWrite);
                        response.Content.CopyToAsync(fs).Wait();
                        log.Debug($"Optimizing Missing Cover Artwork");
                        Transcoder.OptimizeCover(temp, path);
                        log.Debug($"Done Processing Missing Cover Artwork");
                    }
                }
                return path;
            }
        }

#if DEBUG

        public static string FlexxBaseURL => "flexx-tv.com";

        public static string FlexxAuthURL => $"http://auth.{FlexxBaseURL}";

#else
        public static string FlexxBaseURL => "flexx-tv.tk";
        public static string FlexxAuthURL => $"https://auth.{FlexxBaseURL}";
#endif

        /// <summary>
        /// Returns the absolute path to the missing poster image. <br/> If none is found it will
        /// download from flexx server.
        /// </summary>
        public static string MissingPoster
        {
            get
            {
                string path = Path.Combine(MetaData, "missing_poster.jpg");
                if (!File.Exists(path))
                {
                    log.Debug($"Downloading Missing Poster Artwork");
                    HttpClient client = new();
                    using HttpResponseMessage response = client.GetAsync($"https://flexx-tv.tk/assets/images/missing_poster.jpg").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string temp = Path.Combine(TempData, "poster.jpg");
                        if (File.Exists(temp))
                            File.Delete(temp);
                        using FileStream fs = new(temp, FileMode.CreateNew, FileAccess.ReadWrite);
                        response.Content.CopyToAsync(fs).Wait();
                        log.Debug($"Optimizing Missing Poster Artwork");
                        Transcoder.OptimizePoster(temp, path);
                        log.Debug($"Done Processing Missing Poster Artwork");
                    }
                }
                return path;
            }
        }

        /// <summary>
        /// Directory that will store all movie metadata
        /// </summary>
        public static string MovieData => Directory.CreateDirectory(Path.Combine(MetaData, "Movies")).FullName;

        /// <summary>
        /// The primary directory in which all metadata and configuration files are stored
        /// </summary>
        public static string Resources => Directory.CreateDirectory(Path.Combine(Root, "Resources")).FullName;

        public static string Root => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LFInteractive", "Flexx")).FullName;

        /// <summary>
        /// Where all temporary data is stored.
        /// </summary>
        public static string TempData => Directory.CreateDirectory(Path.Combine(Resources, ".tmp")).FullName;

        /// <summary>
        /// Where all TV Show metadata is stored
        /// </summary>
        public static string TVData => Directory.CreateDirectory(Path.Combine(MetaData, "TV")).FullName;

        /// <summary>
        /// Where all users are saved and loaded from
        /// </summary>
        public static string UserData => Directory.CreateDirectory(Path.Combine(Resources, "UserData")).FullName;

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// If versioning is used instead of on the fly transcoding, then this will be used.
        /// </summary>
        /// <param name="metadata_directory"> </param>
        /// <param name="title">              </param>
        /// <param name="resolution">         </param>
        /// <param name="bitrate">            </param>
        /// <returns> </returns>
        public static string GetVersionPath(string metadata_directory, string title, int resolution, int bitrate) => Path.Combine(Directory.CreateDirectory(Path.Combine(metadata_directory, "versions")).FullName, $"{title}-{resolution}-{bitrate}K.mp4");

        #endregion Public Methods
    }

    #endregion Public Classes
}