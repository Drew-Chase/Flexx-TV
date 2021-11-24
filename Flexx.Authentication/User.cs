﻿using ChaseLabs.CLConfiguration.List;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Flexx.Core.Data.Global;

namespace Flexx.Authentication;
public class Users
{
    public static Users Instance = Instance ?? new Users();
    private readonly List<User> users;

    private Users()
    {
        Instance = this;
        users = new();
        LoadExisting();
    }

    public User Add(string username)
    {
        return Add(new User(username));
    }

    public User Add(User user)
    {
        users.Add(user);
        return user;
    }

    public User Get(string username)
    {
        User value = null;
        Parallel.ForEach(users, user =>
        {
            if (user.Username.Equals(username))
            {
                value = user;
            }
        });
        return value ?? Add(username);
    }

    private void LoadExisting()
    {
        log.Info("Loading UserData");
        string[] files = Directory.GetFiles(Paths.UserData, "*.userdata", SearchOption.AllDirectories);
        Parallel.ForEach(files, file =>
        {
            Add(new FileInfo(file).Name.Replace(".userdata", ""));
        });
    }
}

public class User
{
    private readonly Dictionary<string, ushort> WatchedDuration;
    private readonly Dictionary<string, bool> HasWatched;
    private readonly ConfigManager userProfile;
    private readonly Dictionary<string, System.Diagnostics.Process> activeStreams;
    public string Username { get; }
    public bool IsAuthorized { get; private set; }
    public Notifications Notifications { get; }

    internal User(string username)
    {
        Username = username;
#if DEBUG
        userProfile = new(Path.Combine(Directory.CreateDirectory(Path.Combine(Paths.UserData, username)).FullName, $"{username}.userdata"), false, "FlexxTV");
#else
            userProfile = new(Path.Combine(Directory.CreateDirectory(Path.Combine(Paths.UserData, username)).FullName, $"{username}.userdata"), true, "FlexxTV");
#endif
        activeStreams = new();
        HasWatched = new();
        WatchedDuration = new();
        Notifications = new(this);
        UpdateDictionaries();
        IsAuthorized = CheckIfAuthorized();
    }

    private bool CheckIfAuthorized()
    {
        return false;
    }

    private void UpdateDictionaries()
    {
        WatchedDuration.Clear();
        HasWatched.Clear();
        foreach (ChaseLabs.CLConfiguration.Object.Config config in userProfile.List())
        {
            if (config.Key.EndsWith("-watched_duration"))
            {
                if (config.Value is ushort)
                {
                    WatchedDuration.Add(config.Key, config.Value);
                }
            }
            else if (config.Key.EndsWith("-watched"))
            {
                if (config.Value is bool)
                {
                    HasWatched.Add(config.Key, config.Value);
                }
            }
        }
    }

    public void SetHasWatched(string title, bool watched)
    {
        string key = $"{title}-watched";
        ChaseLabs.CLConfiguration.Object.Config cfg = userProfile.GetConfigByKey(key);
        if (cfg == null)
        {
            userProfile.Add(key, watched);
        }
        else
        {
            cfg.Value = watched;
        }

        if (HasWatched.ContainsKey(key))
        {
            HasWatched[key] = watched;
        }
        else
        {
            HasWatched.Add(key, watched);
        }
    }

    public bool GetHasWatched(string title)
    {
        string key = $"{title}-watched";
        if (HasWatched.TryGetValue(key, out bool watched))
        {
            return watched;
        }
        else
        {
            ChaseLabs.CLConfiguration.Object.Config cfg = userProfile.GetConfigByKey(key);
            if (cfg == null)
            {
                userProfile.Add(key, false);
                return false;
            }
            HasWatched.Add(key, cfg.Value);
            return cfg.Value;
        }
    }

    public void SetWatchedDuration(string title, ushort duration)
    {
        try
        {

            string key = $"{title}-watched_duration";
            ChaseLabs.CLConfiguration.Object.Config cfg = userProfile.GetConfigByKey(key);
            if (cfg == null)
            {
                userProfile.Add(key, duration);
                cfg = userProfile.GetConfigByKey(key);
            }
            cfg.Value = duration;

            if (WatchedDuration.ContainsKey(key))
            {
                WatchedDuration[key] = duration;
            }
            else
            {
                WatchedDuration.Add(key, duration);
            }
        }
        catch (Exception e) { log.Error(e); }
    }

    public ushort GetWatchedDuration(string title)
    {
        string key = $"{title}-watched_duration";
        if (WatchedDuration.TryGetValue(key, out ushort duration))
        {
            return duration;
        }
        else
        {
            ChaseLabs.CLConfiguration.Object.Config cfg = userProfile.GetConfigByKey(key);
            if (cfg == null)
            {
                userProfile.Add(key, (ushort)0);
                return 0;
            }
            WatchedDuration.Add(key, cfg.Value);
            return cfg.Value;
        }
    }

    public override string ToString()
    {
        return Username;
    }

    public void AddActiveStream(string media, System.Diagnostics.Process process)
    {
        if (!activeStreams.ContainsKey(media))
        {
            activeStreams.Add(media, process);
        }
    }
    public void RemoveActiveStream(string media)
    {
        if (activeStreams.ContainsKey(media))
        {
            var process = activeStreams[media];
            if (process != null && !process.HasExited)
            {
                process.Kill();
                process.Dispose();
                activeStreams.Remove(media);
            }
        }
    }
}