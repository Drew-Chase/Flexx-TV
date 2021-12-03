using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects.Extras;
using System;
using System.IO;
using Xabe.FFmpeg;

namespace Flexx.Media.Objects
{
    public abstract class MediaBase
    {
        #region Protected Constructors

        protected MediaBase()
        {
        }

        #endregion Protected Constructors

        #region Public Properties

        public MediaVersion[] AlternativeVersions { get; set; }

        public CastListModel Cast { get; set; }

        public virtual string CoverImage { get; set; }

        public bool Downloaded { get; protected set; }

        public virtual string FileName => string.IsNullOrWhiteSpace(PATH) ? "" : new FileInfo(PATH).Name;

        public string FullDuration { get; protected set; }

        public virtual IMediaInfo MediaInfo { get; protected set; }

        public ConfigManager Metadata { get; set; }

        public string MPAA { get; set; }

        public string PATH { get; set; }

        public string Plot { get; set; }

        public virtual string PosterImage { get; set; }

        public sbyte Rating { get; set; }

        public DateTime ReleaseDate { get; set; }

        public DateTime ScannedDate { get; set; }

        public virtual FileStream Stream => string.IsNullOrWhiteSpace(PATH) ? null : new(PATH, FileMode.Open, FileAccess.Read);

        public string Title { get; set; }

        public string TMDB { get; protected set; }

        #endregion Public Properties

        #region Public Methods

        public virtual void AddToTorrentClient(bool useInternal = true)
        {
        }

        public virtual bool ScanForDownloads(out string[] links)
        {
            links = Array.Empty<string>();
            return false;
        }

        public virtual void UpdateMetaData()
        {
        }

        #endregion Public Methods
    }
}