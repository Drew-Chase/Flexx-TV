using Flexx.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Server;

public class Program
{
    #region Public Methods

    public static void Main(string[] args)
    {
        StartHttpServer(args);
        SetupProcess.Run().Wait();
    }

    public static Task StartHttpServer(string[] args) =>
        Task.Run(() =>
        {
            log.Debug("HTTP Server is Starting...");
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel(options =>
                {
                    options.ListenAnyIP(3208);
                });
                webBuilder.UseStartup<Startup>();
                log.Debug("HTTP Server is Now Active!!!");
            }).Build().Run();
        });

    #endregion Public Methods
}