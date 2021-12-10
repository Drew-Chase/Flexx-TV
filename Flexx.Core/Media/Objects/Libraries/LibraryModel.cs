using Flexx.Media.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Libraries
{
    public class LibraryModel
    {
        #region Protected Fields

        protected List<MediaBase> medias;

        #endregion Protected Fields

        #region Protected Constructors

        protected LibraryModel()
        {
            medias = new();
        }

        #endregion Protected Constructors

        #region Public Methods

        public virtual void AddMedia(params MediaBase[] medias)
        {
            foreach (MediaBase media in medias)
            {
                if (media != null && !string.IsNullOrWhiteSpace(media.Title))
                {
                    if (GetMediaByName(media.Title) == null && !string.IsNullOrWhiteSpace(media.TMDB))
                    {
                        log.Debug($"Adding \"{media.Title}\"");
                        this.medias.Add(media);
                    }
                    else
                    {
                        log.Debug($"Not Adding Redundant Title \"{media.Title}\"");
                    }
                }
            }
        }

        public virtual MediaBase GetMediaByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }
                foreach (MediaBase media in medias.ToArray())
                {
                    if (!string.IsNullOrWhiteSpace(media.Title) && media.Title.ToLower().Equals(name.ToLower()))
                        return media;
                }
            }
            catch (Exception e)
            {
                log.Error($"Had trouble trying to Get Media object by the provided Name \"{name}\"", e);
            }
            return null;
        }

        public virtual MediaBase[] GetMediaByYear(ushort year)
        {
            List<MediaBase> list = new();
            foreach (MediaBase media in medias)
            {
                if (media.ReleaseDate.Year.Equals(year))
                {
                    list.Add(media);
                }
            }
            return list.ToArray();
        }

        public MediaBase[] GetMediaItems()
        {
            return medias.ToArray();
        }

        public virtual void Initialize()
        {
        }

        public virtual void PostInitializationEvent()
        {
            if (config.UseVersionFile)
            {
                Task.Run(() =>
                {
                    foreach (var item in medias)
                    {
                        item.AlternativeVersions = Transcoder.CreateVersion(item);
                    }
                }).Wait();
            }
        }

        public virtual void RefreshMetadata()
        {
            foreach (MediaBase media in medias)
            {
                media.UpdateMetaData();
            }
        }

        public virtual Task RefreshMetadataAsync()
        {
            return Task.Run(() => RefreshMetadata());
        }

        public virtual void RemoveMedia(params MediaBase[] medias)
        {
            foreach (MediaBase media in medias)
            {
                this.medias.Remove(media);
            }
        }

        #endregion Public Methods
    }
}