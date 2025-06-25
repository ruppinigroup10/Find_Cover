using System;
using System.Collections.Generic;

namespace FC_Server.Models
{
    /// <summary>
    /// מודל למעקב אחר משתמש בדרך למרחב מוגן
    /// </summary>
    public class UserTracking
    {
        public int tracking_id { get; set; }
        public int user_id { get; set; }
        public int shelter_id { get; set; }
        public DateTime start_time { get; set; }
        public DateTime? arrival_time { get; set; }
        public double last_latitude { get; set; }
        public double last_longitude { get; set; }
        public DateTime last_update_time { get; set; }
        public string status { get; set; } = "EN_ROUTE";
        public string route_polyline { get; set; }
        public double average_speed { get; set; } = 5.0; // ק"מ/שעה
    }

    /// <summary>
    /// תוצאת מעקב
    /// </summary>
    public class TrackingResult
    {
        public bool IsBeingTracked { get; set; }
        public int ShelterId { get; set; }
        public double DistanceToShelter { get; set; }
        public bool HasArrivedAtShelter { get; set; }
        public bool HasDeviatedFromRoute { get; set; }
        public TimeSpan? EstimatedTimeToArrival { get; set; }
        public string CurrentNavigationInstruction { get; set; }
    }

    /// <summary>
    /// נתוני מיקום אחרון של משתמש
    /// </summary>
    public class UserLocationData
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// סטטוס מיקום משתמש
    /// </summary>
    public class UserLocationStatus
    {
        public double DistanceToShelter { get; set; }
        public bool HasArrivedAtShelter { get; set; }
        public bool IsNearShelter { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}