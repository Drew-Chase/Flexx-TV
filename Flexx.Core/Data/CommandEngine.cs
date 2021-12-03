using Flexx.Authentication;
using Flexx.Media.Objects.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Data
{
    public static class CommandEngine
    {

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

        public static void Run()
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
                log.Error($"Unkown command \"{command}\"");
                CommandHelp();
            }
            Run();
        }

        private static void CommandHelp()
        {
            foreach (var cmd in commands)
            {
                log.Debug($"{cmd.Name} -------------> {cmd.Description}");
            }
        }

        internal class Command
        {
            public string Name { get; }
            public string Description { get; }
            public Action Action { get; }
            public Command(string Name, string Description, Action Action)
            {
                this.Name = Name;
                this.Description = Description;
                this.Action = Action;
            }
        }
    }
}
