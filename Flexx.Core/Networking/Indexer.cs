using System.Collections.Generic;
using System.Net;

namespace Flexx.Core.Networking
{
    public class Indexer
    {
        public Indexer()
        {
        }

        public static string[] GetMagnetList(string query)
        {
            List<string> links = new();
            string[] lines = new WebClient().DownloadString($"http://play.drewchaseproject.com:9117/api/v2.0/indexers/all/results/torznab/api?apikey=jmwck51nph2uwy2fvzaawnlpums7clqa&q={query}").Split("\n");
            foreach (string line in lines)
            {
                if (line.Trim().StartsWith("<link>magnet"))
                {
                    links.Add(line.Replace("<link>", "").Replace("</link>", "").Trim());
                }
            }
            return links.ToArray();
        }
    }
}