using ChaseLabs.CLConfiguration.List;
using Flexx.Authentication;
using System;
using System.IO;
using static Flexx.Data.Global;

namespace Flexx.Data
{
    /// <summary>
    /// Stores all system configuration settings
    /// </summary>
    public class Configuration
    {
        #region Private Fields

        private readonly ConfigManager sysProfile;

        #endregion Private Fields

        #region Public Constructors

        public Configuration()
        {
            sysProfile = new(Path.Combine(Paths.Configs, "sys"));
            sysProfile.Add("movies", "");
            sysProfile.Add("port", 3208);
            sysProfile.Add("tv", "");
            sysProfile.Add("next_scheduled_prefetch", DateTime.Now.ToString("MM-dd-yyyy"));
            sysProfile.Add("use_version_file", false);
            sysProfile.Add("language", "en");
            sysProfile.Add("setup", false);
            sysProfile.Add("portforward", false);
            sysProfile.Add("token", "");

            sysProfile.Add("version", "0.0.0");
            sysProfile.Add("channel", "release");

            sysProfile.Add("sonarr_base", "");
            sysProfile.Add("sonarr_key", "");
            sysProfile.Add("sonarr_port", -1);

            sysProfile.Add("radarr_base", "");
            sysProfile.Add("radarr_key", "");
            sysProfile.Add("radarr_port", -1);
            sysProfile.Add("sonarr_connected", false);
            sysProfile.Add("radarr_connected", false);
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// The REST and WebServers port
        /// </summary>
        public int ApiPort { get => sysProfile.GetConfigByKey("port").Value; set => sysProfile.GetConfigByKey("port").Value = value; }

        /// <summary>
        /// The current version of the application. Used for updating <br/><example> <i>
        /// Release.Major.Minor -&gt; <b> <c> 01.05.03 </c></b></i></example>
        /// </summary>
        public string ApplicationVersion { get => sysProfile.GetConfigByKey("version").Value; set => sysProfile.GetConfigByKey("version").Value = value; }

        /// <summary>
        /// The language that all media will be stored in.
        /// </summary>
        public string LanguagePreference { get => sysProfile.GetConfigByKey("language").Value; set => sysProfile.GetConfigByKey("language").Value = value; }

        /// <summary>
        /// The root directory where all movies are stored
        /// </summary>
        public string MovieLibraryPath { get => sysProfile.GetConfigByKey("movies").Value.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar); set => sysProfile.GetConfigByKey("movies").Value = value.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar); }

        /// <summary>
        /// When the system should refresh the <see cref="Utilities.Scanner.Prefetch"> Prefetched
        /// Data </see>.
        /// </summary>
        public string NextScheduledPrefetch { get => sysProfile.GetConfigByKey("next_scheduled_prefetch").Value; set => sysProfile.GetConfigByKey("next_scheduled_prefetch").Value = value; }

        /// <summary>
        /// Rather the program should auto Port Forward.
        /// </summary>
        public bool PortForward { get => sysProfile.GetConfigByKey("portforward").Value; set => sysProfile.GetConfigByKey("portforward").Value = value; }

        /// <summary>
        /// The saved Radarr api key
        /// </summary>
        public string RadarrAPIKey { get => sysProfile.GetConfigByKey("radarr_key").Value; set => sysProfile.GetConfigByKey("radarr_key").Value = value; }

        /// <summary>
        /// The saved Radarr base url
        /// </summary>
        public string RadarrBaseUrl { get => sysProfile.GetConfigByKey("radarr_base").Value; set => sysProfile.GetConfigByKey("radarr_base").Value = value; }

        /// <summary>
        /// Rather radarr was successfully connected to.
        /// </summary>
        public bool RadarrConnected { get => sysProfile.GetConfigByKey("radarr_connected").Value; set => sysProfile.GetConfigByKey("radarr_connected").Value = value; }

        /// <summary>
        /// The saved Radarr port
        /// </summary>
        public int RadarrPort { get => sysProfile.GetConfigByKey("radarr_port").Value; set => sysProfile.GetConfigByKey("radarr_port").Value = value; }

        /// <summary>
        /// </summary>
        public bool Setup => !string.IsNullOrWhiteSpace(MovieLibraryPath) && !string.IsNullOrWhiteSpace(TVLibraryPath) && Directory.Exists(TVLibraryPath) && Directory.Exists(MovieLibraryPath) && Users.Instance.HasHostUser;

        public string SonarrAPIKey { get => sysProfile.GetConfigByKey("sonarr_key").Value; set => sysProfile.GetConfigByKey("sonarr_key").Value = value; }

        public string SonarrBaseUrl { get => sysProfile.GetConfigByKey("sonarr_base").Value; set => sysProfile.GetConfigByKey("sonarr_base").Value = value; }

        public bool SonarrConnected { get => sysProfile.GetConfigByKey("sonarr_connected").Value; set => sysProfile.GetConfigByKey("sonarr_connected").Value = value; }

        public int SonarrPort { get => sysProfile.GetConfigByKey("sonarr_port").Value; set => sysProfile.GetConfigByKey("sonarr_port").Value = value; }

        public string Token { get => sysProfile.GetConfigByKey("token").Value; set => sysProfile.GetConfigByKey("token").Value = value; }

        public string TVLibraryPath { get => sysProfile.GetConfigByKey("tv").Value.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar); set => sysProfile.GetConfigByKey("tv").Value = value.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar); }

        public bool UseVersionFile { get => sysProfile.GetConfigByKey("use_version_file").Value; set => sysProfile.GetConfigByKey("use_version_file").Value = value; }

        #endregion Public Properties
    }
}