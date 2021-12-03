using Flexx.Authentication;
using Flexx.Media.Objects.Libraries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
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
            object jresult = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/{media_type}/{tmdb}/credits?api_key={TMDB_API}");
            if (jresult == null) return;
            JObject json = (JObject)jresult;
            Parallel.ForEach((JArray)json["cast"], token =>
            {
                var c = new CastModel((string)token["name"], (string)token["character"], (string)token["profile_path"], "Actor");
                if (c != null && !string.IsNullOrWhiteSpace((string)token["profile_path"]))
                    cast.Add(c);
            });
            Parallel.ForEach((JArray)json["crew"], token =>
            {
                var c = new CastModel((string)token["name"], (string)token["department"], (string)token["profile_path"], (string)token["job"]);
                if (c != null && !string.IsNullOrWhiteSpace((string)token["profile_path"]))
                    cast.Add(c);
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

        public static object[] GetMediaByActor(string actor)
        {
            List<object> list = new();

            foreach (var movie in MovieLibraryModel.Instance.GetList(Users.Instance.GetGuestUser()))
            {
                foreach (var member in movie.MainCast)
                {
                    if (member.Name == actor)
                    {
                        if (!list.Contains(movie))
                            list.Add(movie);
                    }
                }
            }

            foreach (var show in TvLibraryModel.Instance.GetList(Users.Instance.GetGuestUser()))
            {
                foreach (var member in show.MainCast)
                {
                    if (member.Name == actor)
                    {
                        if (!list.Contains(show))
                            list.Add(show);
                    }
                }
            }
            object obj = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/search/person?api_key={TMDB_API}&language={config.LanguagePreference}&query={actor}&page=1&include_adult=false");
            if (obj != null)
            {
                foreach (JObject json in (JArray)((JToken)obj)["results"][0]["known_for"])
                {
                    string type = (string)json["media_type"];
                    object jobj = Functions.GetJsonObjectFromURL($"https://api.themoviedb.org/3/{type}/{(json["id"])}?api_key={TMDB_API}");
                    if (jobj != null)
                    {
                        string knownFor = JsonConvert.SerializeObject(jobj);
                        list.Add(type.Equals("movie") ? new MovieObject(knownFor) : new SeriesObject(knownFor));
                    }
                }
            }
            return list.ToArray();
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
            if (!string.IsNullOrWhiteSpace(Role))
            {
                this.Role = Role;
            }

            if (!string.IsNullOrWhiteSpace(ProfileImage))
            {
                this.ProfileImage = $"https://image.tmdb.org/t/p/original{ProfileImage}";
            }

            if (!string.IsNullOrWhiteSpace(Job))
            {
                this.Job = Job;
            }
        }
    }
}