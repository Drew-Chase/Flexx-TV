using Flexx.Authentication;
using static Flexx.Core.Data.Global;
using Flexx.Media.Objects.Extras;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flexx.Media.Utilities
{
    public class ActiveStreams
    {
        private readonly List<MediaStream> streams;
        private static ActiveStreams _instance;
        public static ActiveStreams Instance => _instance ??= new ActiveStreams();

        private ActiveStreams()
        {
            streams = new();
        }

        public MediaStream AddStream(MediaStream stream)
        {
            streams.Add(stream);
            return stream;
        }
        public MediaStream Get(string working_directory)
        {
            foreach (MediaStream stream in streams)
            {
                if (stream.WorkingDirectory == working_directory)
                    return stream;
            }
            return null;
        }
        public MediaStream[] Get(User user)
        {
            List<MediaStream> value = new();
            if (user != null && value.Any())
            {
                foreach (MediaStream stream in streams)
                {
                    if (stream.User == user)
                    {
                        value.Add(stream);
                    }
                }
            }

            return value.ToArray();
        }
        public MediaStream[] Get(User user, MediaVersion version)
        {
            List<MediaStream> value = new();
            if (user != null && value.Any())
            {
                foreach (MediaStream stream in streams)
                {
                    if (stream.User == user && stream.Version == version)
                    {
                        value.Add(stream);
                    }
                }
            }

            return value.ToArray();
        }
        public MediaStream Get(User user, MediaVersion version, long startTime)
        {
            if (user != null)
            {
                foreach (MediaStream stream in streams)
                {
                    if (stream.User == user && stream.Version == version && stream.StartTime == startTime)
                    {
                        return stream;
                    }
                }
            }
            return null;

        }
        internal void RemoveStream(MediaStream stream)
        {
            streams.Remove(stream);
        }
    }
    public class MediaStream
    {
        public User User { get; init; }
        public Process Process { get; init; }
        public MediaVersion Version { get; init; }
        public string WorkingDirectory { get; init; }
        public long StartTime { get; init; }
        public FileStream FileStream { get; set; }
        public int StreamTimeout { get; init; }
        private System.Timers.Timer _timer;
        public MediaStream(User user, Process process, MediaVersion version, string working_directory, long start_time, FileStream fileStream, int timeout = 15000)
        {
            User = user;
            Process = process;
            Version = version;
            WorkingDirectory = working_directory;
            StartTime = start_time;
            FileStream = fileStream;
            StreamTimeout = timeout;
            ResetTimeout();
        }
        public void ResetTimeout()
        {
            if (_timer != null)
                _timer.Stop();
            _timer = new(StreamTimeout)
            {
                Enabled = true,
            };
            _timer.Elapsed += (s, e) =>
            {
                KillAsync();
            };
            _timer.Start();
        }
        public Task KillAsync() =>
             Task.Run(() =>
            {
                if (_timer != null)
                    _timer.Stop();
                if (Process != null && !Process.HasExited)
                {
                    Process.Kill();
                    Process.Dispose();
                    Process.Close();
                }
                Thread.Sleep(5000);
                try
                {
                    if (FileStream != null)
                        FileStream.Dispose();
                }
                catch (Exception e)
                {
                    log.Error($"Failed to dispose of FileStream", e);
                }
                Thread.Sleep(500);

                try
                {

                    Directory.Delete(WorkingDirectory, true);
                }
                catch
                {
                    Thread.Sleep(1000);
                    try
                    {

                        Directory.Delete(WorkingDirectory, true);
                    }
                    catch
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            Directory.Delete(WorkingDirectory, true);
                        }
                        catch
                        {
                            log.Error($"Unable to delete working directory! (\"{WorkingDirectory}\")");
                        }
                    }

                }
                ActiveStreams.Instance.RemoveStream(this);
            });

    }
}
