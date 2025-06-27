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
}