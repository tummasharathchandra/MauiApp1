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
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly LocationTrackerService _trackerService;

        private bool _isTracking;
        private ObservableCollection<LocationPoint> _heatMapPoints = new();

        public bool IsTracking
        {
            get => _isTracking;
            set
            {
                _isTracking = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TrackingButtonText));
            }
        }

        public string TrackingButtonText => IsTracking ? "Stop" : "Start";

        public ObservableCollection<LocationPoint> HeatMapPoints
        {
            get => _heatMapPoints;
            set
            {
                _heatMapPoints = value;
                OnPropertyChanged();
            }
        }

        public ICommand ToggleTrackingCommand { get; }
        public ICommand RefreshHeatMapCommand { get; }
        public ICommand ClearDataCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _trackerService = new LocationTrackerService(_databaseService);

            ToggleTrackingCommand = new Command(async () => await ToggleTrackingAsync());
            RefreshHeatMapCommand = new Command(async () => await LoadHeatMapPointsAsync());
            ClearDataCommand = new Command(async () => await ClearDataAsync());
        }

        private async Task ToggleTrackingAsync()
        {
            if (IsTracking)
            {
                _trackerService.StopTracking();
                IsTracking = false;
            }
            else
            {
                IsTracking = true;
                await _trackerService.StartTrackingAsync(newPoint =>
                {
                    HeatMapPoints.Add(newPoint);
                });
            }
        }

        public async Task LoadHeatMapPointsAsync()
        {
            var points = await _databaseService.GetLocationsAsync();
            HeatMapPoints = new ObservableCollection<LocationPoint>(points);
        }

        private async Task ClearDataAsync()
        {
            await _databaseService.ClearLocationsAsync();
            HeatMapPoints.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
