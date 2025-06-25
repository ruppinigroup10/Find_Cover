using System;
using System.Collections.Generic;

namespace FC_Server.Models
{
    /// <summary>
    /// מודל להקצאת משתמש למרחב מוגן
    /// </summary>
    public class ShelterAllocation
    {
        public int allocation_id { get; set; }
        public int user_id { get; set; }
        public int shelter_id { get; set; }
        public int alert_id { get; set; }
        public DateTime allocation_time { get; set; }
        public DateTime? arrival_time { get; set; }
        public DateTime? exit_time { get; set; }
        public string status { get; set; } = "EN_ROUTE";
        public bool is_active { get; set; } = true;
        public double? walking_distance { get; set; }
        public int? actual_walking_time { get; set; }
    }

    /// <summary>
    /// תוצאת הקצאה
    /// </summary>
    public class ShelterAllocationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? AllocatedShelterId { get; set; }
        public string ShelterName { get; set; }
        public int? NearestShelterId { get; set; }
        public double? Distance { get; set; }
        public DateTime? EstimatedArrivalTime { get; set; }
        public TimeSpan? EstimatedWaitTime { get; set; }
        public string RoutePolyline { get; set; }
        public List<string> RouteInstructions { get; set; }
        public string RecommendedAction { get; set; }
        public ShelterDetailsDto ShelterDetails { get; set; }
    }

    /// <summary>
    /// תוצאת הקצאה המונית
    /// </summary>
    public class BatchAllocationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<int, ShelterAllocationResult> AllocationResults { get; set; }
        public Dictionary<int, int> ShelterOccupancy { get; set; } = new Dictionary<int, int>();
        public int TotalProcessed { get; set; }
        public int SuccessfulAllocations { get; set; }
        public int FailedAllocations { get; set; }
        public DateTime CompletionTime { get; set; }
    }

    /// <summary>
    /// מיקום משתמש להקצאה
    /// </summary>
    public class UserLocationWithDetails
    {
        public User User { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>
    /// סטטיסטיקות הקצאה
    /// </summary>
    public class AllocationStatistics
    {
        public int stat_id { get; set; }
        public int UserId { get; set; }
        public int ShelterId { get; set; }
        public DateTime AllocationTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public DateTime ReleaseTime { get; set; }
        public TimeSpan TotalTimeInShelter { get; set; }
        public double WalkingDistance { get; set; }
        public TimeSpan? ActualWalkingTime { get; set; }
    }

    /// <summary>
    /// הקצאת משתמש - לשימוש פנימי
    /// </summary>
    public class UserAllocation
    {
        public int UserId { get; set; }
        public int ShelterId { get; set; }
        public int AlertId { get; set; }
        public DateTime AllocationTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public string Status { get; set; }
        public ShelterDetailsDto ShelterDetails { get; set; }
        public double WalkingDistance { get; set; }
        public TimeSpan? ActualWalkingTime { get; set; }
    }
}