using Flexx.Authentication;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Server;

public class Program
{
    public static void Main(string[] args)
    {
        Functions.InitializeServer().Wait();
        StartHttpServer(args);
        CommandManager();
    }

    public static Task StartHttpServer(string[] args) =>
        Task.Run(() =>
        {
            log.Warn("Server is Starting...");
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel(options =>
                {
                    options.ListenAnyIP(3208);
                });
                webBuilder.UseStartup<Startup>();
                log.Info("Server is Now Active!!!");
            }).Build().Run();
        });

    private static Dictionary<string, string> command_helper = new()
    {
        { "help", "Displays this message." },
        { "clear", "Clears console window." },
        { "list-streams", "Will list all active streams along with their stream info." },
        { "list-movies", "Will list all downloaded and added movies." },
        { "list-tv", "Will list all downloaded and added tv shows." },
    };

    private static Dictionary<string, Action> commands = new()
    {
        {
            "help",
            () => CommandHelp()
        },
        {
            "clear",
            () => Console.Clear()
        },
        {
            "list-movies",
            () =>
            {
                foreach (var movie in MovieLibraryModel.Instance.GetLocalList(Users.Instance.GetGuestUser()))
                {
                    log.Debug($"{movie.Title} ({movie.Year}) -------------> {movie.Plot}");
                }
            }
        },
        {
            "list-tv",
            () =>
            {
                foreach (var show in TvLibraryModel.Instance.GetLocalList(Users.Instance.GetGuestUser()))
                {
                    log.Debug($"{show.Title} ({show.Year}) [{show.Seasons} Seasons | {show.Episodes} Episodes] -------------> {show.Plot}");
                }
            }
        }
    };

    private static void CommandManager()
    {
        Console.Write(">>> ");
        string command = Console.ReadLine().ToLower();
        bool found = false;
        foreach (var cmd in commands)
        {
            if (command.Equals(cmd.Key.ToLower()))
            {
                cmd.Value.Invoke();
                found = true;
                break;
            }
        }
        if (!found)
        {
            log.Error($"Unkown command \"{command}\"");
            CommandHelp();
        }
        CommandManager();
    }

    private static void CommandHelp()
    {
        foreach (var cmd in command_helper)
        {
            log.Debug($"{cmd.Key} -------------> {cmd.Value}");
        }
    }
}