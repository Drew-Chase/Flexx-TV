using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects.Extras;
using System;
using System.IO;
using Xabe.FFmpeg;

namespace Flexx.Media.Interfaces
{
    public interface IMedia
    {
        string PATH { get; set; }
        virtual string FileName => new FileInfo(PATH).Name;
        string Title { get; set; }
        string Plot { get; set; }
        string MPAA { get; set; }
        sbyte Rating { get; set; }
        string PosterImage { get; set; }
        string CoverImage { get; set; }

        virtual bool Watched
        {
            get
            {
                if (Metadata.GetConfigByKey("watched") == null)
                {
                    Metadata.Add("watched", false);
                }

                return Metadata.GetConfigByKey("watched").ParseBoolean();
            }
            set
            {
                if (Metadata.GetConfigByKey("watched") == null)
                {
                    Metadata.Add("watched", false);
                }

                Metadata.GetConfigByKey("watched").Value = value.ToString();
            }
        }

        virtual uint WatchedDuration
        {
            get
            {
                if (Metadata.GetConfigByKey("watched_duration") == null)
                {
                    Metadata.Add("watched_duration", 0);
                }

                return uint.Parse(Metadata.GetConfigByKey("watched_duration").Value);
            }
            set
            {
                if (Metadata.GetConfigByKey("watched_duration") == null)
                {
                    Metadata.Add("watched_duration", 0);
                }

                Metadata.GetConfigByKey("watched_duration").Value = value.ToString();
            }
        }

        DateTime ReleaseDate { get; set; }
        DateTime ScannedDate { get; set; }
        ConfigManager Metadata { get; set; }
        virtual IMediaInfo MediaInfo => FFmpeg.GetMediaInfo(PATH).Result;
        virtual FileStream Stream => new(PATH, FileMode.Open, FileAccess.Read);
        CastListModel Cast { get; set; }

        void UpdateMetaData();

        bool ScanForDownloads(out string[] links);

        void AddToTorrentClient(bool useInternal = true);
    }
}