using Flexx.Authentication;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Extras;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using static Flexx.Core.Data.Global;

namespace Flexx.Media.Utilities;

public class Transcoder
{
    public static Transcoder Instance = null;

    public enum EncoderPreset
    {
        ultrafast,
        superfast,
        veryfast,
        faster,
        fast,
        medium,
        slow,
        slower,
        veryslow,
        placebo,
    }

    private Transcoder()
    {
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
                Arguments = $"-i \"{input}\" -pix_fmt rgba -vf scale={scale}:-1 \"{output}\"",
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
        new("4K High", 2160, 34000),  //High
        new("4K Medium", 2160, 20000), //Medium
        new("4K Low", 2160, 13000), //Low
                                    //1080p
        new("1080p High", 1080, 8000),  //High
        new("1080p Medium", 1080, 6000), //Medium
        new("1080p Low", 1080, 4500), //Low
                                      //720p
        new("720p High", 720, 4000),  //High
        new("720p Medium", 720, 2250), //Medium
        new("720p Low", 720, 1500), //Low
                                    //480p
        new("480p Low", 480, 500), //Low
                                   //360p
        new("360p Low", 360, 400), //Low
                                   //240p
        new("240p Low", 240, 300), //Low
    };

    public static MediaVersion[] GetAcceptableVersions(MediaBase media)
    {
        List<MediaVersion> Versions = new();
        try
        {
            IVideoStream videoStream = media.MediaInfo.VideoStreams.ToArray()[0];
            log.Warn($"Original File = Title: {media.Title}, Resolution: {videoStream.Width}x{videoStream.Height}, Bitrate: {videoStream.Bitrate / 1000}Kbps");
            foreach (MediaVersion resolution in AllResolutions)
            {
                if (videoStream.Width >= resolution.Height - 100 && videoStream.Bitrate / 1000 >= resolution.BitRate)
                    Versions.Add(resolution);
            }
        }
        catch (Exception e)
        {
            log.Error("Had issue while fetching resolution data for PreTranscoding Alternative versions", e);
        }
        return Versions.ToArray();
    }

    public static MediaVersion[] CreateVersion(MediaBase media, bool force = false)
    {
        List<MediaVersion> Versions = media.AlternativeVersions.Any() ? GetAcceptableVersions(media).ToList() : media.AlternativeVersions.ToList();
        Parallel.ForEach(Versions, new() { MaxDegreeOfParallelism = 2 }, res =>
        {
            try
            {
                string directoryOutput = Directory.CreateDirectory(Path.Combine(Directory.GetParent(media.Metadata.PATH).FullName, "versions")).FullName;
                string fileOutput = Paths.GetVersionPath(Directory.GetParent(media.Metadata.PATH).FullName, media.Title, res.Height, res.BitRate);
                if (force || !File.Exists(fileOutput))
                {
                    string exe = Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0];
                    string arguments = $"-y -i \"{media.PATH}\" -vf scale={res.Height}:-2 -loglevel quiet -preset ultrafast -pix_fmt p010le -map_metadata 0 -c:v libx264 -b:v {res.BitRate}K -c:a aac -b:a 384k -movflags +faststart -movflags use_metadata_tags \"{fileOutput}\"";
                    Process process = new()
                    {
                        StartInfo = new()
                        {
                            FileName = exe,
                            Arguments = arguments,
                            UseShellExecute = false,
                            //WindowStyle = ProcessWindowStyle.Hidden,
                        },
                        EnableRaisingEvents = true,
                    };
                    process.Exited += (s, e) =>
                    {
                        log.Info($"Finished Creating Version file for \"{media.Title}\", Width={res.Height}, Bitrate={res.BitRate}Kbps");
                        if (!File.Exists(fileOutput))
                        {
                            log.Debug($"ffmpeg {arguments}");
                            log.Error($"Version file was corrupted \"{media.Title}\", Width={res.Height}, Bitrate={res.BitRate}Kbps");
                        }
                        else if (new FileInfo(fileOutput).Length < 1000)
                        {
                            log.Debug($"ffmpeg {arguments}");
                            File.Delete(fileOutput);
                            log.Error($"Version file was corrupted \"{media.Title}\", Width={res.Height}, Bitrate={res.BitRate}Kbps");
                        }
                    };
                    process.Start();
                    log.Warn($"Creating Version file for \"{media.Title}\", Width={res.Height}, Bitrate={res.BitRate}Kbps");

                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Versions.Remove(res);
                log.Error($"Had issue while running the conversion process for alternative version Width={res.Height}, Bitrate={res.BitRate}Kbps", e);
            }
        });
        return Versions.ToArray();
    }

    public static MediaStream GetTranscodedStream(User requestedUser, MediaBase media, MediaVersion version, int start_time, long startTick)
    {
        if (string.IsNullOrWhiteSpace(media.PATH))
        {
            return null;
        }
        long startTime = startTick == 0 ? DateTime.Now.Ticks : startTick;
        string directoryOutput = Directory.CreateDirectory(Path.Combine(Paths.TempData, "streams", $"{requestedUser}_{version}_{start_time}_{startTime}")).FullName;
        string fileOutput = Path.Combine(directoryOutput, $"v_t{media.Title}-r{version.DisplayName}.m3u8");
        if (!File.Exists(fileOutput))
        {
            string exe = Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0];
            StringBuilder arguments = FFmpegArgumentBuilder(media.PATH, fileOutput, height: version.Height, video_bitrate: version.BitRate, start_duration: start_time);
            arguments.Append($" -hls_list_size 0 -hls_time 10 -hls_playlist_type event -f hls \"{fileOutput}\"");
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = exe,
                    Arguments = arguments.ToString(),
                    UseShellExecute = false,
                    WorkingDirectory = directoryOutput,
                },
                EnableRaisingEvents = true,
            };
            process.Start();
            while (!File.Exists(fileOutput))
            {
            }
            Thread.Sleep(500);
            return ActiveStreams.Instance.AddStream(new(requestedUser, process, version, directoryOutput, startTime, new(fileOutput, FileMode.Open, FileAccess.Read)));
        }
        return ActiveStreams.Instance.Get(directoryOutput);
    }

    //public static FileStream GetM3U8(User requestedUser, MediaBase media, MediaVersion version)
    //{
    //    string directoryOutput = Directory.CreateDirectory(Path.Combine(Paths.TempData, "streams", "hls", $"{requestedUser}", version.DisplayName)).FullName;
    //    string fileName = $"{media.TMDB}_{requestedUser}_{version.DisplayName}";
    //    string fileOutput = Path.Combine(directoryOutput, $"{fileName}.m3u8");
    //    StringBuilder builder = new();
    //    builder.Append("#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-TARGETDURATION:10\n#EXT-X-MEDIA-SEQUENCE:0\n#EXT-X-PLAYLIST-TYPE:EVENT\n");
    //    int segments = (int)Math.Floor(media.MediaInfo.Duration.TotalSeconds / 10);
    //    double lastDuration = ((media.MediaInfo.Duration.TotalSeconds / 10) - segments) * 10;
    //    for (int i = 0; i < segments; i++)
    //    {
    //        builder.Append("#EXTINF:10,\n");
    //        builder.Append($"{fileName}_{i}.ts\n");
    //    }

    //    builder.Append($"#EXTINF:{lastDuration},\n");
    //    builder.Append($"{fileName}_{segments + 1}.ts\n");
    //    builder.Append($"#EXT-X-ENDLIST");
    //    using (TextWriter writer = new StreamWriter(fileOutput))
    //    {
    //        writer.Write(builder.ToString());
    //        writer.Flush();
    //        writer.Dispose();
    //        writer.Close();
    //    }
    //    return new FileStream(fileOutput, FileMode.Open, FileAccess.Read);
    //}

    //public static FileStream GetTS(string file)
    //{
    //    string[] arguments = file.Split('_');
    //    string id = arguments[0];
    //    string user = arguments[1];
    //    string version = arguments[2];
    //    int segment = int.Parse(arguments[3]);
    //    string directoryOutput = Directory.CreateDirectory(Path.Combine(Paths.TempData, "streams", "hls", user, version)).FullName;
    //    string fileName = $"{id}_{user}_{version}";
    //    double startPosition = segment * 10;

    //    MediaBase media =

    //    string exe = Directory.GetFiles(Paths.FFMpeg, "ffmpeg*", SearchOption.AllDirectories)[0];
    //    StringBuilder builder = FFmpegArgumentBuilder(media.PATH, file, height: version.Height, video_bitrate: version.BitRate, start_duration: start_time);
    //    builder.Append($" \"{fileOutput}\"");
    //    Process process = new()
    //    {
    //        StartInfo = new()
    //        {
    //            FileName = exe,
    //            Arguments = builder.ToString(),
    //            UseShellExecute = false,
    //            WorkingDirectory = directoryOutput,
    //        },
    //        EnableRaisingEvents = true,
    //    };
    //}

    public static StringBuilder FFmpegArgumentBuilder(string input, string output, int start_duration = 0, int width = -2, int height = 720, string video_codec = "libx264", string audio_codec = "aac", int video_bitrate = 4500, int audio_bitrate = 380, bool UseHardwareAccel = false, EncoderPreset preset = EncoderPreset.ultrafast)
    {
        StringBuilder builder = new();
        if (UseHardwareAccel)
            builder.Append("-hwaccel auto ");
        builder.Append($"-ss {start_duration} -i \"{input}\" -loglevel quiet -vf scale={width}:{height} -c:v {video_codec} -c:a {audio_codec} -b:v {video_bitrate}K -b:a {audio_bitrate}K -preset {preset} -movflags +faststart");
        return builder;
    }
}