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

    public static bool RegisterServer(User user)
    {
        if (user.IsAuthorized(PlanTier.Free))
        {
            HttpResponseMessage response = new HttpClient().PostAsync($"http://localhost/addServer.php", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("host", user.Token),
                new KeyValuePair<string,string>("public", Firewall.GetPublicIP().ToString()),
                new KeyValuePair<string,string>("local", Firewall.GetLocalIP().ToString()),
                new KeyValuePair<string,string>("port", config.ApiPort.ToString()),
            })).Result;
            if (response.IsSuccessStatusCode)
            {
                string content = response.Content.ReadAsStringAsync().Result;
                JObject json = (JObject)JsonConvert.DeserializeObject(content);
                return json.ContainsKey("token");
            }
        }
        return false;
    }

    #endregion Public Methods
}