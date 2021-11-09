using Flexx.Core.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using static Flexx.Core.Data.Global;

namespace Flexx.Server.Controllers
{
    [ApiController]
    [Route("/api/get/")]
    public class GetController : ControllerBase
    {
        [HttpGet("{username}/notifications")]
        public IActionResult GetUsersNotifications(string username)
        {
            Notifications Notifications = Users.Instance.Get(username).Notifications;

            return new JsonResult(new
            {
                @new = Notifications.Get().Where(n => n.New).ToList().Count,
                count = Notifications.Get().Length,
                notifications = Notifications.GetObject()
            });
        }

        [HttpGet("{username}/notifications/push")]
        public IActionResult PushNotification(string username, string type, string title, string message)
        {
            if (Enum.TryParse(typeof(NotificationType), type, out object typeObject))
            {
                User user = Users.Instance.Get(username);
                user.Notifications.Push(new((NotificationType)typeObject, user, title, message, DateTime.Now, true));
                return new OkResult();
            }
            return new BadRequestResult();
        }

        [HttpGet("{username}/notifications/mark-as-read")]
        public IActionResult MarkNotificationAsRead(string username, string title)
        {
            Users.Instance.Get(username).Notifications.MarkAsRead(title);
            return new OkResult();
        }
    }
}