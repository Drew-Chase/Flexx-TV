﻿using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects.Extras;
using System;
using System.IO;
using Xabe.FFmpeg;

namespace Flexx.Media.Objects
{
    public abstract class MediaBase
    {
        public string TMDB { get; protected set; }
        public string PATH { get; set; }
        public virtual string FileName => string.IsNullOrWhiteSpace(PATH) ? "" : new FileInfo(PATH).Name;
        public string Title { get; set; }
        public string Plot { get; set; }
        public string MPAA { get; set; }
        public sbyte Rating { get; set; }
        public virtual string PosterImage { get; set; }
        public virtual string CoverImage { get; set; }

        public DateTime ReleaseDate { get; set; }
        public DateTime ScannedDate { get; set; }
        public ConfigManager Metadata { get; set; }
        public virtual IMediaInfo MediaInfo { get; protected set; }
        public string FullDuration { get; protected set; }
        public virtual FileStream Stream => string.IsNullOrWhiteSpace(PATH) ? null : new(PATH, FileMode.Open, FileAccess.Read);
        public CastListModel Cast { get; set; }
        public bool Downloaded { get; protected set; }
        public MediaVersion[] AlternativeVersions { get; set; }

        protected MediaBase()
        {
        }

        public virtual void UpdateMetaData()
        {
        }

        public virtual bool ScanForDownloads(out string[] links)
        {
            links = Array.Empty<string>();
            return false;
        }

        public virtual void AddToTorrentClient(bool useInternal = true)
        {
        }
    }
}