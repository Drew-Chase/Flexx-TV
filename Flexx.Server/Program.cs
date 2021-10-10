using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Flexx.Media.Utilities;
using static Flexx.Core.Data.Global;
using Flexx.Media.Objects.Libraries;
using System.IO;

namespace Flexx.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Directory.Delete(Paths.TempData, true);
            config = new();
            FFMpegUtil.Init();
            Task.Run(() => MovieLibraryModel.Instance.Initialize());
            Task.Run(() => TvLibraryModel.Instance.Initialize());
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                foreach (var process in FFMpegUtil.Instance.ActiveTranscodingProcess)
                {
                    process.Kill();
                }
            };
            log.Info("Server is Launching");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        options.ListenAnyIP(3208);
                    });
                    webBuilder.UseStartup<Startup>();
                    log.Info("Server is Now Active");
                });
    }
}
