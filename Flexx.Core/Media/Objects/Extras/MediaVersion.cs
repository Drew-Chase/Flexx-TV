using System.Diagnostics.CodeAnalysis;

namespace Flexx.Media.Objects.Extras
{
    public struct MediaVersion
    {
        #region Public Constructors

        public MediaVersion(string DisplayName, int Height, int BitRate)
        {
            this.DisplayName = DisplayName;
            this.Height = Height;
            this.BitRate = BitRate;
        }

        #endregion Public Constructors

        #region Public Properties

        public int BitRate { get; }

        public string DisplayName { get; }

        public int Height { get; }

        #endregion Public Properties

        #region Public Methods

        public static bool operator !=(MediaVersion a, MediaVersion b)
        {
            return !(a == b);
        }

        public static bool operator ==(MediaVersion a, MediaVersion b)
        {
            return a.Equals(b);
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return ((MediaVersion)obj).DisplayName.Equals(DisplayName);
        }

        public override string ToString()
        {
            return DisplayName;
        }

        #endregion Public Methods
    }
}