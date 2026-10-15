using System;
using SQLite;

namespace MauiApp1.Models
{
    /// <summary>
    /// Entity stored in the local SQLite database representing tracked GPS positions.
    /// </summary>
    [Table("LocationPoints")]
    public class LocationPoint
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}