using DEnc;
using Flexx.Media.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Utilities
{
    public class Transcoder
    {
        public static Transcoder Instance = null;
        public List<Process> ActiveTranscodingProcess;

        private Transcoder()
        {
            ActiveTranscodingProcess = new();
            log.Debug($"Initializing Transcoder");
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
                            if (!string.IsNullOrWhiteSpace(new FileInfo(file).Extension))
                            {
                                continue;
                            }

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
            if (Instance != null)
            {
                return;
            }

            Instance = new Transcoder();
        }

        public static void OptimizePoster(string input, string output)
        {
            OptimizeImage(input, output, 320);
        }

        public static void OptimizeCover(string input, string output)
        {
            OptimizeImage(input, output, 1280);
        }

        public static void OptimizeLogo(string input, string output)
        {
            OptimizeImage(input, output, 500);
        }

        public static void OptimizeImage(string input, string output, int scale)
        {
            string exe = Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0];
            if (File.Exists(output))
            {
                File.Delete(output);
            }

            Process process = new()
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

        public static DashEncodeResult GetTranscodedStream(string requestedUser, MediaBase media, int targetWidth, int targetHeight, int targetBitRate)
        {
            if (string.IsNullOrWhiteSpace(media.PATH))
            {
                return null;
            }
            Encoder encoder = new(Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0], Directory.GetFiles(Paths.FFMpeg, "ffprobe*", SearchOption.AllDirectories)[0], "", output => log.Debug(output), output => log.Error(output), Paths.TranscodedData(requestedUser));
            encoder.EnableStreamCopying = true;
            return encoder.GenerateDash(media.PATH, Path.Combine(Paths.TranscodedData(requestedUser), $"{media.Title}"), 24, 24, Quality.GenerateDefaultQualities(DefaultQuality.medium, "medium"), outDirectory: Paths.TranscodedData(requestedUser));
        }
    }
}