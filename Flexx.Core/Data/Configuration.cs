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
            sysProfile = new(Path.Combine(Paths.Configs, "sys"), false);
            sysProfile.Add("movies", "");
            sysProfile.Add("tv", "");
        }
    }
}
