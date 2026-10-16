using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    /// <summary>
    /// Service responsible for managing local SQLite database connection,
    /// initialization, and asynchronous CRUD operations for location points.
    /// </summary>
    public class DatabaseService
    {
        // Asynchronous connection instance for SQLite database operations
        private SQLiteAsyncConnection _database;

        /// <summary>
        /// Asynchronously initializes the SQLite database connection and creates
        /// the required LocationPoint table if it does not already exist.
        /// </summary>
        private async Task InitAsync()
        {
            if (_database != null)
                return;

            // Define the local application data path for the SQLite database file
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "heatmap_locations.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            // Create the database table for storing GPS location points
            await _database.CreateTableAsync<LocationPoint>();
        }

        /// <summary>
        /// Inserts a new GPS coordinate with a UTC timestamp into the SQLite database.
        /// </summary>
        /// <param name="latitude">Recorded latitude value.</param>
        /// <param name="longitude">Recorded longitude value.</param>
        /// <returns>The number of rows inserted into the table.</returns>
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

        /// <summary>
        /// Asynchronously retrieves all stored location point records from the SQLite database.
        /// </summary>
        /// <returns>A list of all persisted LocationPoint objects.</returns>
        public async Task<List<LocationPoint>> GetLocationsAsync()
        {
            await InitAsync();
            return await _database.Table<LocationPoint>().ToListAsync();
        }

        /// <summary>
        /// Deletes all recorded location points from the local SQLite database.
        /// </summary>
        /// <returns>The number of rows deleted from the table.</returns>
        public async Task<int> ClearLocationsAsync()
        {
            await InitAsync();
            return await _database.DeleteAllAsync<LocationPoint>();
        }
    }
}