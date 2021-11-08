using ChaseLabs.CLConfiguration.List;
using System.IO;
using static Flexx.Core.Data.Global;

namespace Flexx.Core.Data
{
    public class Configuration
    {
        public string MovieLibraryPath { get => sysProfile.GetConfigByKey("movies").Value; set => sysProfile.GetConfigByKey("movies").Value = value; }
        public string TVLibraryPath { get => sysProfile.GetConfigByKey("tv").Value; set => sysProfile.GetConfigByKey("tv").Value = value; }
        public string NextScheduledPrefetch { get => sysProfile.GetConfigByKey("next_scheduled_prefetch").Value; set => sysProfile.GetConfigByKey("next_scheduled_prefetch").Value = value; }
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
            sysProfile.Add("next_scheduled_prefetch", "");
        }
    }
}