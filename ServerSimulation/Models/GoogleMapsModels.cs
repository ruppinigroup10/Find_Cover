using System;
using System.Collections.Generic;

namespace ServerSimulation.Models
{
    /// <summary>
    /// Request model for calculating distances between multiple origins and destinations
    /// </summary>
    // public class DistanceMatrixRequest
    // {
    //     public List<LocationPoint> Origins { get; set; }
    //     public List<LocationPoint> Destinations { get; set; }
    //     public TravelMode Mode { get; set; } = TravelMode.Walking;
    //     public bool AvoidHighways { get; set; } = false;
    //     public bool AvoidTolls { get; set; } = false;
    // }

    /// <summary>
    /// Response model containing the distance matrix results
    /// </summary>
    // public class DistanceMatrixResponse
    // {
    //     public bool Success { get; set; }
    //     public string ErrorMessage { get; set; }
    //     public List<DistanceMatrixRow> Rows { get; set; }
    //     public string Status { get; set; }
    // }

    /// <summary>
    /// Represents a single row in the distance matrix (one origin to all destinations)
    /// </summary>
    // public class DistanceMatrixRow
    // {
    //     public List<DistanceMatrixElement> Elements { get; set; }
    // }

    /// <summary>
    /// Represents a single origin-destination pair result
    /// </summary>
    // public class DistanceMatrixElement
    // {
    //     public DistanceInfo Distance { get; set; }
    //     public DurationInfo Duration { get; set; }
    //     public string Status { get; set; }
    // }

    /// <summary>
    /// Distance information between two points
    /// </summary>
    public class DistanceInfo
    {
        public string Text { get; set; } // Human-readable distance (e.g., "1.2 km")
        public int Value { get; set; } // Distance in meters
    }

    /// <summary>
    /// Duration information for travel between two points
    /// </summary>
    public class DurationInfo
    {
        public string Text { get; set; } // Human-readable duration (e.g., "15 mins")
        public int Value { get; set; } // Duration in seconds
    }

    /// <summary>
    /// Represents a geographic location
    /// </summary>
    public class LocationPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Id { get; set; } // Optional identifier for the location

        public LocationPoint() { }

        public LocationPoint(double latitude, double longitude, string id = null)
        {
            Latitude = latitude;
            Longitude = longitude;
            Id = id;
        }

        public override string ToString()
        {
            return $"{Latitude},{Longitude}";
        }
    }

    /// <summary>
    /// Travel modes supported by Google Maps
    /// </summary>
    public enum TravelMode
    {
        Driving,
        Walking,
        Bicycling,
        Transit
    }

    /// <summary>
    /// Request model for getting directions between two points
    /// </summary>
    public class DirectionsRequest
    {
        public LocationPoint Origin { get; set; }
        public LocationPoint Destination { get; set; }
        public TravelMode Mode { get; set; } = TravelMode.Walking;
        public bool Alternatives { get; set; } = false;
        public DateTime? DepartureTime { get; set; }
    }

    /// <summary>
    /// Response model for directions
    /// </summary>
    public class DirectionsResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<Route> Routes { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Represents a route from origin to destination
    /// </summary>
    public class Route
    {
        public string Summary { get; set; }
        public List<Leg> Legs { get; set; }
        public string OverviewPolyline { get; set; }
        public Bounds Bounds { get; set; }
    }

    /// <summary>
    /// Represents a leg of a journey (portion between waypoints)
    /// </summary>
    public class Leg
    {
        public DistanceInfo Distance { get; set; }
        public DurationInfo Duration { get; set; }
        public string StartAddress { get; set; }
        public string EndAddress { get; set; }
        public LocationPoint StartLocation { get; set; }
        public LocationPoint EndLocation { get; set; }
        public List<Step> Steps { get; set; }
    }

    /// <summary>
    /// Represents a single step in navigation
    /// </summary>
    public class Step
    {
        public DistanceInfo Distance { get; set; }
        public DurationInfo Duration { get; set; }
        public LocationPoint StartLocation { get; set; }
        public LocationPoint EndLocation { get; set; }
        public string HtmlInstructions { get; set; }
        public string Polyline { get; set; }
        public TravelMode TravelMode { get; set; }
    }

    /// <summary>
    /// Represents geographic bounds
    /// </summary>
    public class Bounds
    {
        public LocationPoint Northeast { get; set; }
        public LocationPoint Southwest { get; set; }
    }

    /// <summary>
    /// Configuration for Google Maps API
    /// </summary>
    public class GoogleMapsConfig
    {
        public string ApiKey { get; set; }
        public int MaxElementsPerRequest { get; set; } = 100; // Google's limit is 100 elements per request
        public int MaxWaypointsPerRoute { get; set; } = 25; // Google's limit is 25 waypoints
        public string Language { get; set; } = "he"; // Hebrew for Israel
        public string Region { get; set; } = "IL"; // Israel region
    }
}