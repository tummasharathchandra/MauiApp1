using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1.Services
{
    /// <summary>
    /// Background location service responsible for querying device GPS hardware,
    /// persisting captured coordinates to SQLite, and broadcasting updates to the UI thread.
    /// </summary>
    public class LocationTrackerService
    {
        // Reference to SQLite database service for persistence
        private readonly DatabaseService _databaseService;

        // Token source used to manage and cancel the background tracking loop
        private CancellationTokenSource _cts;

        /// <summary>
        /// Gets a value indicating whether background GPS polling is actively running.
        /// </summary>
        public bool IsTracking { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationTrackerService"/> class.
        /// </summary>
        /// <param name="databaseService">Injected SQLite database service instance.</param>
        public LocationTrackerService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Requests location permissions and begins continuous background GPS location polling.
        /// </summary>
        /// <param name="onLocationUpdated">Callback invoked on the main UI thread when a new GPS coordinate is logged.</param>
        public async Task StartTrackingAsync(Action<LocationPoint> onLocationUpdated)
        {
            // Verify and request device runtime location permissions
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return;

            IsTracking = true;
            _cts = new CancellationTokenSource();

            // Run location polling task asynchronously off the UI thread
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Configure high-accuracy GPS polling request
                        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                        var location = await Geolocation.Default.GetLocationAsync(request, _cts.Token);

                        if (location != null)
                        {
                            // Persist coordinate directly to SQLite database
                            await _databaseService.SaveLocationAsync(location.Latitude, location.Longitude);

                            var point = new LocationPoint
                            {
                                Latitude = location.Latitude,
                                Longitude = location.Longitude,
                                Timestamp = DateTime.UtcNow
                            };

                            // Marshal execution back to the main UI thread to update map overlays safely
                            MainThread.BeginInvokeOnMainThread(() => onLocationUpdated?.Invoke(point));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GPS Tracking Error: {ex.Message}");
                    }

                    // Poll location at regular 10-second intervals
                    await Task.Delay(10000, _cts.Token);
                }
            }, _cts.Token);
        }

        /// <summary>
        /// Cancels background polling task and stops GPS tracking service.
        /// </summary>
        public void StopTracking()
        {
            IsTracking = false;
            _cts?.Cancel();
        }
    }
}