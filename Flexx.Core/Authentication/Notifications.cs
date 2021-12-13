using ChaseLabs.CLConfiguration.List;
using ChaseLabs.CLConfiguration.Object;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Flexx.Data.Global;

namespace Flexx.Authentication
{
    public enum NotificationType
    {
        Server,

        Account,

        User,

        Billing,

        Promotional,
    }

    public class NotificationModel
    {
        #region Public Constructors

        public NotificationModel(NotificationType type, User user, string title, string message, DateTime added, bool @new)
        {
            Type = type;
            User = user;
            Title = title;
            Message = message;
            Added = added;
            New = @new;
        }

        #endregion Public Constructors

        #region Public Properties

        public DateTime Added { get; }

        public string Message { get; }

        public bool New { get; set; }

        public string Title { get; }

        public NotificationType Type { get; }

        public User User { get; }

        #endregion Public Properties

        #region Public Methods

        public string GetAddedDisplay()
        {
            TimeSpan span = DateTime.Now.Subtract(Added);
            if (span.TotalSeconds < 60)
            {
                return $"{Math.Floor(span.TotalSeconds)} seconds ago";
            }
            else if (span.TotalMinutes < 60)
            {
                return $"{Math.Floor(span.TotalMinutes)} minutes ago";
            }
            else if (span.TotalHours < 24)
            {
                return $"{Math.Floor(span.TotalHours)} hours ago";
            }
            else if (span.TotalDays < 7)
            {
                return $"{Math.Floor(span.TotalDays)} days ago";
            }
            else
            {
                return Added.ToString("MM-dd-yyyy");
            }
        }

        #endregion Public Methods
    }

    public class Notifications
    {
        #region Private Fields

        private readonly ConfigManager notificationManager;

        private readonly User User;

        private List<NotificationModel> notifications;

        #endregion Private Fields

        #region Public Constructors

        public Notifications(User user)
        {
            notificationManager = new(Path.Combine(Directory.CreateDirectory(Path.Combine(Paths.UserData, user.Username)).FullName, "notifications"));
            notifications = new();
            User = user;
            LoadFromCache();
        }

        #endregion Public Constructors

        #region Public Methods

        public NotificationModel[] Get()
        {
            return notifications.ToArray();
        }

        public object[] GetObject()
        {
            List<object> json = new();

            foreach (NotificationModel n in notifications)
            {
                json.Add(new
                {
                    type = n.Type,
                    user = n.User.ToString(),
                    title = n.Title,
                    message = n.Message,
                    added = n.GetAddedDisplay(),
                    @new = n.New
                });
            }

            return json.ToArray();
        }

        public void MarkAsRead(string title)
        {
            notifications.Where(n => n.Title.Equals(title)).ToArray()[0].New = true;
        }

        public void Pop(NotificationModel notification)
        {
            notifications.Remove(notification);
            notificationManager.Remove($"{notification.Type}**-**{notification.Title}**-**{notification.Added:MM-dd-yyyy-HH:mm:ss}**-**{notification.New}");
            notifications = notifications.OrderBy(n => n.Added).ToList();
        }

        public void Push(NotificationModel notification)
        {
            notifications.Add(notification);
            notificationManager.Add($"{notification.Type}**-**{notification.Title}**-**{notification.Added:MM-dd-yyyy-HH:mm:ss}**-**{notification.New}", notification.Message);
            notifications = notifications.OrderBy(n => n.Added).ToList();
        }

        #endregion Public Methods

        #region Private Methods

        private void LoadFromCache()
        {
            foreach (Config config in notificationManager.List())
            {
                if (Enum.TryParse(typeof(NotificationType), config.Key.Split("**-**")[0].Trim().Replace("**-**", ""), out object type))
                {
                    notifications.Add(new NotificationModel((NotificationType)type, User, config.Key.Split("**-**")[1].Trim().Replace("**-**", ""), config.Value, DateTime.TryParse(config.Key.Split("**-**")[2].Trim().Replace("**-**", ""), out DateTime added) ? added : DateTime.Now, !bool.TryParse(config.Key.Split("**-**")[3].Trim().Replace("**-**", ""), out bool @new) || @new));
                }
                else
                {
                    notificationManager.Remove(config);
                }
            }
            notifications = notifications.OrderBy(n => n.Added).ToList();
        }

        #endregion Private Methods
    }
}