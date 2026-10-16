using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MauiApp1.ViewModels;
using MauiApp1.Models;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
    private MainViewModel ViewModel => BindingContext as MainViewModel;

    public MainPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles page appearance by loading logged points from SQLite and centering the map.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            await ViewModel.LoadHeatMapPointsAsync();
            RenderLocationTrail(ViewModel.HeatMapPoints);
        }

        await CenterMapOnUserAsync();
    }

    /// <summary>
    /// Unsubscribes event listeners to prevent memory leaks.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// Listens for view model property changes and triggers map rendering.
    /// </summary>
    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.HeatMapPoints))
        {
            RenderLocationTrail(ViewModel.HeatMapPoints);
        }
    }

    /// <summary>
    /// Clears existing map elements and renders circular overlays for each tracked GPS point.
    /// </summary>
    /// <param name="points">Recorded GPS points retrieved from database persistence</param>
    private void RenderLocationTrail(IEnumerable<LocationPoint> points)
    {
        TrackMap.MapElements.Clear();

        if (points == null || !points.Any())
            return;

        foreach (var point in points)
        {
            var circle = new Circle
            {
                Center = new Location(point.Latitude, point.Longitude),
                Radius = Distance.FromMeters(20),
                StrokeColor = Colors.DarkBlue,
                StrokeWidth = 2,
                FillColor = Color.FromRgba(0, 122, 255, 180)
            };

            TrackMap.MapElements.Add(circle);
        }
    }

    /// <summary>
    /// Retrieves current geolocation coordinates and moves the map viewport focus.
    /// </summary>
    private async Task CenterMapOnUserAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync()
                           ?? await Geolocation.Default.GetLocationAsync();

            if (location != null)
            {
                TrackMap.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        new Location(location.Latitude, location.Longitude),
                        Distance.FromKilometers(2)
                    )
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error centering map viewport: {ex.Message}");
        }
    }
}