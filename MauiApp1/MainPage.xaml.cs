
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MauiApp1.Models;
using MauiApp1.ViewModels;

namespace MauiApp1
{
    /// <summary>
    /// MainPage displays the interactive map and renders location overlays.
    /// It connects the user interface to MainViewModel and visualizes
    /// recorded GPS coordinates as density-based colored circles.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Provides convenient access to the ViewModel assigned to this page.
        /// The ViewModel manages tracking commands and location records.
        /// </summary>
        private MainViewModel ViewModel =>
            BindingContext as MainViewModel;

        /// <summary>
        /// Stores the collection currently being observed so that collection
        /// change events can be subscribed to and unsubscribed from correctly.
        /// </summary>
        private ObservableCollection<LocationPoint> _observedPoints;

        /// <summary>
        /// Initializes the page and loads its XAML-defined map and controls.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Runs when the page becomes visible.
        /// Subscribes to ViewModel changes, loads saved SQLite records,
        /// renders the location overlays, and attempts to center the map
        /// on the device's last known or current location.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel == null)
                return;

            // Prevent duplicate event subscriptions when the page reappears.
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Observe additions and removals of individual location records.
            AttachToPointsCollection(ViewModel.HeatMapPoints);

            // Restore previously saved GPS locations from the SQLite database.
            await ViewModel.LoadHeatMapPointsAsync();

            // Draw the density-based overlays using the loaded location history.
            RenderLocationTrail(ViewModel.HeatMapPoints);

            // Center the map on the available device location when possible.
            await CenterMapOnUserAsync();
        }

        /// <summary>
        /// Runs when the page is no longer visible.
        /// Removes event subscriptions to avoid duplicate callbacks and
        /// unnecessary references when the page is opened again.
        /// </summary>
        protected override void OnDisappearing()
        {
            if (ViewModel != null)
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;

            // Stop listening to changes from the previous collection.
            AttachToPointsCollection(null);

            base.OnDisappearing();
        }

        /// <summary>
        /// Handles changes to ViewModel properties.
        /// When the HeatMapPoints collection is replaced, subscribes to the
        /// new collection and redraws the map overlays.
        /// </summary>
        private void OnViewModelPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.HeatMapPoints))
            {
                // The ViewModel may replace the collection after a database load.
                AttachToPointsCollection(ViewModel.HeatMapPoints);

                // Redraw using the latest collection of recorded locations.
                RenderLocationTrail(ViewModel.HeatMapPoints);
            }
        }

        /// <summary>
        /// Subscribes to changes in the ObservableCollection of GPS points.
        /// This allows the map to refresh when points are added or removed,
        /// without requiring the user to restart the application.
        /// </summary>
        private void AttachToPointsCollection(
            ObservableCollection<LocationPoint> points)
        {
            // Remove the old subscription before observing a new collection.
            if (_observedPoints != null)
            {
                _observedPoints.CollectionChanged -=
                    OnPointsCollectionChanged;
            }

            _observedPoints = points;

            // Subscribe only when a valid collection is provided.
            if (_observedPoints != null)
            {
                _observedPoints.CollectionChanged +=
                    OnPointsCollectionChanged;
            }
        }

        /// <summary>
        /// Handles additions, removals, and resets in the location collection.
        /// Recalculates density and redraws the map using the current points.
        /// </summary>
        private void OnPointsCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            RenderLocationTrail(ViewModel?.HeatMapPoints);
        }

        /// <summary>
        /// Renders the location history as colored circle overlays.
        /// For each GPS point, counts other recorded points within a fixed
        /// distance and selects a color representing local point density.
        /// </summary>
        /// <param name="points">
        /// GPS location records retrieved from SQLite or collected during tracking.
        /// </param>
        private void RenderLocationTrail(
            IEnumerable<LocationPoint> points)
        {
            // Remove previous overlays before drawing the latest density display.
            TrackMap.MapElements.Clear();

            if (points == null)
                return;

            // Create a snapshot so the same set of points is used for calculations.
            var locations = points.ToList();

            if (locations.Count == 0)
                return;

            // A point is considered a neighbor when it is within 50 meters.
            const double densityRadiusMeters = 50;

            // Each colored circle extends 35 meters from its center coordinate.
            const double overlayRadiusMeters = 35;

            foreach (var point in locations)
            {
                // Count nearby points, including the current point itself.
                // A higher count represents a greater concentration of locations.
                int nearbyCount = locations.Count(other =>
                    DistanceBetweenMeters(point, other)
                    <= densityRadiusMeters);

                // Choose overlay colors according to the calculated density.
                var (stroke, fill) = GetDensityColors(nearbyCount);

                // Create a circle centered on the recorded GPS coordinate.
                var circle = new Circle
                {
                    Center = new Location(
                        point.Latitude,
                        point.Longitude),

                    Radius = Distance.FromMeters(overlayRadiusMeters),

                    // Outline and semi-transparent fill communicate density.
                    StrokeColor = stroke,
                    StrokeWidth = 2,
                    FillColor = fill
                };

                // Add the circle to the map so it becomes visible to the user.
                TrackMap.MapElements.Add(circle);
            }
        }

        /// <summary>
        /// Calculates the geographic distance between two recorded GPS points
        /// using the Haversine formula, which accounts for Earth's curvature.
        /// The result is returned in meters.
        /// </summary>
        /// <param name="first">First GPS coordinate.</param>
        /// <param name="second">Second GPS coordinate.</param>
        /// <returns>Distance between the coordinates in meters.</returns>
        private static double DistanceBetweenMeters(
            LocationPoint first,
            LocationPoint second)
        {
            // Approximate mean radius of Earth, expressed in meters.
            const double earthRadiusMeters = 6371000;

            // Convert coordinates from degrees to radians for trigonometric functions.
            double lat1 = DegreesToRadians(first.Latitude);
            double lat2 = DegreesToRadians(second.Latitude);

            double deltaLat = DegreesToRadians(
                second.Latitude - first.Latitude);

            double deltaLon = DegreesToRadians(
                second.Longitude - first.Longitude);

            // Apply the Haversine formula to calculate angular separation.
            double a =
                Math.Pow(Math.Sin(deltaLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Pow(Math.Sin(deltaLon / 2), 2);

            // Convert angular separation into a distance along Earth's surface.
            double c = 2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

            return earthRadiusMeters * c;
        }

        /// <summary>
        /// Converts an angle measured in degrees into radians.
        /// The Haversine formula requires angular measurements in radians.
        /// </summary>
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Maps the number of nearby points to a density color.
        /// Blue indicates low density, yellow/orange indicates medium density,
        /// and red indicates high density.
        /// These thresholds are configurable for the application's needs.
        /// </summary>
        /// <param name="count">Number of recorded points within the density radius.</param>
        /// <returns>Outline and fill colors for the corresponding density level.</returns>
        private static (Color Stroke, Color Fill) GetDensityColors(int count)
        {
            // One or two nearby points represent a low-density area.
            if (count <= 2)
            {
                return (
                    Color.FromArgb("#FF2563EB"),
                    Color.FromArgb("#662563EB"));
            }

            // Three to five nearby points represent a medium-density area.
            if (count <= 5)
            {
                return (
                    Color.FromArgb("#FFF59E0B"),
                    Color.FromArgb("#66F59E0B"));
            }

            // Six or more nearby points represent a high-density area.
            return (
                Color.FromArgb("#FFDC2626"),
                Color.FromArgb("#66DC2626"));
        }

        /// <summary>
        /// Attempts to center the map on the device's last known location.
        /// If no last known location is available, requests a current location.
        /// Location failures are logged for debugging instead of crashing the page.
        /// </summary>
        private async Task CenterMapOnUserAsync()
        {
            try
            {
                var location =
                    await Geolocation.Default.GetLastKnownLocationAsync()
                    ?? await Geolocation.Default.GetLocationAsync();

                if (location != null)
                {
                    // Display a map region with a two-kilometer radius.
                    TrackMap.MoveToRegion(
                        MapSpan.FromCenterAndRadius(
                            new Location(
                                location.Latitude,
                                location.Longitude),
                            Distance.FromKilometers(2)));
                }
            }
            catch (Exception ex)
            {
                // Record the failure for diagnosis if location access is unavailable.
                System.Diagnostics.Debug.WriteLine(
                    $"Error centering map viewport: {ex.Message}");
            }
        }
    }
}
