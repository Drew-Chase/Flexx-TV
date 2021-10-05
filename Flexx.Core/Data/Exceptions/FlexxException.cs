using System;
namespace Flexx.Core.Data.Exceptions
{
    public class FlexxException : Exception
    {
        public FlexxException(string message) : base(message) { }
    }
    public class LibraryNotFoundException : FlexxException
    {
        public LibraryNotFoundException(string library) : base($"Library {library} does NOT Exist!") { }
    }
    public class MediaNotFoundException : FlexxException
    {
        public MediaNotFoundException(string file) : base($"Media File located at \"{file}\" could not be found") { }
    }

    public class SeriesNotFoundException : FlexxException
    {
        public SeriesNotFoundException(string name) : base($"No Show named {name} was loaded") { }
    }
    public class SeasonNotFoundException : FlexxException
    {
        public SeasonNotFoundException(int number) : base($"Season #${number} doesn't Exist!") { }
    }
    public class EpisodeNotFoundException : FlexxException
    {
        public EpisodeNotFoundException(int episode_number, int season_number, string series) : base($"S{season_number}E{episode_number} of {series} doesn't Exist!") { }
    }
}
