
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
    /// <summary>
    /// Manages application state and connects the user interface to
    /// the location tracking service and SQLite database service.
    /// Implements INotifyPropertyChanged so the UI can respond to
    /// changes in tracking status and the collection of location points.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // Handles saving, retrieving, and deleting GPS records in SQLite.
        private readonly DatabaseService _databaseService;

        // Handles location permission requests and GPS polling.
        private readonly LocationTrackerService _trackerService;

        // Indicates whether location tracking is currently active.
        private bool _isTracking;

        // Stores the location history displayed by the map.
        private ObservableCollection<LocationPoint> _heatMapPoints = new();

        /// <summary>
        /// Gets or sets the current tracking status.
        /// Property-change notifications update the button text when tracking
        /// starts or stops.
        /// </summary>
        public bool IsTracking
        {
            get => _isTracking;

            set
            {
                // Avoid unnecessary UI notifications if the value is unchanged.
                if (_isTracking == value)
                    return;

                _isTracking = value;

                OnPropertyChanged();

                // Notify the UI that the Start/Stop button text must change.
                OnPropertyChanged(nameof(TrackingButtonText));
            }
        }

        /// <summary>
        /// Provides the text displayed on the tracking button.
        /// Displays "Start" when tracking is stopped and "Stop" when active.
        /// </summary>
        public string TrackingButtonText =>
            IsTracking ? "Stop" : "Start";

        /// <summary>
        /// Contains the recorded GPS points used to render map overlays.
        /// ObservableCollection notifies the page when points are added
        /// or removed during tracking and clearing operations.
        /// </summary>
        public ObservableCollection<LocationPoint> HeatMapPoints
        {
            get => _heatMapPoints;

            set
            {
                if (ReferenceEquals(_heatMapPoints, value))
                    return;

                _heatMapPoints = value
                    ?? new ObservableCollection<LocationPoint>();

                // Notify the page if the entire collection is replaced.
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Starts or stops location tracking.
        /// Bound to the Start/Stop button in MainPage.xaml.
        /// </summary>
        public ICommand ToggleTrackingCommand { get; }

        /// <summary>
        /// Reloads saved location records from SQLite.
        /// Bound to the Refresh Map button in MainPage.xaml.
        /// </summary>
        public ICommand RefreshHeatMapCommand { get; }

        /// <summary>
        /// Deletes the saved location history and clears the map collection.
        /// Bound to the Clear button in MainPage.xaml.
        /// </summary>
        public ICommand ClearDataCommand { get; }

        /// <summary>
        /// Initializes the database and tracking services and connects
        /// each user-interface command to its corresponding method.
        /// </summary>
        public MainViewModel()
        {
            // Create the SQLite persistence service.
            _databaseService = new DatabaseService();

            // Share the database service with the GPS tracking service.
            _trackerService = new LocationTrackerService(_databaseService);

            // Connect buttons to asynchronous command handlers.
            ToggleTrackingCommand =
                new Command(async () => await ToggleTrackingAsync());

            RefreshHeatMapCommand =
                new Command(async () => await LoadHeatMapPointsAsync());

            ClearDataCommand =
                new Command(async () => await ClearDataAsync());
        }

        /// <summary>
        /// Starts or stops GPS tracking.
        /// New locations are saved by LocationTrackerService and added
        /// to HeatMapPoints so the page can refresh its map overlays.
        /// </summary>
        private async Task ToggleTrackingAsync()
        {
            // If tracking is active, cancel the polling operation.
            if (IsTracking)
            {
                _trackerService.StopTracking();
                IsTracking = false;
                return;
            }

            try
            {
                // Request permission and start collecting location records.
                await _trackerService.StartTrackingAsync(newPoint =>
                {
                    // Ignore an unexpected empty callback.
                    if (newPoint == null)
                        return;

                    // The tracking service invokes this callback on the UI thread.
                    // Adding the point triggers the collection-change event
                    // observed by MainPage.xaml.cs.
                    HeatMapPoints.Add(newPoint);
                });

                // Reflect the actual state of the tracking service.
                // If permission was denied, the service remains stopped.
                IsTracking = _trackerService.IsTracking;
            }
            catch (Exception ex)
            {
                // Restore the button state if starting tracking fails.
                IsTracking = false;

                System.Diagnostics.Debug.WriteLine(
                    $"Unable to start location tracking: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves previously recorded locations from SQLite.
        /// Replaces the observable collection with the retrieved records.
        /// The property-change notification allows the page to observe
        /// the new collection and redraw its map overlays.
        /// </summary>
        public async Task LoadHeatMapPointsAsync()
        {
            var points = await _databaseService.GetLocationsAsync();

            HeatMapPoints = new ObservableCollection<LocationPoint>(points);
        }

        /// <summary>
        /// Deletes all persisted location records and clears the in-memory
        /// collection. The collection-change notification tells the map
        /// page to remove the displayed location overlays.
        /// </summary>
        private async Task ClearDataAsync()
        {
            // Remove location history from persistent SQLite storage first.
            await _databaseService.ClearLocationsAsync();

            // Clear the current collection so the map can update immediately.
            HeatMapPoints.Clear();
        }

        /// <summary>
        /// Raised when a ViewModel property changes.
        /// MainPage subscribes to this event to detect collection replacement
        /// and changes to properties used by the user interface.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// CallerMemberName supplies the property name automatically when
        /// this method is called from within a property setter.
        /// </summary>
        /// <param name="propertyName">
        /// Name of the property that changed.
        /// </param>
        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
