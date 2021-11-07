using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Libraries
{
    public class LibraryModel
    {
        protected List<MediaBase> medias;

        protected LibraryModel()
        {
            medias = new();
        }

        public virtual void Initialize()
        {
        }

        public virtual MediaBase GetMediaByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }

                return medias.Where(m => m.Title.ToLower().Equals(name.ToLower())).FirstOrDefault();
            }
            catch (Exception e)
            {
                log.Error($"Had trouble trying to Get Media object by the provided Name {name}", e);
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
                        log.Warn($"Not Adding Reduntant Title \"{media.Title}\"");
                    }
                }
            }
        }

        public MediaBase[] GetMediaItems()
        {
            return medias.ToArray();
        }

        public virtual void RemoveMedia(params MediaBase[] medias)
        {
            foreach (MediaBase media in medias)
            {
                this.medias.Remove(media);
            }
        }

        public virtual Task RefreshMetadataAsync()
        {
            return Task.Run(() => RefreshMetadata());
        }

        public virtual void RefreshMetadata()
        {
            foreach (MediaBase media in medias)
            {
                media.UpdateMetaData();
            }
        }
    }
}