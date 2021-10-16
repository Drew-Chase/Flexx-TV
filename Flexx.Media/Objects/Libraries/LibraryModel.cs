using System.Collections.Generic;
using System.Threading.Tasks;

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
            name = name.ToLower();
            foreach (MediaBase media in medias)
            {
                if (media.Title.ToLower().Equals(name) || media.FileName.ToLower().Equals(name))
                {
                    return media;
                }
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
                if (media != null)
                {
                    this.medias.Add(media);
                }
            }
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