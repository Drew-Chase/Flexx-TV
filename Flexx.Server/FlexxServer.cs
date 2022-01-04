using Flexx.Data;
using Flexx.Networking;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Flexx.Data.Global;

namespace Flexx.Server;

public class FlexxServer
{
    #region Public Methods

    public static void Main(string[] args)
    {
        if (OperatingSystem.IsWindows())
        {
            Console.Title = "FlexxTV Media Server";

            Firewall.AddToFirewall();
        }
        if (args.Length == 0)
        {
            if (!config.Setup || string.IsNullOrWhiteSpace(config.MovieLibraryPath) || string.IsNullOrWhiteSpace(config.TVLibraryPath))
            {
                ModifyWindow(false);

                if (OperatingSystem.IsWindows())
                {
                    new Process()
                    {
                        StartInfo = new()
                        {
                            FileName = $"http://127.0.0.1:{config.ApiPort}/Setup",
                            Verb = "open",
                            UseShellExecute = true,
                        }
                    }.Start();
                }
                StartHttpServer().Wait();
            }
            else
            {
                ModifyWindow(true);
                SetupProcess.Run().Wait();
                ModifyWindow(false);
                StartHttpServer().Wait();
            }
        }
        else
        {
            foreach (var item in args)
            {
                switch (item)
                {
                    case "-firewall":
                        Firewall.AddToFirewall();
                        Environment.Exit(0);
                        continue;
                    case "-show":
                        SetupProcess.Run().Wait();
                        ModifyWindow(true);
                        StartHttpServer().Wait();
                        break;

                    default:
                        continue;
                }
            }
        }
    }

    public static void ModifyWindow(bool show)
    {
        if (OperatingSystem.IsWindows())
        {
            [DllImport("kernel32.dll")]
            static extern IntPtr GetConsoleWindow();

            [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            const int SW_HIDE = 0; const int SW_SHOW = 5;

            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, show ? SW_SHOW : SW_HIDE);
        }
    }

    private static Task StartHttpServer() =>
            Task.Run(() =>
        {
            log.Debug("HTTP Server is Starting...");
            Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseIISIntegration();
                webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                webBuilder.UseKestrel(options =>
                {
                    options.ListenAnyIP(config.ApiPort);
                });
                webBuilder.UseStartup<Startup>();
                log.Debug("HTTP Server is Now Active!!!");
            }).Build().Run();
        });

    #endregion Public Methods
}