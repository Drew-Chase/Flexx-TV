using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Flexx.Utilities
{
    public static class ApplicationMonitor
    {
        #region Public Properties

        public static object CurrentUsage => new
        {
            CPU = FlexxCPUUsage + "%",
            GPU = FlexxGPUUsage + "%",
            Network = FlexxNetworkUsage + "Kbps",
            Memory = FlexxMemoryUsage + "MB",
        };

        #endregion Public Properties

        #region Private Properties

        private static double FlexxCPUUsage =>
            Task.Run(async () =>
            {
                try
                {
                    DateTime startTime = DateTime.Now;
                    TimeSpan startCPU = Process.GetCurrentProcess().TotalProcessorTime;
                    await Task.Delay(500);
                    return Math.Round((Process.GetCurrentProcess().TotalProcessorTime - startCPU).TotalMilliseconds / (Environment.ProcessorCount * (DateTime.Now - startTime).TotalMilliseconds) * 100, 2);
                }
                catch { }
                return 0;
            }).Result;

        private static double FlexxGPUUsage => Math.Round(0d, 2);

        private static double FlexxMemoryUsage => Math.Round(Process.GetCurrentProcess().PrivateMemorySize64 / Math.Pow(1024, 2), 2);

        private static double FlexxNetworkUsage => Math.Round(0d, 2);

        #endregion Private Properties
    }
}