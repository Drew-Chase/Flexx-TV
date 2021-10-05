using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flexx.Core.Data.Exceptions;
using Flexx.Media.Interfaces;
using System.Linq;

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
            medias = medias.OrderBy(m => m.Title).ToList();
        }
        public virtual IMedia GetMediaByName(string name)
        {
            name = name.ToLower();
            foreach (var media in medias)
            {
                if (media.Title.ToLower().Equals(name) || media.FileName.ToLower().Equals(name))
                {
                    return media;
                }
            }
            throw new MediaNotFoundException(name);
        }

        public virtual IMedia[] GetMediaByYear(ushort year)
        {
            List<IMedia> list = new();
            foreach (var media in medias)
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
            if (medias.Length != 0)
                this.medias.AddRange(medias);
        }
        public virtual void RemoveMedia(params IMedia[] medias)
        {
            foreach (IMedia media in medias)
            {
                this.medias.Remove(media);
            }
        }
        public virtual Task RefreshMetadataAsync() => Task.Run(() => RefreshMetadata());
        public virtual void RefreshMetadata()
        {
            foreach (IMedia media in medias)
            {
                media.UpdateMetaData();
            }
        }
    }
}
