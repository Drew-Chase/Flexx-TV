using System;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using static Flexx.Core.Data.Global;
using System.Diagnostics;
using Flexx.Media.Interfaces;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Flexx.Media.Utilities
{
    public class FFMpegUtil
    {
        public static FFMpegUtil Instance = null;
        public List<Process> ActiveTranscodingProcess;
        private FFMpegUtil()
        {
            ActiveTranscodingProcess = new();
            log.Debug($"Initializing FFMPEG");
            if (Directory.GetFiles(Paths.FFMpeg, "*ffmpeg*", SearchOption.AllDirectories).Length == 0)
            {
                log.Debug($"Downloading Latest FFMPEG Version");
                FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, Paths.FFMpeg).ContinueWith(e =>
                {
                    if (OperatingSystem.IsMacOS())
                    {
                        log.Debug("Setting up Unix permissions");
                        string[] files = Directory.GetFiles(Paths.FFMpeg);
                        foreach (string file in files)
                        {
                            if (!string.IsNullOrWhiteSpace(new FileInfo(file).Extension)) continue;
                            log.Debug($"Setting file {new FileInfo(file).Name} as an executable");
                            Process.Start("chmod", $"a+x {file}");
                        }
                    }
                }).Wait();
            }
            FFmpeg.SetExecutablesPath(Paths.FFMpeg);
        }
        public static void Init()
        {
            if (Instance != null) return;
            Instance = new FFMpegUtil();
        }
        public static void OptimizePoster(string input, string output)
        {
            OptimizeImage(input, output, 320);
        }
        public static void OptimizeCover(string input, string output)
        {
            OptimizeImage(input, output, 1280);
        }
        public static void OptimizeImage(string input, string output, int scale)
        {
            string exe = Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0];
            var process = new Process()
            {
                StartInfo = new()
                {
                    FileName = exe,
                    Arguments = $"-i \"{input}\" -vf scale={scale}:-1 \"{output}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
                EnableRaisingEvents = true,
            };
            process.Exited += (s, e) => File.Delete(input);
            process.Start();
        }
        public static FileStream GetTranscodedStream(string requestedUser, IMedia media, int targetResolution, int targetBitRate)
        {
            string fileOutput = Path.Combine(Directory.CreateDirectory(Path.Combine(Paths.TempData, $"stream_{requestedUser}")).FullName, $"v_{requestedUser}_{media.FileName}.m3u8");
            File.Create(fileOutput).Close();
            Thread.Sleep(500);
            string exe = Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0];
            string arguments = $" -i \"{media.PATH}\" -vf scale=w=-1:h={targetResolution}:force_original_aspect_ratio=decrease -c:a aac -ar 48000 -c:v h264 -profile:v main -crf 20 -sc_threshold 0 -g 48 -keyint_min 48 -hls_time 4 -b:v {targetBitRate}k -maxrate {targetBitRate}k -bufsize 7500k -b:a 192k -hls_segment_filename \"{fileOutput}-{targetResolution}_%d.ts\" -hls_list_size 10 -f hls hls_master_name \"{fileOutput}-{targetResolution}.m3u8\"";
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = exe,
                    Arguments = arguments,
                    RedirectStandardOutput = false,
                },
                EnableRaisingEvents = true,
            };
            process.Exited += (s, e) =>
            {
                Instance.ActiveTranscodingProcess.Remove(process);
                string dir = new FileInfo(fileOutput).Directory.FullName;
                bool issue = false;
                foreach (string file in Directory.GetFiles(dir))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        issue = true;
                        continue;
                    }
                }
                if (!issue)
                    Directory.Delete(dir, true);
            };
            process.Start();
            Instance.ActiveTranscodingProcess.Add(process);
            return new(fileOutput, FileMode.Open);
        }
    }
}
