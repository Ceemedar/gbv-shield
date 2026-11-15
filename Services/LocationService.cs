using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GBV_Shield.Services
{
    /// <summary>
    /// Polls device GPS and forwards coordinates to ReportsService.SubmitLocationAsync().
    /// </summary>
    public class LocationService
    {
        private readonly ReportsService _reports;
        private CancellationTokenSource? _cts;

        public LocationService(ReportsService reports) => _reports = reports;

        public Task StartTrackingAsync()
        {
            _cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!_cts!.IsCancellationRequested)
                {
                    try
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                        var location = await Geolocation.Default.GetLocationAsync(request, _cts.Token);

                        if (location != null)
                            await _reports.SubmitLocationAsync(location.Latitude, location.Longitude);
                        else
                            await _reports.SubmitLocationAsync(0, 0); // fallback
                    }
                    catch (FeatureNotSupportedException ex)
                    {
                        Console.WriteLine($"[LocationService] GPS not supported: {ex.Message}");
                        _cts.Cancel();
                    }
                    catch (PermissionException ex)
                    {
                        Console.WriteLine($"[LocationService] Permission denied: {ex.Message}");
                        _cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LocationService] Unexpected: {ex}");
                    }

                    try
                    {
                        await Task.Delay(5000, _cts.Token);
                    }
                    catch (TaskCanceledException) { /* normal on stop */ }
                }
            }, _cts.Token);

            return Task.CompletedTask;
        }

        public void StopTracking()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocationService] Stop error: {ex.Message}");
            }
        }
    }
}
