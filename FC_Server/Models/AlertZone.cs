using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace FC_Server.Models
{
    public class AlertZone
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; }
        public string PolygonCoordinates { get; set; }
        public int ResponseTime { get; set; }
        public bool IsActive { get; set; }

        private List<Coordinate> _coordinates;

        [JsonIgnore]
        public List<Coordinate> Coordinates
        {
            get
            {
                if (_coordinates == null && !string.IsNullOrEmpty(PolygonCoordinates))
                {
                    _coordinates = JsonConvert.DeserializeObject<List<Coordinate>>(PolygonCoordinates);
                }
                return _coordinates ?? new List<Coordinate>();
            }
        }

        // בודק אם נקודה נמצאת בתוך הפוליגון
        public bool ContainsPoint(double latitude, double longitude)
        {
            if (Coordinates.Count < 3) return false;

            bool inside = false;
            int n = Coordinates.Count;

            double p1x = Coordinates[0].Longitude;
            double p1y = Coordinates[0].Latitude;

            for (int i = 1; i <= n; i++)
            {
                double p2x = Coordinates[i % n].Longitude;
                double p2y = Coordinates[i % n].Latitude;

                if (latitude > Math.Min(p1y, p2y))
                {
                    if (latitude <= Math.Max(p1y, p2y))
                    {
                        if (longitude <= Math.Max(p1x, p2x))
                        {
                            if (p1y != p2y)
                            {
                                double xinters = (latitude - p1y) * (p2x - p1x) / (p2y - p1y) + p1x;
                                if (p1x == p2x || longitude <= xinters)
                                    inside = !inside;
                            }
                        }
                    }
                }

                p1x = p2x;
                p1y = p2y;
            }

            return inside;
        }

        // בודק אם שם האזור תואם להתרעה
        public bool MatchesAlertAreaName(string areaName)
        {
            if (string.IsNullOrEmpty(areaName)) return false;

            // נרמול לצורך השוואה
            string normalized1 = ZoneName.Trim().Replace(" ", "").Replace("-", "");
            string normalized2 = areaName.Trim().Replace(" ", "").Replace("-", "");

            return normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}