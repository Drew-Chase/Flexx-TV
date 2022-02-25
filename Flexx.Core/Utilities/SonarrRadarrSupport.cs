using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static Flexx.Data.Global;

namespace Flexx.Utilities
{
    public static class SonarrRadarrSupport
    {
        #region Public Methods

        /// <summary>
        /// Adds Movie/Show to Radarr/Sonarr
        /// </summary>
        /// <param name="radarr"> rather to use radarr or sonarr </param>
        /// <param name="media">  The FlexxTV media object </param>
        public static void AddToSonarrRadarr(bool radarr, MediaBase media)
        {
            if (TestConnection(radarr, out string url, out string api))
            {
                HttpClient client = new();
                HttpResponseMessage response;
                if (radarr)
                {
                }
                else
                {
                    response = client.GetAsync($"{url}/series/lookup?term={media.Title}&apikey={api}").Result;
                    if (Functions.TryGetJsonObjectFromURL($"{url}/series/lookup?term={media.Title}&apikey={api}", out JObject json))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            response = client.PostAsync($"{url}", new FormUrlEncodedContent(new[]
                            {
                                new KeyValuePair<string, string>("tvdbId", ""), new KeyValuePair<string, string>("title", ""),
                                new KeyValuePair<string, string>("profileId", ""), new KeyValuePair<string, string>("titleSlug", ""),
                                new KeyValuePair<string, string>("images", ""), new KeyValuePair<string, string>("seasons", ""),
                                new KeyValuePair<string, string>("monitored", "true"),
                            })).Result;
                            if (response.IsSuccessStatusCode)
                            {
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets all currently queued items for download
        /// </summary>
        /// <param name="radarr"> rather its radarr or sonarr </param>
        /// <returns> array of queued items with necessary information </returns>
        public static object[] GetDownloading(bool radarr)
        {
            if (TestConnection(radarr, out string url, out string api))
            {
                if (Functions.TryGetJsonObjectFromURL(url, out JArray jobj))
                {
                    List<object> list = new();
                    Parallel.ForEach(jobj, json =>
                   {
                       object item = new { };
                       string status = (string) json["status"];
                       string resolution = (string) json["quality"]["quality"]["name"];
                       double percentage = int.TryParse((string) json["sizeleft"], out int remaining) && int.TryParse((string) json["size"], out int total) ? Math.Round((double) ((remaining / total) - 1) * -1 * 100, 2) : -1;
                       if (radarr)
                       {
                           MovieModel media = MovieLibraryModel.Instance.GetMovieByName((string) json["movie"]["title"], int.Parse((string) json["movie"]["year"]));
                           if (media != null)
                           {
                               item = new
                               {
                                   id = media.TMDB,
                                   status,
                                   resolution,
                                   percentage,
                               };
                           }
                       }
                       else
                       {
                           EpisodeModel media = TvLibraryModel.Instance.GetTVShowByName((string) json["series"]["title"], int.Parse((string) json["series"]["year"])).GetSeasonByNumber(int.Parse((string) json["episode"]["seasonNumber"])).GetEpisodeByNumber(int.Parse((string) json["episode"]["episodeNumber"]));
                           if (media != null)
                           {
                               item = new
                               {
                                   id = media.TMDB,
                                   seasson = media.Season.Season_Number,
                                   episode = media.Episode_Number,
                                   status,
                                   resolution,
                                   percentage,
                               };
                           }
                       }
                       list.Add(item);
                   });
                    return list.ToArray();
                }
            }

            return null;
        }

        /// <summary>
        /// Will test connection and return rather it was successful, the <see
        /// cref="HttpResponseMessage"> Response Message </see>, the connection url and the api key
        /// </summary>
        /// <param name="radarr">   If it should test radarr or sonarr </param>
        /// <param name="response"> The connection response </param>
        /// <param name="url">      The connection url </param>
        /// <param name="api">      The connection api key </param>
        /// <returns> If it was successful </returns>
        public static bool TestConnection(bool radarr, out HttpResponseMessage response, out string url, out string api)
        {
            if (string.IsNullOrWhiteSpace(radarr ? config.RadarrBaseUrl : config.SonarrBaseUrl) || string.IsNullOrWhiteSpace(radarr ? config.RadarrAPIKey : config.SonarrAPIKey) || (radarr ? config.RadarrPort : config.SonarrPort) == -1)
            {
                response = null;
                url = "";
                api = "";
                if (radarr)
                    config.RadarrConnected = false;
                else
                    config.SonarrConnected = false;
                return false;
            }
            try
            {
                if (radarr)
                {
                    url = $"{config.RadarrBaseUrl}:{config.RadarrPort}/api";
                    api = config.RadarrAPIKey;
                    response = new HttpClient().GetAsync($"{url}/system/status?apikey={api}").Result;
                    config.RadarrConnected = response.IsSuccessStatusCode;
                }
                else
                {
                    url = $"{config.SonarrBaseUrl}:{config.SonarrPort}/api";
                    api = config.SonarrAPIKey;
                    response = new HttpClient().GetAsync($"{url}/system/status?apikey={api}").Result;
                    config.SonarrConnected = response.IsSuccessStatusCode;
                }
                return response.IsSuccessStatusCode;
            }
            catch
            {
                response = null;
                url = "";
                api = "";
                if (radarr)
                    config.RadarrConnected = false;
                else
                    config.SonarrConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Will test connection and return rather it was successful and the <see
        /// cref="HttpResponseMessage"> Response Message </see>
        /// </summary>
        /// <param name="radarr">   If it should test radarr or sonarr </param>
        /// <param name="response"> The connection response </param>
        /// <returns> If it was successful </returns>
        public static bool TestConnection(bool radarr, out HttpResponseMessage response)
        {
            return TestConnection(radarr, out response, out _, out _);
        }

        /// <summary>
        /// Will test connection and return connection url and api key
        /// </summary>
        /// <param name="radarr"> If it should test radarr or sonarr </param>
        /// <param name="url">    The connection url </param>
        /// <param name="api">    The connection api key </param>
        /// <returns> </returns>
        public static bool TestConnection(bool radarr, out string url, out string api)
        {
            return TestConnection(radarr, out _, out url, out api);
        }

        /// <summary>
        /// Will test connection and return only rather it was successful
        /// </summary>
        /// <param name="radarr"> If it should test radarr or sonarr </param>
        /// <returns> If it was successful </returns>
        public static bool TestConnection(bool radarr)
        {
            return TestConnection(radarr, out _, out _, out _);
        }

        /// <summary>
        /// Will Test both radarr and sonarr
        /// </summary>
        /// <returns> If it was successful </returns>
        public static bool TestConnection()
        {
            return TestConnection(true) && TestConnection(false);
        }

        #endregion Public Methods
    }
}