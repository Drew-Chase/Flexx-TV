using System;
using System.IO;
using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
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
        virtual bool Watched { get => Metadata.GetConfigByKey("watched").ParseBoolean(); set => Metadata.GetConfigByKey("watched").Value = value.ToString(); }
        virtual uint WatchedDuration { get => uint.Parse(Metadata.GetConfigByKey("watched_duration").Value); set => Metadata.GetConfigByKey("watched_duration").Value = value.ToString(); }
        DateTime ReleaseDate { get; set; }
        ConfigManager Metadata { get; set; }
        virtual IMediaInfo MediaInfo => FFmpeg.GetMediaInfo(PATH).Result;
        virtual FileStream Stream => new(PATH, FileMode.Open, FileAccess.Read);
        CastListModel Cast { get; set; }
        void UpdateMetaData();
    }
}