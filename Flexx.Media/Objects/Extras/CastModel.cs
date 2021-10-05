using System;
using System.Net;
using Flexx.Media.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Extras
{
    public enum CastType
    {
        Actor,
        Director,
        Writer,
        Producter,
        Generic
    }
    public class CastListModel
    {
        public CastType Type { get; private set; }
        private IMedia Media;
        private CastModel[] FullCast;
        public CastListModel(IMedia Media)
        {
            this.Media = Media;
            JObject json;
            using (WebClient client = new())
            {
                if (Media.GetType().Equals(typeof(MovieModel)))
                    json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/movie/{((MovieModel)Media).TMDB}/credits?api_key={TMDB_API}"));
                else
                    json = (JObject)JsonConvert.DeserializeObject(client.DownloadString($"https://api.themoviedb.org/3/tv/{((EpisodeModel)Media).Season.Series.TMDB}/credits?api_key={TMDB_API}"));
            }
        }


    }
    public class CastModel
    {
        public string Name { get; private set; }
        public string Role { get; private set; }
        public string ProfileImage { get; private set; }
        public CastModel(string Name, string Role, string ProfileImage)
        {
            this.Name = Name;
            this.Role = Role;
            this.ProfileImage = ProfileImage;
        }

    }
}
