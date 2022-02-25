using Flexx.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using static Flexx.Data.Global;

namespace Flexx.Networking;

public static class Remote
{
    #region Public Methods

    public static bool IsServerRegistered(User user)
    {
        if (user != null && string.IsNullOrWhiteSpace(user.Token) && user.IsHost)
        {
            HttpResponseMessage response = new HttpClient().GetAsync($"{Paths.FlexxAuthURL}/getServer.php?{user.Token}").Result;
            if (response.IsSuccessStatusCode)
            {
                JObject json = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                return json.ContainsKey("exists") && (bool) json["exists"];
            }
        }
        return false;
    }

    public static bool RegisterServer(User user)
    {
        if (user.IsAuthorized(PlanTier.Free))
        {
            HttpResponseMessage response = new HttpClient().PostAsync($"{Paths.FlexxAuthURL}/addServer.php", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("host", user.Token),
                new KeyValuePair<string,string>("public", Firewall.GetPublicIP().ToString()),
                new KeyValuePair<string,string>("local", Firewall.GetLocalIP().ToString()),
                new KeyValuePair<string,string>("port", config.ApiPort.ToString()),
            })).Result;
            if (response.IsSuccessStatusCode)
            {
                string content = response.Content.ReadAsStringAsync().Result;
                JObject json = (JObject) JsonConvert.DeserializeObject(content);
                if (json != null)
                {
                    return (json.ContainsKey("error") && (string) json["error"] == "Server already exists") || (json.ContainsKey("token") && new HttpClient().PostAsync($"{Paths.FlexxAuthURL}/updateAccount.php", new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string,string>("token", user.Token),
                        new KeyValuePair<string,string>("property", "servers"),
                        new KeyValuePair<string,string>("value", user.Token),
                    })).Result.IsSuccessStatusCode);
                }
            }
        }
        return false;
    }

    #endregion Public Methods
}