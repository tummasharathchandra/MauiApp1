using System;
using System.Collections.Generic;
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
    /// ViewModel responsible for managing application state, commands, and communication
    /// between the location tracking services, local SQLite database, and the user interface.
    /// Implements INotifyPropertyChanged to support two-way UI data binding in .NET MAUI.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // Service references for SQLite persistence and GPS location tracking
        private readonly DatabaseService _databaseService;
        private readonly LocationTrackerService _trackerService;

        private bool _isTracking;
        private ObservableCollection<LocationPoint> _heatMapPoints = new();

        /// <summary>
        /// Gets or sets a value indicating whether background location tracking is active.
        /// Triggers UI updates for dependent properties like TrackingButtonText.
        /// </summary>
        public bool IsTracking
        {
            get => _isTracking;
            set
            {
                _isTracking = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TrackingButtonText)); // Updates button label dynamically
            }
        }

        /// <summary>
        /// Gets the dynamic text displayed on the primary action button based on tracking state.
        /// </summary>
        public string TrackingButtonText => IsTracking ? "Stop" : "Start";

        /// <summary>
        /// Gets or sets the collection of recorded location points rendered on the map overlay.
        /// ObservableCollection automatically notifies the UI when items are added or removed.
        /// </summary>
        public ObservableCollection<LocationPoint> HeatMapPoints
        {
            get => _heatMapPoints;
            set
            {
                _heatMapPoints = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command to start or stop continuous GPS location capture.
        /// </summary>
        public ICommand ToggleTrackingCommand { get; }

        /// <summary>
        /// Command to fetch and refresh the stored GPS location points from the SQLite database.
        /// </summary>
        public ICommand RefreshHeatMapCommand { get; }

        /// <summary>
        /// Command to wipe all stored location history from the SQLite database and map overlay.
        /// </summary>
        public ICommand ClearDataCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// Instantiates underlying services and registers UI action commands.
        /// </summary>
        public MainViewModel()
        {
            // Initialize database service and pass reference to location tracker service
            _databaseService = new DatabaseService();
            _trackerService = new LocationTrackerService(_databaseService);

            // Bind UI commands to their respective asynchronous execution handler methods
            ToggleTrackingCommand = new Command(async () => await ToggleTrackingAsync());
            RefreshHeatMapCommand = new Command(async () => await LoadHeatMapPointsAsync());
            ClearDataCommand = new Command(async () => await ClearDataAsync());
        }

        /// <summary>
        /// Toggles the background location tracking loop on or off.
        /// Subscribes to new location updates to update the active points collection in real time.
        /// </summary>
        private async Task ToggleTrackingAsync()
        {
            if (IsTracking)
            {
                // Stop location monitoring service
                _trackerService.StopTracking();
                IsTracking = false;
            }
            else
            {
                IsTracking = true;

                // Begin location collection and append each new coordinate directly to the observable collection
                await _trackerService.StartTrackingAsync(newPoint =>
                {
                    HeatMapPoints.Add(newPoint);
                });
            }
        }

        /// <summary>
        /// Asynchronously fetches all stored location records from the local SQLite database 
        /// and updates the HeatMapPoints collection.
        /// </summary>
        public async Task LoadHeatMapPointsAsync()
        {
            var points = await _databaseService.GetLocationsAsync();
            HeatMapPoints = new ObservableCollection<LocationPoint>(points);
        }

        /// <summary>
        /// Asynchronously deletes all location records from the local SQLite database 
        /// and clears the active map collection.
        /// </summary>
        private async Task ClearDataAsync()
        {
            await _databaseService.ClearLocationsAsync();
            HeatMapPoints.Clear();
        }

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Helper method to raise property change notifications for UI bindings.
        /// Uses CallerMemberName attribute to automatically supply the calling property name.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}