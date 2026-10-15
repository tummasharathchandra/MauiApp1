using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        private async Task InitAsync()
        {
            if (_database != null)
                return;

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "heatmap_locations.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            await _database.CreateTableAsync<LocationPoint>();
        }

        public async Task<int> SaveLocationAsync(double latitude, double longitude)
        {
            await InitAsync();

            var point = new LocationPoint
            {
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = DateTime.UtcNow
            };

            return await _database.InsertAsync(point);
        }

        public async Task<List<LocationPoint>> GetLocationsAsync()
        {
            await InitAsync();
            return await _database.Table<LocationPoint>().ToListAsync();
        }

        public async Task<int> ClearLocationsAsync()
        {
            await InitAsync();
            return await _database.DeleteAllAsync<LocationPoint>();
        }
    }
}
