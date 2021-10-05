using System;
using ChaseLabs.CLConfiguration.List;
using static Flexx.Core.Data.Global;
using System.IO;

namespace Flexx.Core.Data
{
    public class Configuration
    {
        public string MovieLibraryPath { get => sysProfile.GetConfigByKey("movies").Value; set => sysProfile.GetConfigByKey("movies").Value = value; }
        public string TVLibraryPath { get => sysProfile.GetConfigByKey("tv").Value; set => sysProfile.GetConfigByKey("tv").Value = value; }
        private ConfigManager sysProfile, userProfile;

        public Configuration()
        {
#if DEBUG
            sysProfile = new(Path.Combine(Paths.Configs, "sys"), false);
#else
             sysProfile = new(Path.Combine(Paths.Configs, "sys"), true);
#endif
            sysProfile.Add("movies", "");
            sysProfile.Add("tv", "");
        }
    }
}
