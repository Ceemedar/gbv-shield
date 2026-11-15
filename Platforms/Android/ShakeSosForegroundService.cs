using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using GBV_Shield.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GBV_Shield.Platforms.Android;

[Service(
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeLocation)]
public sealed class ShakeSosForegroundService : Service
{
    private const int NotificationId = 7301;
    private const string ChannelId = "gbv_shield_shake_sos";
    private PowerManager.WakeLock? _wakeLock;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
        StartForeground(NotificationId, BuildNotification());

        var powerManager = (PowerManager?)GetSystemService(PowerService);
        _wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial, "GBVShield:ShakeSOS");
        _wakeLock?.Acquire();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (!ShakeDetectionService.IsEnabled)
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        ShakeDetectionService.Start();
        _ = App.Services.GetService<LiveLocationService>()?.ResumeActiveSessionAsync();
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        ShakeDetectionService.Stop();
        if (_wakeLock?.IsHeld == true)
            _wakeLock.Release();
        _wakeLock?.Dispose();
        _wakeLock = null;
        base.OnDestroy();
    }

    public override IBinder? OnBind(Intent? intent) => null;

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        var channel = new NotificationChannel(
            ChannelId,
            "Shake SOS protection",
            NotificationImportance.Low)
        {
            Description = "Keeps shake-to-SOS active while GBV Shield is in the background."
        };
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!);
        var pendingIntent = launchIntent == null
            ? null
            : PendingIntent.GetActivity(
                this,
                0,
                launchIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(this, ChannelId)
            : new Notification.Builder(this);

        return builder.SetContentTitle("GBV Shield protection active")
            .SetContentText("Shake-to-SOS is monitoring in the background")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetCategory(Notification.CategoryService)
            .SetContentIntent(pendingIntent)
            .Build();
    }
}
