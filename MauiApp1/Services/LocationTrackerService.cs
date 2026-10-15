using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    public class LocationTrackerService
    {
        private readonly DatabaseService _databaseService;
        private CancellationTokenSource _cts;

        public bool IsTracking { get; private set; }

        public LocationTrackerService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task StartTrackingAsync(Action<LocationPoint> onLocationUpdated)
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return;

            IsTracking = true;
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                        var location = await Geolocation.Default.GetLocationAsync(request, _cts.Token);

                        if (location != null)
                        {
                            await _databaseService.SaveLocationAsync(location.Latitude, location.Longitude);

                            var point = new LocationPoint
                            {
                                Latitude = location.Latitude,
                                Longitude = location.Longitude,
                                Timestamp = DateTime.UtcNow
                            };

                            MainThread.BeginInvokeOnMainThread(() => onLocationUpdated?.Invoke(point));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GPS Error: {ex.Message}");
                    }

                    await Task.Delay(10000, _cts.Token);
                }
            }, _cts.Token);
        }

        public void StopTracking()
        {
            IsTracking = false;
            _cts?.Cancel();
        }
    }
}
