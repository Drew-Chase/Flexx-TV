using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Objects.Extras
{
    public class CastListModel
    {
        private readonly CastModel[] FullCast;

        public CastListModel(string media_type, string tmdb)
        {
            List<CastModel> cast = new();
            JObject json = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString($"https://api.themoviedb.org/3/{media_type}/{tmdb}/credits?api_key={TMDB_API}"));
            Parallel.ForEach((JArray)json["cast"], token =>
            {
                cast.Add(new(token["name"].ToString(), token["character"].ToString(), token["profile_path"].ToString(), "Actor"));
            });
            Parallel.ForEach((JArray)json["crew"], token =>
            {
                cast.Add(new(token["name"].ToString(), token["department"].ToString(), token["profile_path"].ToString(), token["job"].ToString()));
            });
            FullCast = cast.ToArray();
        }
        public CastModel[] GetCast()
        {
            return FullCast;
        }

        public CastModel[] GetCast(string job)
        {
            return FullCast.Where(c => c.Job.Equals(job)).ToArray();
        }

    }

    public class CastModel
    {
        public string Name { get; private set; }
        public string Role { get; private set; }
        public string ProfileImage { get; private set; }
        public string Job { get; private set; }

        public CastModel(string Name, string Role, string ProfileImage, string Job)
        {
            this.Name = Name;
            this.Role = Role;
            this.ProfileImage = $"https://image.tmdb.org/t/p/original{ProfileImage}";
            this.Job = Job;
        }
    }
}