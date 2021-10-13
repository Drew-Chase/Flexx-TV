using Flexx.Media.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexx.Media.Objects.Libraries
{
    public class LibraryModel
    {
        protected List<IMedia> medias;

        protected LibraryModel()
        {
            medias = new();
        }

        public virtual void Initialize()
        {
        }

        public virtual IMedia GetMediaByName(string name)
        {
            name = name.ToLower();
            foreach (IMedia media in medias)
            {
                if (media.Title.ToLower().Equals(name) || media.FileName.ToLower().Equals(name))
                {
                    return media;
                }
            }
            return null;
        }

        public virtual IMedia[] GetMediaByYear(ushort year)
        {
            List<IMedia> list = new();
            foreach (IMedia media in medias)
            {
                if (media.ReleaseDate.Year.Equals(year))
                {
                    list.Add(media);
                }
            }
            return list.ToArray();
        }

        public virtual void AddMedia(params IMedia[] medias)
        {
            foreach (IMedia media in medias)
            {
                if (media != null)
                {
                    this.medias.Add(media);
                }
            }
        }

        public virtual void RemoveMedia(params IMedia[] medias)
        {
            foreach (IMedia media in medias)
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
            foreach (IMedia media in medias)
            {
                media.UpdateMetaData();
            }
        }
    }
}