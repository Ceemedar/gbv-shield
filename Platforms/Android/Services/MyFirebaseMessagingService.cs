using Android.App;
using Android.Util;
using Firebase.Messaging;
using GBV_Shield.Services;

namespace GBV_Shield.Platforms.Android.Services
{
    [Service(
        Name = "com.gbvshield.app.MyFirebaseMessagingService", // 👈 important: matches AndroidManifest
        Exported = false
    )]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            Log.Debug("FCM", $"Message received: {message.Data}");

            var title = message.GetNotification()?.Title ?? "GBV Shield";
            var body = message.GetNotification()?.Body ?? "New alert";

            // Forward into NotificationService (from DI container)
            var notificationService = MauiProgram.CurrentApp
                                                 .Services
                                                 .GetService(typeof(NotificationService)) as NotificationService;

            notificationService?.HandleIncomingNotification(title, body);
        }

        [System.Obsolete("Required override for the Firebase messaging callback.")]
        public override void OnNewToken(string token)
        {
            Log.Debug("FCM", "A new device token was received.");

            var notificationService = MauiProgram.CurrentApp
                                                 .Services
                                                 .GetService(typeof(NotificationService)) as NotificationService;

            if (notificationService != null)
            {
                _ = notificationService.SaveToken(token); // async fire-and-forget
            }
        }
    }
}
