using ChaseLabs.CLLogger;
using ChaseLabs.CLLogger.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flexx.Core.Data
{
    public static class Global
    {
        public static string TMDB_API => "378ae44c6e7f5dde094cd8c8456378e0";
#if DEBUG
        public static ILog log => LogManager.Init().SetLogDirectory(Path.Combine(Paths.Log, "latest.log")).SetDumpMethod(DumpType.NoDump).SetMinimumLogType(Lists.LogTypes.All);
#else
        public static ILog log => LogManager.Init().SetLogDirectory(Path.Combine(Paths.Log, "latest.log")).SetDumpMethod(3000).SetMinimumLogType(Lists.LogTypes.Info);
#endif
        public static Configuration config = new();
        public static string[] Media_Extensions = new string[] { "mpegg", "mpeg", "mp4", "mkv", "m4a", "m4v", "f4v", "f4a", "m4b", "m4r", "f4b", "mov", "3gp", "3gp2", "3g2", "3gpp", "3gpp2", "ogg", "oga", "ogv", "ogx", "wmv", "wma", "flv", "avi" };

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
            public static string TranscodedData(string username) => Directory.CreateDirectory(Path.Combine(TempData, "transcoded", username)).FullName;
            public static string MetaData => Directory.CreateDirectory(Path.Combine(Resources, "Metadata")).FullName;
            public static string MovieData => Directory.CreateDirectory(Path.Combine(MetaData, "Movies")).FullName;
            public static string TVData => Directory.CreateDirectory(Path.Combine(MetaData, "TV")).FullName;

            public static string MissingPoster
            {
                get
                {
                    string path = Path.Combine(MetaData, "missing.jpg");
                    if (!File.Exists(path))
                    {
                        new System.Net.WebClient().DownloadFile($"https://flexx-tv.tk/assets/images/MissingArtwork.jpg", path);
                    }
                    return path;
                }
            }
        }

        public static class Functions
        {
            public static IEnumerable<T[]> SplitArray<T>(T[] fullArray, int size)
            {
                for (int i = 0; i < fullArray.Length; i += size)
                {
                    T[] range;
                    try
                    {
                        range = fullArray.ToList().GetRange(i, Math.Min(size, fullArray.Length - i)).ToArray();
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        continue;
                    }
                    yield return range;
                }
            }
        }
    }
}