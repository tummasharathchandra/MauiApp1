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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.HeatMapPoints))
        {
            RenderLocationTrail(ViewModel.HeatMapPoints);
        }
    }

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
            System.Diagnostics.Debug.WriteLine($"Error centering map: {ex.Message}");
        }
    }
}