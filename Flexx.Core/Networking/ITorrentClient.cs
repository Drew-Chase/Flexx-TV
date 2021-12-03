namespace Flexx.Networking
{
    public interface ITorrentClient
    {
        #region Public Properties

        public string API_Root { get; set; }

        #endregion Public Properties

        #region Public Methods

        void SendMagnetURI(string magnet);

        #endregion Public Methods
    }
}