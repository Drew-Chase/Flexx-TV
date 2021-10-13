namespace Flexx.Core.Networking
{
    public interface ITorrentClient
    {
        public string API_Root { get; set; }

        void SendMagnetURI(string magnet);
    }
}