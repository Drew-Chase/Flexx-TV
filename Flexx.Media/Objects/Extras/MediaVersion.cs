namespace Flexx.Media.Objects.Extras
{
    public struct MediaVersion
    {
        public string DisplayName { get; }
        public int Width { get; }
        public int BitRate { get; }

        public MediaVersion(string DisplayName, int Width, int BitRate)
        {
            this.DisplayName = DisplayName;
            this.Width = Width;
            this.BitRate = BitRate;
        }
    }
}