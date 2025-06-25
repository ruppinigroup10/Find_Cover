using System;
using System.Collections.Generic;

namespace FC_Server.Models
{
    #region Google Maps Request/Response Models

    public class DirectionsRequest
    {
        public LocationPoint Origin { get; set; }
        public LocationPoint Destination { get; set; }
        public TravelMode Mode { get; set; } = TravelMode.Walking;
        public string Language { get; set; } = "he";
        public List<LocationPoint> Waypoints { get; set; }
        public bool AvoidHighways { get; set; }
        public bool AvoidTolls { get; set; }
    }

    public class DirectionsResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public List<Route> Routes { get; set; }
    }

    public class DistanceMatrixRequest
    {
        public List<LocationPoint> Origins { get; set; }
        public List<LocationPoint> Destinations { get; set; }
        public TravelMode Mode { get; set; } = TravelMode.Walking;
        public string Units { get; set; } = "metric";
        public string Language { get; set; } = "he";
    }

    public class DistanceMatrixResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> OriginAddresses { get; set; }
        public List<string> DestinationAddresses { get; set; }
        public List<DistanceMatrixRow> Rows { get; set; }
    }

    #endregion

    #region Google Maps Data Models

    public class Route
    {
        public string Summary { get; set; }
        public string OverviewPolyline { get; set; }
        public List<RouteLeg> Legs { get; set; }
        public RouteBounds Bounds { get; set; }
        public string Copyrights { get; set; }
        public List<string> Warnings { get; set; }
    }

    public class RouteLeg
    {
        public DistanceInfo Distance { get; set; }
        public DurationInfo Duration { get; set; }
        public string StartAddress { get; set; }
        public string EndAddress { get; set; }
        public LocationPoint StartLocation { get; set; }
        public LocationPoint EndLocation { get; set; }
        public List<RouteStep> Steps { get; set; }
    }

    public class RouteStep
    {
        public string HtmlInstructions { get; set; }
        public DistanceInfo Distance { get; set; }
        public DurationInfo Duration { get; set; }
        public LocationPoint StartLocation { get; set; }
        public LocationPoint EndLocation { get; set; }
        public string Polyline { get; set; }
        public TravelMode TravelMode { get; set; }
        public string Maneuver { get; set; }
    }

    public class DistanceMatrixRow
    {
        public List<DistanceMatrixElement> Elements { get; set; }
    }

    public class DistanceMatrixElement
    {
        public string Status { get; set; }
        public DistanceInfo Distance { get; set; }
        public DurationInfo Duration { get; set; }
    }

    public class DistanceInfo
    {
        public string Text { get; set; }
        public int Value { get; set; } // במטרים
    }

    public class DurationInfo
    {
        public string Text { get; set; }
        public int Value { get; set; } // בשניות
    }

    public class LocationPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Name { get; set; }

        public LocationPoint() { }

        public LocationPoint(double lat, double lon, string name = "")
        {
            Latitude = lat;
            Longitude = lon;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Latitude},{Longitude}";
        }
    }

    public class RouteBounds
    {
        public LocationPoint Northeast { get; set; }
        public LocationPoint Southwest { get; set; }
    }

    #endregion

    #region Enums

    public enum TravelMode
    {
        Walking,
        Driving,
        Transit,
        Bicycling
    }

    #endregion
}