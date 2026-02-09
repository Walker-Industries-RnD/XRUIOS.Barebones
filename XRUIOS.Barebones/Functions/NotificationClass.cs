using Pariah_Cybersecurity;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones.Functions
{
    public static class NotificationClass
    {
        public struct NotificationContent
        {
            public string Action;
            public Dictionary<string, string> Args;
            public List<string> Texts;
            public FileRecord? InlineImage;
            public FileRecord? Logo;
            public List<Button> Buttons;
            public string? Tag;
            public string? Group;
            public DateTime? ExpirationTime;

            public NotificationContent() { }

            public NotificationContent(
                string action,
                Dictionary<string, string>? args = null,
                List<string>? texts = null,
                FileRecord? inlineImage = null,
                FileRecord? logo = null,
                List<Button>? buttons = null,
                string? tag = null,
                string? group = null,
                DateTime? expirationTime = null)
            {
                Action = action;
                Args = args ?? new Dictionary<string, string>();
                Texts = texts ?? new List<string>();
                InlineImage = inlineImage;
                Logo = logo;
                Buttons = buttons ?? new List<Button>();
                Tag = tag;
                Group = group;
                ExpirationTime = expirationTime;
            }

            public struct Button
            {
                public string Content;
                public string Action;
                public Dictionary<string, string> Args;
                public bool BackgroundActivation;

                public Button() { }

                public Button(string content, string action, Dictionary<string, string>? args = null, bool backgroundActivation = true)
                {
                    Content = content;
                    Action = action;
                    Args = args ?? new Dictionary<string, string>();
                    BackgroundActivation = backgroundActivation;
                }
            }
        }


        public static event Action<List<NotificationContent>>? OnNotificationsUpdated;

        //LLet's try something a little different
        private static async Task<List<NotificationContent>> LoadAllAsync()
        {
            var historyFile = await DataHandler.JSONDataHandler.LoadJsonFile("NotificationHistory", DataPath);
            var notifications = (List<NotificationContent>?)await DataHandler.JSONDataHandler.GetVariable<List<NotificationContent>>(historyFile, "Data", encryptionKey);
            return notifications ?? new List<NotificationContent>();
        }

        private static async Task SaveAllAsync(List<NotificationContent> notifications)
        {
            var historyFile = await DataHandler.JSONDataHandler.LoadJsonFile("NotificationHistory", DataPath);
            var editedJson = await DataHandler.JSONDataHandler.UpdateJson<List<NotificationContent>>(historyFile, "Data", notifications, encryptionKey);
            await DataHandler.JSONDataHandler.SaveJson(editedJson);

            OnNotificationsUpdated?.Invoke(notifications);
        }

        // C
        public static async Task AddNotification(NotificationContent notification)
        {
            var notifications = await LoadAllAsync();

            if (notifications.Any(n => n.Tag == notification.Tag && n.Group == notification.Group))
                throw new InvalidOperationException($"Notification with Tag '{notification.Tag}' already exists in Group '{notification.Group}'.");

            notifications.Add(notification);
            await SaveAllAsync(notifications);
        }

        // R
        public static async Task<List<NotificationContent>> GetNotifications(bool includeExpired = false)
        {
            var notifications = await LoadAllAsync();
            if (!includeExpired)
                notifications = notifications.Where(n => n.ExpirationTime == null || n.ExpirationTime > DateTime.Now).ToList();
            return notifications;
        }

        // D
        public static async Task RemoveNotification(string tag, string group)
        {
            var notifications = await LoadAllAsync();
            notifications = notifications.Where(n => !(n.Tag == tag && n.Group == group)).ToList();
            await SaveAllAsync(notifications);
        }

        public static async Task ClearAllNotifications()
        {
            await SaveAllAsync(new List<NotificationContent>());
        }
    }
}
