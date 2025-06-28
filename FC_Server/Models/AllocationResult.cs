using System;
using System.Collections.Generic;

namespace FC_Server.Models
{
    public class AllocationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? AllocatedShelterId { get; set; }
        public string ShelterName { get; set; }
        public double? Distance { get; set; }
        public DateTime? EstimatedArrivalTime { get; set; }
        public string RoutePolyline { get; set; }
        public List<string> RouteInstructions { get; set; }
        public ShelterDetailsDto ShelterDetails { get; set; }
        public string RecommendedAction { get; set; }
    }

    public class AllocationRequest
    {
        public User User { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AlertLatitude { get; set; }
        public double AlertLongitude { get; set; }
        public int AlertId { get; set; }
        public DateTime RequestTime { get; set; }
    }

    public class AssignmentInfo
    {
        public int UserId { get; set; }
        public int ShelterId { get; set; }
        public double Distance { get; set; }
    }


}