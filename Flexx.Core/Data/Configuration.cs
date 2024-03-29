﻿using ChaseLabs.CLConfiguration.List;
using System.IO;
using static Flexx.Data.Global;

namespace Flexx.Data
{
    public class Configuration
    {
        #region Private Fields

        private readonly ConfigManager sysProfile;

        #endregion Private Fields

        #region Public Constructors

        public Configuration()
        {
            sysProfile = new(Path.Combine(Paths.Configs, "sys"), true);
            sysProfile.Add("movies", "");
            sysProfile.Add("port", 3208);
            sysProfile.Add("tv", "");
            sysProfile.Add("next_scheduled_prefetch", "");
            sysProfile.Add("use_version_file", false);
            sysProfile.Add("language", "en");
            sysProfile.Add("setup", false);
            sysProfile.Add("portforward", false);
            sysProfile.Add("token", "");
        }

        #endregion Public Constructors

        #region Public Properties

        public int ApiPort { get => sysProfile.GetConfigByKey("port").Value; set => sysProfile.GetConfigByKey("port").Value = value; }

        public string LanguagePreference { get => sysProfile.GetConfigByKey("language").Value; set => sysProfile.GetConfigByKey("language").Value = value; }

        public string MovieLibraryPath { get => sysProfile.GetConfigByKey("movies").Value; set => sysProfile.GetConfigByKey("movies").Value = value; }

        public string NextScheduledPrefetch { get => sysProfile.GetConfigByKey("next_scheduled_prefetch").Value; set => sysProfile.GetConfigByKey("next_scheduled_prefetch").Value = value; }

        public bool PortForward { get => sysProfile.GetConfigByKey("portforward").Value; set => sysProfile.GetConfigByKey("portforward").Value = value; }

        public bool Setup
        {
            get => sysProfile.GetConfigByKey("setup").Value;
            set => sysProfile.GetConfigByKey("setup").Value = value;
        }

        public string Token { get => sysProfile.GetConfigByKey("token").Value; set => sysProfile.GetConfigByKey("token").Value = value; }

        public string TVLibraryPath { get => sysProfile.GetConfigByKey("tv").Value; set => sysProfile.GetConfigByKey("tv").Value = value; }

        public bool UseVersionFile { get => sysProfile.GetConfigByKey("use_version_file").Value; set => sysProfile.GetConfigByKey("use_version_file").Value = value; }

        #endregion Public Properties
    }
}