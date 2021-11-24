namespace Flexx.Media.Objects.Extras
{
    public struct MediaVersion
    {
        public string DisplayName { get; }
        public int Height { get; }
        public int BitRate { get; }

        public MediaVersion(string DisplayName, int Width, int BitRate)
        {
            this.DisplayName = DisplayName;
            this.Height = Width;
            this.BitRate = BitRate;
        }
    }
}