using System.Diagnostics.CodeAnalysis;

namespace Flexx.Media.Objects.Extras
{
    public struct MediaVersion
    {
        public string DisplayName { get; }
        public int Height { get; }
        public int BitRate { get; }

        public MediaVersion(string DisplayName, int Height, int BitRate)
        {
            this.DisplayName = DisplayName;
            this.Height = Height;
            this.BitRate = BitRate;
        }

        public static bool operator ==(MediaVersion a, MediaVersion b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(MediaVersion a, MediaVersion b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return ((MediaVersion)obj).DisplayName.Equals(DisplayName);
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}