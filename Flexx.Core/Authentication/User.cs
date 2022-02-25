using ChaseLabs.CLConfiguration.List;
using Flexx.Media.Objects;
using Flexx.Media.Objects.Libraries;
using Flexx.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Flexx.Data.Global;

namespace Flexx.Authentication;

/// <summary>
/// All available Flexx Plans
/// </summary>
public enum PlanTier
{
    Free,

    Rust,

    Crimson,

    Hotrod
}

/// <summary>
/// This class contains all users currently loaded into memory
/// </summary>
public class Users
{
    #region Public Fields

    /// <summary>
    /// The singleton pattern used to insure that there is only ever one instance of this class
    /// </summary>
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
        if (users.Find(u => u.Username.Equals("guest")) == null)
            Add("guest");
    }

    #endregion Private Constructors

    #region Public Properties

    public bool HasHostUser => users.Find(u => u.IsHost) != null;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Initializes and Adds user with Username
    /// </summary>
    /// <param name="username"> </param>
    /// <returns> </returns>
    public User Add(string username)
    {
        return Add(new User(username));
    }

    /// <summary>
    /// Adds already initialized user
    /// </summary>
    /// <param name="user"> </param>
    /// <returns> </returns>
    public User Add(User user)
    {
        users.Add(user);
        return user;
    }

    /// <summary>
    /// Searches for user by delimiter. If none is found, returns the <see cref="GetGuestUser">
    /// Guest User </see>
    /// </summary>
    /// <param name="delimiter"> Username or Users Token </param>
    /// <returns> </returns>
    public User Get(string delimiter)
    {
        User value = null;
        if (string.IsNullOrWhiteSpace(delimiter))
            return GetGuestUser();
        Parallel.ForEach(users, user =>
        {
            try
            {
                if (user.Username.Equals(delimiter) || user.Token.Equals(delimiter) || user.Email.Equals(delimiter) || user.FirstName.Equals(delimiter))
                {
                    value = user;
                }
            }
            catch (NullReferenceException e)
            {
                log.Error($"Search has returned with a null reference while searching for {delimiter}", e);
            }
            catch (Exception e)
            {
                log.Error($"Unexpected Error has occurred while searching for user: {delimiter}", e);
            }
        });
        return value ?? GetGuestUser();
    }

    /// <summary>
    /// Gets the guest user
    /// </summary>
    /// <returns> </returns>
    public User GetGuestUser()
    {
        return Get("guest");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Loads Already saved users form the <see cref="Paths.UserData"> User Data </see> Directory
    /// </summary>
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
        Plan = PlanTier.Free;
        Username = username;
        Email = "";
        FirstName = "";
        LastName = "";

        userProfile = new(Path.Combine(Directory.CreateDirectory(Path.Combine(Paths.UserData, username)).FullName, $"{username}.userdata"));
        HasWatched = new();
        WatchedDuration = new();
        ContinueWatching = new();
        Notifications = new(this);
        UpdateDictionaries();

        userProfile.Add("isHost", false);
        userProfile.Add("token", "");
        userProfile.Add("username", "");
        userProfile.Add("email", "");
        userProfile.Add("first name", "");
        userProfile.Add("last name", "");
        Token = userProfile.GetConfigByKey("token").Value;
        IsLocal = string.IsNullOrWhiteSpace(Token);
        LoginWithToken();

        if (IsHost && !Remote.IsServerRegistered(this))
        {
            Remote.RegisterServer(this);
        }
    }

    #endregion Internal Constructors

    #region Public Properties

    public string Email { get; private set; }

    public string FirstName { get; private set; }

    /// <summary>
    /// Checks if config value is host is true or not and returns the value
    /// </summary>
    public bool IsHost { get => userProfile.GetConfigByKey("isHost").Value; set => userProfile.GetConfigByKey("isHost").Value = value; }

    public bool IsLocal { get; private set; }

    public string LastName { get; private set; }

    public Notifications Notifications { get; }

    /// <summary>
    /// The users current plan. <br/> Value is populated from <see cref="LoginWithToken"> Remove
    /// Server </see>
    /// </summary>
    public PlanTier Plan { get; private set; }

    /// <summary>
    /// Users token is used for authentication
    /// </summary>
    public string Token { get; private set; }

    public string Username { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Retrieves a list of all movies or episodes that have been started but not finished.
    /// </summary>
    /// <returns> </returns>
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

    /// <summary>
    /// Generates a token from users stored username and given password
    /// </summary>
    /// <param name="password"> </param>
    /// <returns> </returns>
    public object GenerateToken(string password)
    {
        if (!string.IsNullOrEmpty(password))
        {
            HttpResponseMessage response = new HttpClient().PostAsync($"{Paths.FlexxAuthURL}/login.php", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("email", Username),
                new KeyValuePair<string,string>("password", password),
            })).Result;
            if (response.IsSuccessStatusCode)
            {
                string content = response.Content.ReadAsStringAsync().Result;
                JObject json = (JObject) JsonConvert.DeserializeObject(content);
                if (json.ContainsKey("token"))
                {
                    try
                    {
                        Token = (string) json["token"];
                        FirstName = (string) json["fname"];
                        LastName = (string) json["lname"];
                        Email = (string) json["email"];
                        Plan = Enum.TryParse(typeof(PlanTier), (string) json["plan"], out object planTier) ? (PlanTier) planTier : PlanTier.Free;
                        userProfile.GetConfigByKey("token").Value = Token;
                        IsLocal = false;
                        return new
                        {
                            Token,
                        };
                    }
                    catch
                    {
                    }
                }
                else if (json.ContainsKey("error"))
                {
                    return new
                    {
                        error = (string) json["error"]
                    };
                }
            }
        }
        return new
        {
            error = "Unable to connect to authentication server"
        };
    }

    /// <summary>
    /// Gets rather a specified <see cref="MovieModel"> Movie </see> or <see cref="EpisodeModel">
    /// Episode </see> has been watched or not
    /// </summary>
    /// <param name="media">
    /// <see cref="MovieModel"> Movie </see> or <see cref="EpisodeModel"> Episode </see>
    /// </param>
    /// <returns> </returns>
    public bool GetHasWatched(MediaBase media)
    {
        string key = "";
        if (media.GetType().Equals(typeof(MovieModel)))
        {
            key = $"m_{media.TMDB}";
        }
        else if (media.GetType().Equals(typeof(EpisodeModel)))
        {
            EpisodeModel episode = (EpisodeModel) media;
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

    /// <summary>
    /// Gets the time in seconds that a <see cref="MovieModel"> Movie </see> or <see
    /// cref="EpisodeModel"> Episode </see> was watched until
    /// </summary>
    /// <param name="media">
    /// <see cref="MovieModel"> Movie </see> or <see cref="EpisodeModel"> Episode </see>
    /// </param>
    /// <returns> </returns>
    public ushort GetWatchedDuration(MediaBase media)
    {
        string key = "";
        if (media.GetType().Equals(typeof(MovieModel)))
        {
            key = $"m_{media.TMDB}";
        }
        else if (media.GetType().Equals(typeof(EpisodeModel)))
        {
            EpisodeModel episode = (EpisodeModel) media;
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
                userProfile.Add(key, (ushort) 0);
                return 0;
            }
            WatchedDuration.Add(key, cfg.Value);
            return cfg.Value;
        }
    }

    /// <summary>
    /// Checks if a user has specified plan or higher
    /// </summary>
    /// <param name="plan"> Plan required </param>
    /// <returns> </returns>
    public bool IsAuthorized(PlanTier plan = PlanTier.Free) => !string.IsNullOrWhiteSpace(Token) && plan >= Plan;

    /// <summary>
    /// Marks <see cref="MovieModel"> Movie </see> or <see cref="EpisodeModel"> Episode </see> as
    /// watched or not.
    /// </summary>
    /// <param name="media">  
    /// <see cref="MovieModel"> Movie </see> or <see cref="EpisodeModel"> Episode </see>
    /// </param>
    /// <param name="watched"> </param>
    public void SetHasWatched(MediaBase media, bool watched)
    {
        string key = "";
        if (media.GetType().Equals(typeof(MovieModel)))
        {
            key = $"m_{media.TMDB}";
        }
        else if (media.GetType().Equals(typeof(EpisodeModel)))
        {
            EpisodeModel episode = (EpisodeModel) media;
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

    /// <summary>
    /// Sets the time in seconds that a <see cref="MovieModel"> Movie </see> or <see
    /// cref="EpisodeModel"> Episode </see> was watched until
    /// </summary>
    /// <param name="media">    </param>
    /// <param name="duration"> </param>
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
                EpisodeModel episode = (EpisodeModel) media;
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

    /// <summary>
    /// Attempts to login with stored token.
    /// </summary>
    private void LoginWithToken()
    {
        if (!string.IsNullOrEmpty(Token))
        {
            HttpResponseMessage response = new HttpClient().PostAsync($"{Paths.FlexxAuthURL}/login.php", new FormUrlEncodedContent(new[]
                            {
                                new KeyValuePair<string, string>("token", Token),
                            })).Result;
            log.Debug(response);
            if (response.IsSuccessStatusCode)
            {
                JObject json = (JObject) JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                if (json.ContainsKey("token"))
                {
                    try
                    {
                        Token = (string) json["token"];
                        FirstName = (string) json["fname"];
                        LastName = (string) json["lname"];
                        Email = (string) json["email"];
                        Username = json["username"] != null ? (string) json["username"] : string.IsNullOrWhiteSpace(FirstName) ? Email : FirstName;
                        Plan = Enum.TryParse(typeof(PlanTier), (string) json["plan"], out object planTier) ? (PlanTier) planTier : PlanTier.Free;
                        IsLocal = false;
                        userProfile.GetConfigByKey("email").Value = Email;
                        userProfile.GetConfigByKey("username").Value = Username;
                        userProfile.GetConfigByKey("first name").Value = FirstName;
                        userProfile.GetConfigByKey("last name").Value = LastName;
                    }
                    catch
                    {
                    }
                }
            }
        }
    }

    /// <summary>
    /// Loads <see cref="GetWatchedDuration"> Watch Duration </see> and <see cref="GetHasWatched">
    /// Has Watched </see> values from metadata.
    /// </summary>
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