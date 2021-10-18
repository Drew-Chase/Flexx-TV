﻿using ChaseLabs.CLConfiguration.List;
using System.IO;
using static Flexx.Core.Data.Global;

namespace Flexx.Core.Data
{
    public class Configuration
    {
        public string MovieLibraryPath => sysProfile.GetConfigByKey("movies").Value;
        public string TVLibraryPath => sysProfile.GetConfigByKey("tv").Value;
        private readonly ConfigManager sysProfile;

        public Configuration()
        {
#if DEBUG
            sysProfile = new(Path.Combine(Paths.Configs, "sys"), false, "FlexxTV");
#else
            sysProfile = new(Path.Combine(Paths.Configs, "sys"), true, "FlexxTV");
#endif
            sysProfile.Add("movies", "");
            sysProfile.Add("tv", "");
        }
    }
}