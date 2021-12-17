using Flexx.Data;
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

    public static void AddToFirewall()
    {
        if (OperatingSystem.IsWindows())
        {
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = Paths.ExecutingBinary,
                    Arguments = "-firewall",
                    Verb = "runas",
                    UseShellExecute = true,
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            if (!config.Setup)
            {
                log.Warn("Adding FlexxTV Media Server to Firewall");

                AddToFirewall();
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
                SetupProcess.Run().Wait();
                ModifyWindow(true);
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
                        FirewallManager.FirewallCom firewall = new();
                        firewall.AddAuthorizeApp(new("FlexxTV Media Server", Paths.ExecutingBinary) { Enabled = true });
                        Environment.Exit(0);
                        continue;
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
                    ModifyWindow(true);
                });
                webBuilder.UseStartup<Startup>();
                log.Debug("HTTP Server is Now Active!!!");
            }).Build().Run();
        });

    #endregion Public Methods
}