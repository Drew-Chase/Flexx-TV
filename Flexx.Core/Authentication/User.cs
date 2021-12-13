using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flexx.Data.Global;

namespace Flexx.Authentication;

public class Users
{
    #region Public Fields

    public static Users Instance = Instance ?? new Users();

    #endregion Public Fields

    #region Private Fields

    private readonly List<User> users;

    #endregion Private Fields

    #region Private Constructors

    private Users()
    {
        Instance = this;
        users = new();
        LoadExisting();
    }

    #endregion Private Constructors

    #region Public Methods

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
        if (string.IsNullOrWhiteSpace(username)) return GetGuestUser();
        Parallel.ForEach(users, user =>
        {
            if (user.Username.Equals(username))
            {
                value = user;
            }
        });
        return value ?? Add(username);
    }

    public User GetGuestUser()
    {
        return Get("guest");
    }

    #endregion Public Methods

    #region Private Methods

    private void LoadExisting()
    {
        log.Debug("Loading UserData");
        string[] files = Directory.GetFiles(Paths.UserData, "*.userdata", SearchOption.AllDirectories);
        Parallel.ForEach(files, file =>
        {
            Add(new FileInfo(file).Name.Replace(".userdata", ""));
        });
    }

    #endregion Private Methods
}

public class User
{
    #region Private Fields

    private readonly Dictionary<string, bool> HasWatched;

    private readonly ConfigManager userProfile;

    private readonly Dictionary<string, ushort> WatchedDuration;

    private Dictionary<string, DateTime> ContinueWatching;

    #endregion Private Fields

    #region Internal Constructors

    internal User(string username)
    {
        Username = username;
        userProfile = new(Path.Combine(Directory.CreateDirectory(Path.Combine(Paths.UserData, username)).FullName, $"{username}.userdata"), false);
        HasWatched = new();
        WatchedDuration = new();
        ContinueWatching = new();
        Notifications = new(this);
        UpdateDictionaries();
        IsAuthorized = CheckIfAuthorized();
    }

    #endregion Internal Constructors

    #region Public Properties

    public bool IsAuthorized { get; private set; }

    public Notifications Notifications { get; }

    public string Username { get; }

    #endregion Public Properties

    #region Public Methods

    public MediaBase[] ContinueWatchingList()
    {
        List<MediaBase> list = new();
        foreach (var (name, _) in ContinueWatching.OrderBy(key => key.Value))
        {
            try
            {
                string[] components = name.Split('_');
                string id = components[1];
                if (components[0].Equals("e"))
                {
                    // Episode
                    if (int.TryParse(components[2], out int season) && int.TryParse(components[3], out int episode))
                    {
                        var show = TvLibraryModel.Instance.GetShowByTMDB(id);
                        if (show != null)
                        {
                            var seasonModel = show.GetSeasonByNumber(season);
                            if (seasonModel != null)
                            {
                                var episodeModel = seasonModel.GetEpisodeByNumber(episode);
                                if (episodeModel != null)
                                {
                                    list.Add(episodeModel);
                                }
                            }
                        }
                    }
                }
                else if (components[0].Equals("m"))
                {
                    // Movie
                    MediaBase media = MovieLibraryModel.Instance.GetMovieByTMDB(id);
                    if (media != null)
                    {
                        list.Add(media);
                    }
                }
            }
            catch
            {
                continue;
            }
        }
        return list.ToArray();
    }

    public bool GetHasWatched(MediaBase media)
    {
        string key = "";
        if (media.GetType().Equals(typeof(MovieModel)))
        {
            key = $"m_{media.TMDB}";
        }
        else if (media.GetType().Equals(typeof(EpisodeModel)))
        {
            EpisodeModel episode = (EpisodeModel)media;
            key = $"e_{episode.TMDB}_{episode.Season.Season_Number}_{episode.Episode_Number}";
        }

        key += "-watched";
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

    public ushort GetWatchedDuration(MediaBase media)
    {
        string key = "";
        if (media.GetType().Equals(typeof(MovieModel)))
        {
            key = $"m_{media.TMDB}";
        }
        else if (media.GetType().Equals(typeof(EpisodeModel)))
        {
            EpisodeModel episode = (EpisodeModel)media;
            key = $"e_{episode.TMDB}_{episode.Season.Season_Number}_{episode.Episode_Number}";
        }

        key += "-watched_duration";
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

    public void RemoveFromContinueWatching(MediaBase media)
    {
        if (media != null)
        {
            string name = "";
            if (media.GetType().Equals(typeof(MovieModel)))
            {
                name = $"m_{media.TMDB}";
            }
            else if (media.GetType().Equals(typeof(EpisodeModel)))
            {
                EpisodeModel episode = (EpisodeModel)media;
                name = $"e_{episode.TMDB}_{episode.Season.Season_Number}_{episode.Episode_Number}";
            }
            if (!string.IsNullOrEmpty(name))
            {
                name += "-continue-watching";
                ContinueWatching.Remove(name);
                userProfile.Remove(name);
            }
        }
    }

    public void SetHasWatched(MediaBase media, bool watched)
    {
        string key = "";
        if (media.GetType().Equals(typeof(MovieModel)))
        {
            key = $"m_{media.TMDB}";
        }
        else if (media.GetType().Equals(typeof(EpisodeModel)))
        {
            EpisodeModel episode = (EpisodeModel)media;
            key = $"e_{episode.TMDB}_{episode.Season.Season_Number}_{episode.Episode_Number}";
        }

        key += "-watched";

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

    public void SetToContinueWatching(MediaBase media)
    {
        if (media != null)
        {
            string name = "";
            DateTime time = DateTime.Now;
            if (media.GetType().Equals(typeof(MovieModel)))
            {
                name = $"m_{media.TMDB}";
            }
            else if (media.GetType().Equals(typeof(EpisodeModel)))
            {
                EpisodeModel episode = (EpisodeModel)media;
                name = $"e_{episode.TMDB}_{episode.Season.Season_Number}_{episode.Episode_Number}";
            }
            if (!string.IsNullOrEmpty(name))
            {
                name += "-continue-watching";
                ContinueWatching.Add(name, time);
                userProfile.Add(name, time.ToString("MM/dd/yyyy-HH:mm:ss:ff"));
            }
        }
    }

    public void SetWatchedDuration(MediaBase media, ushort duration)
    {
        try
        {
            string key = "";
            if (media.GetType().Equals(typeof(MovieModel)))
            {
                key = $"m_{media.TMDB}";
            }
            else if (media.GetType().Equals(typeof(EpisodeModel)))
            {
                EpisodeModel episode = (EpisodeModel)media;
                key = $"e_{episode.TMDB}_{episode.Season.Season_Number}_{episode.Episode_Number}";
            }

            key += "-watched_duration";
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

    public override string ToString()
    {
        return Username;
    }

    #endregion Public Methods

    #region Private Methods

    private bool CheckIfAuthorized()
    {
        return false;
    }

    private void UpdateDictionaries()
    {
        WatchedDuration.Clear();
        HasWatched.Clear();
        ContinueWatching.Clear();
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
            else if (config.Key.EndsWith("-continue-watching"))
            {
                if (DateTime.TryParse(config.Value, out DateTime time))
                {
                    ContinueWatching.Add(config.Key, time);
                }
            }
        }
    }

    #endregion Private Methods
}