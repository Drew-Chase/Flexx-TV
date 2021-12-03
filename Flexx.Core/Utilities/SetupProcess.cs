using Flexx.Authentication;
using Flexx.Data;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using System;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Utilities
{
    public static class SetupProcess
    {
        public static Task Run()
        {
            return Task.Run(() =>
            {
                SetupStep[] steps = new SetupStep[]
                {
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
                    new("Fetching Movie Discovery Categories", () => Scanner.Prefetch(true, true)),
                    new("Fetching TV Discovery Categories", () => Scanner.Prefetch(false, true)),
                    new("Running Movie Post Initialization Event", () => MovieLibraryModel.Instance.PostInitializationEvent()),
                    new("Running TV Post Initialization Event", () => TvLibraryModel.Instance.PostInitializationEvent()),
                    new("Initializing Command Engine", () => CommandEngine.Run()),
                };

                for (int i = 0; i < steps.Length; i++)
                {
                    log.Warn($"{steps[i].Name} ({i + 1}/{steps.Length + 1})...");

                    if (i == steps.Length - 1)
                        log.Info($"Server is Now ACTIVE.", "Type \"help\" for help");

                    steps[i].Action.Invoke();
                    if (i != steps.Length - 1)
                        log.Info($"Done {steps[i].Name}! ({i + 1}/{steps.Length + 1})");
                }
            });
        }
    }

    internal class SetupStep
    {
        public string Name { get; }
        public Action Action { get; }

        public SetupStep(string Name, Action Action)
        {
            this.Name = Name;
            this.Action = Action;
        }
    }
}