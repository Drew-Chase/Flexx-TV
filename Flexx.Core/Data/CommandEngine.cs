using Flexx.Authentication;
using Flexx.Media.Objects.Libraries;
using System;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Data
{
    public static class CommandEngine
    {
        #region Private Fields

        private static readonly Command[] commands = new Command[]
        {
            new ("help", "Displays this message.", ()=>CommandHelp()),
            new ("clear", "Clears console window.", ()=>Console.Clear()),
            new ("list-streams", "Will list all active streams along with their stream info.", ()=>log.Error($"This Command has no functionality yet...")),
            new ("list-movies", "Will list all downloaded and added movies.", ()=>
                {
                    foreach (var movie in MovieLibraryModel.Instance.GetLocalList(Users.Instance.GetGuestUser()))
                    {
                        log.Debug($"{movie.Title} ({movie.Year}) -------------> {movie.Plot}");
                    }
                }),
            new ("list-tv", "Will list all downloaded and added tv shows.", ()=>
                {
                    foreach (var show in TvLibraryModel.Instance.GetLocalList(Users.Instance.GetGuestUser()))
                    {
                        log.Debug($"{show.Title} ({show.Year}) [{show.Seasons} Seasons | {show.Episodes} Episodes] -------------> {show.Plot}");
                    }
                }),
        };

        #endregion Private Fields

        #region Public Methods

        public static Task Run()
        {
            return Task.Run(() =>
            {
                Console.Write(">>> ");
                string command = Console.ReadLine().ToLower();
                bool found = false;
                foreach (var cmd in commands)
                {
                    if (command.Equals(cmd.Name.ToLower()))
                    {
                        cmd.Action.Invoke();
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    log.Error($"Unknown command \"{command}\"");
                    CommandHelp();
                }
                Run();
            });
        }

        #endregion Public Methods

        #region Private Methods

        private static void CommandHelp()
        {
            foreach (var cmd in commands)
            {
                log.Debug($"{cmd.Name} -------------> {cmd.Description}");
            }
        }

        #endregion Private Methods

        #region Internal Classes

        internal class Command
        {
            #region Public Constructors

            public Command(string Name, string Description, Action Action)
            {
                this.Name = Name;
                this.Description = Description;
                this.Action = Action;
            }

            #endregion Public Constructors

            #region Public Properties

            public Action Action { get; }

            public string Description { get; }

            public string Name { get; }

            #endregion Public Properties
        }

        #endregion Internal Classes
    }
}