using Flexx.Core.Authentication;
using Flexx.Media.Objects.Libraries;
using Flexx.Media.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Directory.Delete(Paths.TempData, true);
            config = new();
            FFMpegUtil.Init();
            Task.Run(() => MovieLibraryModel.Instance.Initialize()).ContinueWith(a => log.Warn("Done Loading Movies"));
            Task.Run(() => TvLibraryModel.Instance.Initialize()).ContinueWith(a => log.Warn("Done Loading TV Shows"));
            _ = Users.Instance;
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                foreach (System.Diagnostics.Process process in FFMpegUtil.Instance.ActiveTranscodingProcess)
                {
                    process.Kill();
                }
            };
            log.Info("Server is Launching");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
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
}