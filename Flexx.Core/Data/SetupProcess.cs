using Flexx.Authentication;
using Flexx.Media.Objects.Libraries;
using Flexx.Networking;
using Flexx.Utilities;
using System;
using System.Threading.Tasks;
using static Flexx.Data.Global;

namespace Flexx.Data
{
    public static class SetupProcess
    {
        #region Public Methods

        public static Task Run()
        {
            return Task.Run(() =>
            {
                SetupStep[] steps = new SetupStep[]
                {
                    new ("Cleaning Temporary Files", ()=> System.IO.Directory.Delete(Paths.TempData, true)),
                    new("Loading User Data", () => _ = Users.Instance),
                    new("Loading Configuration Settings", () => config = new()),
                    new("Initializing Transcoder", () => Transcoder.Init()),
                    new("Gathering Needed Assets", () =>
                    {
                        _ = Paths.MissingPoster;
                        _ = Paths.MissingCover;
                    }),
                    new("Initializing Movie Library", () => MovieLibraryModel.Instance.Initialize()),
                    new("Initializing TV Library", () => TvLibraryModel.Instance.Initialize()),
                    new("Fetching Movie Trailers", () => MovieLibraryModel.Instance.FetchAllTrailers()),
                    new("Fetching Movie Discovery Categories", () => Scanner.Prefetch(true, false)),
                    new("Fetching TV Discovery Categories", () => Scanner.Prefetch(false, false)),
                    new("Running Movie Post Initialization Event", () => MovieLibraryModel.Instance.PostInitializationEvent()),
                    new("Running TV Post Initialization Event", () => TvLibraryModel.Instance.PostInitializationEvent()),

                    new ("Running Network Initialization Event", ()=> Firewall.OpenPort(config.ApiPort).Wait()),
                    new("Initializing Command Engine", () => CommandEngine.Run()),
                };

                for (int i = 0; i < steps.Length; i++)
                {
                    log.Warn($"{steps[i].Name} ({i + 1}/{steps.Length})...");

                    if (i == steps.Length - 1)
                        log.Info($"Server is Now ACTIVE.", "Type \"help\" for help");

                    steps[i].Action.Invoke();
                }
            });
        }

        #endregion Public Methods

        #region Internal Classes

        internal static class CommandEngine
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

        internal class SetupStep
        {
            #region Public Constructors

            public SetupStep(string Name, Action Action)
            {
                this.Name = Name;
                this.Action = Action;
            }

            #endregion Public Constructors

            #region Public Properties

            public Action Action { get; }

            public string Name { get; }

            #endregion Public Properties
        }

        #endregion Internal Classes
    }
}