using EclipseProject;
using XRUIOS.Barebones;
using Pariah_Cybersecurity;
using static XRUIOS.Barebones.Interfaces.NotificationClass;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones.Functions
{
    public class NotificationClass 
    {
         
        public static readonly NotificationClass Instance = new();
        private NotificationClass() { }

        public static event Action<List<NotificationContent>>? OnNotificationsUpdated;

        //LLet's try something a little different
        private static async Task<List<NotificationContent>> LoadAllAsync()
        {
            var directoryPath = Path.Combine(DataPath, "NotificationHistory");
            var historyFile = await DataHandler.JSONDataHandler.LoadJsonFile("NotificationHistory", directoryPath);
            var notifications = (List<NotificationContent>?)await DataHandler.JSONDataHandler.GetVariable<List<NotificationContent>>(historyFile, "Data", encryptionKey);
            return notifications ?? new List<NotificationContent>();
        }

        private static async Task SaveAllAsync(List<NotificationContent> notifications)
        {
            var directoryPath = Path.Combine(DataPath, "NotificationHistory");
            var historyFile = await DataHandler.JSONDataHandler.LoadJsonFile("NotificationHistory", directoryPath);
            var editedJson = await DataHandler.JSONDataHandler.UpdateJson<List<NotificationContent>>(historyFile, "Data", notifications, encryptionKey);
            await DataHandler.JSONDataHandler.SaveJson(editedJson);

            OnNotificationsUpdated?.Invoke(notifications);
        }

        // C
        [SeaOfDirac("NotificationClass.AddNotification", new[] { "notification" }, typeof(Task), typeof(NotificationContent))]
        public static async Task AddNotification(NotificationContent notification)
        {
            var notifications = await LoadAllAsync();

            if (notifications.Any(n => n.Tag == notification.Tag && n.Group == notification.Group))
                throw new InvalidOperationException($"Notification with Tag '{notification.Tag}' already exists in Group '{notification.Group}'.");

            notifications.Add(notification);
            await SaveAllAsync(notifications);
        }

        // R
        [SeaOfDirac("NotificationClass.GetNotifications", new[] { "includeExpired" }, typeof(Task<List<NotificationContent>>), typeof(bool))]
        public static async Task<List<NotificationContent>> GetNotifications(bool includeExpired = false)
        {
            var notifications = await LoadAllAsync();
            if (!includeExpired)
                notifications = notifications.Where(n => n.ExpirationTime == null || n.ExpirationTime > DateTime.Now).ToList();
            return notifications;
        }

        // D
        [SeaOfDirac("NotificationClass.RemoveNotification", new[] { "tag", "group" }, typeof(Task), typeof(string), typeof(string))]
        public static async Task RemoveNotification(string tag, string group)
        {
            var notifications = await LoadAllAsync();
            notifications = notifications.Where(n => !(n.Tag == tag && n.Group == group)).ToList();
            await SaveAllAsync(notifications);
        }

        [SeaOfDirac("NotificationClass.ClearAllNotifications", null, typeof(Task))]
        public static async Task ClearAllNotifications()
        {
            await SaveAllAsync(new List<NotificationContent>());
        }

    }
}
