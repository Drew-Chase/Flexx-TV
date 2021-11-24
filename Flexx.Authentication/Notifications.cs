using ChaseLabs.CLConfiguration.List;
using ChaseLabs.CLConfiguration.Object;
using static Flexx.Core.Data.Global;

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

    public class Notifications
    {
        private List<NotificationModel> notifications;
        private readonly ConfigManager notificationManager;
        private readonly User User;

        public Notifications(User user)
        {
#if DEBUG
            notificationManager = new(Path.Combine(Directory.CreateDirectory(Path.Combine(Paths.UserData, user.Username)).FullName, "notifications"), false, "FlexxTV");
#else
            notificationManager = new(Path.Combine(Directory.CreateDirectory(Path.Combine(Paths.UserData, user.Username)).FullName, "notifications"), true, "FlexxTV");
#endif
            notifications = new();
            User = user;
            LoadFromCache();
        }

        public void Push(NotificationModel notification)
        {
            notifications.Add(notification);
            notificationManager.Add($"{notification.Type}**-**{notification.Title}**-**{notification.Added:MM-dd-yyyy-HH:mm:ss}**-**{notification.New}", notification.Message);
            notifications = notifications.OrderBy(n => n.Added).ToList();
        }

        public void Pop(NotificationModel notification)
        {
            notifications.Remove(notification);
            notificationManager.Remove($"{notification.Type}**-**{notification.Title}**-**{notification.Added:MM-dd-yyyy-HH:mm:ss}**-**{notification.New}");
            notifications = notifications.OrderBy(n => n.Added).ToList();
        }

        public void MarkAsRead(string title)
        {
            notifications.Where(n => n.Title.Equals(title)).ToArray()[0].New = true;
        }

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
                    type = n.Type.ToString(),
                    user = n.User.ToString(),
                    title = n.Title,
                    message = n.Message,
                    added = n.GetAddedDisplay(),
                    @new = n.New
                });
            }

            return json.ToArray();
        }

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
    }

    public class NotificationModel
    {
        public NotificationType Type { get; }
        public User User { get; }
        public string Title { get; }
        public string Message { get; }
        public DateTime Added { get; }
        public bool New { get; set; }

        public NotificationModel(NotificationType type, User user, string title, string message, DateTime added, bool @new)
        {
            Type = type;
            User = user;
            Title = title;
            Message = message;
            Added = added;
            New = @new;
        }

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
    }
}