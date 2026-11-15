using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Extensions.DependencyInjection;

#if ANDROID
using Android.OS;
using Android.Telephony;
using Android.Content.PM;
using AndroidX.Core.Content;
#endif

using MauiSmsMessage = Microsoft.Maui.ApplicationModel.Communication.SmsMessage;

namespace GBV_Shield.Services
{
    public static class SosService
    {
        private const string PrefHotline = "HotlineNumber";
        private const string PrefContacts = "EmergencyContacts";

        private static Page? MainPage => Application.Current?.Windows.FirstOrDefault()?.Page;

        public static async Task<bool> SendSosAsync(bool silent = false)
        {
            try
            {
                if (!await EnsurePermissionsAsync(silent))
                    return false;

                var contactsString = Preferences.Get(PrefContacts, string.Empty);
                var contacts = contactsString
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();

                if (contacts.Count == 0)
                {
                    if (MainPage != null)
                        await MainPage.DisplayAlertAsync("SOS Failed", "No emergency contacts found in Settings.", "OK");
                    return false;
                }

                string addressPart = "(Location unavailable)";
                string mapsLink = "";
                Location? currentLocation = null;

                try
                {
                    currentLocation = await Geolocation.Default.GetLastKnownLocationAsync()
                              ?? await Geolocation.Default.GetLocationAsync(
                                  new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

                    if (currentLocation != null)
                    {
                        addressPart = $"Lat: {currentLocation.Latitude:F5}, Lon: {currentLocation.Longitude:F5}";
                        mapsLink = $"https://www.google.com/maps/search/?api=1&query={currentLocation.Latitude},{currentLocation.Longitude}";
                    }
                }
                catch (Exception geEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[SOS] Geolocation failed: {geEx.Message}");
                }

                string liveTrackingLink = "";
                try
                {
                    var liveLocation = App.Services.GetService<LiveLocationService>();
                    if (liveLocation != null)
                        liveTrackingLink = await liveLocation.StartOrRefreshSessionAsync(currentLocation) ?? "";
                }
                catch (Exception liveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[SOS] Live-location start failed: {liveEx.Message}");
                }

                var message =
                    "🚨 SOS ALERT 🚨\n" +
                    "I’m in danger, please help me urgently!\n\n" +
                    $"📍 {addressPart}\n" +
                    (string.IsNullOrEmpty(mapsLink) ? "" : $"🌍 Current location: {mapsLink}\n") +
                    (string.IsNullOrEmpty(liveTrackingLink) ? "" : $"🔴 Live tracking (2 hours): {liveTrackingLink}");

                var dispatched = false;
                foreach (var contact in contacts)
                {
                    try
                    {
#if ANDROID
                        if (silent)
                        {
                            var context = Android.App.Application.Context;

                            if (context == null ||
                                ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.SendSms) != Permission.Granted)
                            {
                                if (MainPage != null)
                                    await MainPage.DisplayAlertAsync("Permission Needed", "SMS permission is required for automatic SOS.", "OK");
                                continue;
                            }

#pragma warning disable CA1422
                            var smsManager = SmsManager.Default;

                            if (smsManager != null)
                            {
                                var parts = smsManager.DivideMessage(message);
                                if (parts is { Count: > 1 })
                                    smsManager.SendMultipartTextMessage(contact, null, parts, null, null);
                                else
                                    smsManager.SendTextMessage(contact, null, message, null, null);
                                dispatched = true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[SOS] SmsManager null, using fallback");

                                var smsMessage = new MauiSmsMessage(message, contact);
                                await Microsoft.Maui.ApplicationModel.Communication.Sms.ComposeAsync(smsMessage);
                            }
#pragma warning restore CA1422
                            continue;
                        }
#endif
                        var smsMessageNormal = new MauiSmsMessage(message, contact);
                        await Microsoft.Maui.ApplicationModel.Communication.Sms.ComposeAsync(smsMessageNormal);
                        dispatched = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SOS] Failed sending to {contact}: {ex.Message}");
                    }
                }

                if (silent)
                {
                    try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(400)); } catch { }
                }
                else
                {
                    if (MainPage != null)
                        await MainPage.DisplayAlertAsync("SOS", "Your emergency message was prepared.", "OK");
                }
                return dispatched;
            }
            catch (Exception ex)
            {
                if (MainPage != null)
                    await MainPage.DisplayAlertAsync("Error", $"SOS failed: {ex.Message}", "OK");
                return false;
            }
        }

        private static async Task<bool> EnsurePermissionsAsync(bool silent)
        {
            var locStatus = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<
                Microsoft.Maui.ApplicationModel.Permissions.LocationWhenInUse>();

            if (locStatus != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
            {
                locStatus = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<
                    Microsoft.Maui.ApplicationModel.Permissions.LocationWhenInUse>();

                if (locStatus != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                {
                    if (MainPage != null)
                        await MainPage.DisplayAlertAsync("Permission Needed", "Location permission is required for SOS.", "OK");
                    return false;
                }
            }

#if ANDROID
            if (silent)
            {
                try
                {
                    var context = Android.App.Application.Context;

                    bool granted = context != null &&
                        ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.SendSms) == Permission.Granted;

                    if (!granted)
                    {
                        if (MainPage != null)
                        {
                            await MainPage.DisplayAlertAsync(
                                "SMS Permission Needed",
                                "Automatic SOS requires SMS permission. Please grant it in app settings.",
                                "OK");
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnsurePermissions] Android SMS check failed: {ex.Message}");
                    return false;
                }
            }
#endif
            return true;
        }

        public static async Task CallHotlineAsync()
        {
            try
            {
                var hotline = Preferences.Get(PrefHotline, string.Empty);
                if (string.IsNullOrWhiteSpace(hotline))
                {
                    if (MainPage != null)
                        await MainPage.DisplayAlertAsync("Hotline", "No hotline number is saved in settings.", "OK");
                    return;
                }

                var uri = new Uri($"tel:{hotline}");
                await Launcher.Default.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                if (MainPage != null)
                    await MainPage.DisplayAlertAsync("Error", $"Unable to call hotline: {ex.Message}", "OK");
            }
        }
    }
}
