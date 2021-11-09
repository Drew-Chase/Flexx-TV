using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                if (Functions.IsFileLocked(new FileInfo(output)))
                {
                    Thread.Sleep(500);
                    OptimizeImage(input, output, scale);
                }
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

        private static readonly List<MediaVersion> AllResolutions = new()
        {
            //4K
            new("4K High", 3840, 34000),  //High
            new("4K Medium", 3840, 20000), //Medium
            new("4K Low", 3840, 13000), //Low
            //1080p
            new("1080p High", 1920, 8000),  //High
            new("1080p Medium", 1920, 6000), //Medium
            new("1080p Low", 1920, 4500), //Low
            //720p
            new("720p High", 1280, 4000),  //High
            new("720p Medium", 1280, 2250), //Medium
            new("720p Low", 1280, 1500), //Low
            //480p
            new("480p Low", 854, 500), //Low
            //360p
            new("360p Low", 640, 400), //Low
            //240p
            new("240p Low", 426, 300), //Low
        };

        public static MediaVersion[] CreateVersion(MediaBase media, bool force = false)
        {
            List<MediaVersion> Versions = new();
            try
            {
                IVideoStream videoStream = media.MediaInfo.VideoStreams.ToArray()[0];
                log.Warn($"Original File = Title: {media.Title}, Resolution: {videoStream.Width}x{videoStream.Height}, Bitrate: {videoStream.Bitrate / 1000}Kbps");
                foreach (MediaVersion resolution in AllResolutions)
                {
                    if (videoStream.Width >= resolution.Width - 100 && videoStream.Bitrate / 1000 >= resolution.BitRate)
                        Versions.Add(resolution);
                }
            }
            catch (Exception e)
            {
                log.Error("Had issue while fetching resolution data for PreTranscoding Alternative versions", e);
            }
            Versions.Reverse();
            Parallel.ForEach(Versions, new() { MaxDegreeOfParallelism = 2 }, res =>
            {
                try
                {
                    string directoryOutput = Directory.CreateDirectory(Path.Combine(Directory.GetParent(media.Metadata.PATH).FullName, "versions")).FullName;
                    string fileOutput = Paths.GetVersionPath(Directory.GetParent(media.Metadata.PATH).FullName, media.Title, res.Width, res.BitRate);
                    if (force || !File.Exists(fileOutput))
                    {
                        string exe = Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0];
                        string arguments = $"-y -i \"{media.PATH}\" -vf scale={res.Width}:-2 -preset ultrafast -pix_fmt p010le -map_metadata 0 -c:v libx264 -b:v {res.BitRate}K -c:a aac -b:a 384k -movflags +faststart -movflags use_metadata_tags \"{fileOutput}\"";
                        Process process = new()
                        {
                            StartInfo = new()
                            {
                                FileName = exe,
                                Arguments = arguments,
                                UseShellExecute = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                            },
                            EnableRaisingEvents = true,
                        };
                        process.Exited += (s, e) =>
                        {
                            log.Info($"Finished Creating Version file for \"{media.Title}\", Width={res.Width}, Bitrate={res.BitRate}Kbps");
                            Instance.ActiveTranscodingProcess.Remove(process);
                            if (!File.Exists(fileOutput))
                            {
                                log.Debug($"ffmpeg {arguments}");
                                log.Error($"Version file was corrupted \"{media.Title}\", Width={res.Width}, Bitrate={res.BitRate}Kbps");
                            }
                            else if (new FileInfo(fileOutput).Length < 1000)
                            {
                                log.Debug($"ffmpeg {arguments}");
                                File.Delete(fileOutput);
                                log.Error($"Version file was corrupted \"{media.Title}\", Width={res.Width}, Bitrate={res.BitRate}Kbps");
                            }
                        };
                        Instance.ActiveTranscodingProcess.Add(process);
                        process.Start();
                        log.Warn($"Creating Version file for \"{media.Title}\", Width={res.Width}, Bitrate={res.BitRate}Kbps");

                        process.WaitForExit();
                    }
                }
                catch (Exception e)
                {
                    Versions.Remove(res);
                    log.Error($"Had issue while running the conversion process for alternative version Width={res.Width}, Bitrate={res.BitRate}Kbps", e);
                }
            });
            return Versions.ToArray();
        }

        public static (FileStream, Process) GetTranscodedStream(string requestedUser, MediaBase media, int targetResolution, int targetBitRate)
        {
            if (string.IsNullOrWhiteSpace(media.PATH))
            {
                return (null, null);
            }
            string directoryOutput = Directory.CreateDirectory(Path.Combine(Paths.TempData, $"stream_{requestedUser}")).FullName;
            string fileOutput = Path.Combine(directoryOutput, $"v_t{media.Title}-r{targetResolution}.m3u8");
            if (File.Exists(fileOutput))
                File.Delete(fileOutput);
            File.Create(fileOutput).Close();
            string exe = Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0];
            string arguments = $"-i \"{media.PATH}\" -bitrate {targetBitRate}k -preset ultrafast -vcodec libx264 -f rtsp \"rtsp://127.0.0.1:1234\" -rtsp_transport http";
            //string arguments = $"-i \"{media.PATH}\" -bitrate {targetBitRate}k -f hls -hls_time 2 -hls_playlist_type vod -hls_flags independent_segments -hls_segment_type mpegts -hls_segment_filename \"{Path.Combine(directoryOutput, $"stream_t{media.Title}-r{targetResolution}%02d.ts")}\" \"{fileOutput}\"";
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = exe,
                    Arguments = arguments,
                    UseShellExecute = true,
                    //WindowStyle = ProcessWindowStyle.Hidden,
                },
                EnableRaisingEvents = true,
            };
            process.Exited += (s, e) =>
            {
                Instance.ActiveTranscodingProcess.Remove(process);
            };
            process.Start();
            Instance.ActiveTranscodingProcess.Add(process);
            return (new(fileOutput, FileMode.Open), process);
        }
    }
}