﻿using ChaseLabs.CLLogger;
using Flexx.Authentication;
using Flexx.Media.Objects.Libraries;
using Flexx.Utilities;
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


}