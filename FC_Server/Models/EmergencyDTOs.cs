using System;
using System.Collections.Generic;

namespace FC_Server.Models
{
    #region Request DTOs

    public class UserLocationRequest
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class FindShelterRequest
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? CenterLatitude { get; set; }
        public double? CenterLongitude { get; set; }
    }

    public class MultipleAllocationRequest
    {
        public List<UserLocationDto> UserLocations { get; set; }
        public double? CenterLatitude { get; set; }
        public double? CenterLongitude { get; set; }
    }

    public class ReleaseShelterRequest
    {
        public int UserId { get; set; }
    }

    #endregion

    #region Response DTOs

    public class ShelterRouteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool HasArrived { get; set; }
        public ShelterDetailsDto ShelterDetails { get; set; }
        public RouteInfoDto RouteInfo { get; set; }
        public bool RequiresAction { get; set; }
        public string ActionType { get; set; } // NAVIGATE, FIND_ALTERNATIVE, RETURN_TO_SHELTER
        public string RecommendedAction { get; set; }
    }

    public class LocationUpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool HasArrived { get; set; }
        public RouteInfoDto UpdatedRoute { get; set; }
        public double? DistanceRemaining { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public string CurrentInstruction { get; set; }
        public bool RequiresAction { get; set; }
        public string ActionType { get; set; }
    }

    public class EmergencyStatusResponse
    {
        public bool IsAlertActive { get; set; }
        public string UserStatus { get; set; } // NOT_ALLOCATED, EN_ROUTE, ARRIVED, LEFT_SHELTER, ALERT_ENDED
        public int? ShelterId { get; set; }
        public TimeSpan? TimeInShelter { get; set; }
        public string Message { get; set; }
        public bool RequiresAction { get; set; }
        public string ActionType { get; set; }
    }

    public class AreaStatusResponse
    {
        public int TotalShelters { get; set; }
        public int AvailableShelters { get; set; }
        public int FullShelters { get; set; }
        public List<ShelterStatusDto> Shelters { get; set; }
    }

    public class MultipleAllocationResponse
    {
        public bool Success { get; set; }
        public int TotalProcessed { get; set; }
        public int SuccessfulAllocations { get; set; }
        public int FailedAllocations { get; set; }
        public List<UserAllocationResult> UserResults { get; set; }
        public AllocationStatisticsDto Statistics { get; set; }
    }

    #endregion

    #region Detail DTOs

    public class ShelterDetailsDto
    {
        public int ShelterId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Capacity { get; set; }
        public int? CurrentOccupancy { get; set; }
        public bool IsAccessible { get; set; }
        public bool PetsFriendly { get; set; }
        public string AdditionalInfo { get; set; }
        public double? Distance { get; set; }
        public TimeSpan? EstimatedWaitTime { get; set; }
    }

    public class RouteInfoDto
    {
        public double Distance { get; set; }
        public DateTime? EstimatedArrivalTime { get; set; }
        public string RoutePolyline { get; set; }
        public string CurrentInstruction { get; set; }
        public List<string> AllInstructions { get; set; }
        public List<double[]> RouteCoordinates { get; set; }
    }

    public class ShelterStatusDto
    {
        public int ShelterId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Capacity { get; set; }
        public int CurrentOccupancy { get; set; }
        public int AvailableSpaces { get; set; }
        public double OccupancyPercentage { get; set; }
        public string Status { get; set; } // Available, Moderate, AlmostFull, Full
        public double Distance { get; set; }
    }

    public class UserLocationDto
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class UserAllocationResult
    {
        public int UserId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public ShelterDetailsDto AllocatedShelter { get; set; }
    }

    public class AllocationStatisticsDto
    {
        public double AverageDistance { get; set; }
        public double MaxDistance { get; set; }
        public double AllocationPercentage { get; set; }
    }

    #endregion

    #region Alert DTOs

    public class ActiveEmergencyAlert
    {
        public int AlertId { get; set; }
        public string AlertType { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmergencyAlertStatus
    {
        public int AlertId { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int AffectedUsers { get; set; }
    }

    public class EmergencyAlertRequest
    {
        public string AlertType { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public string CreatedBy { get; set; }
    }

    #endregion

    #region Supporting DTOs - For ServerSimulation Compatibility

    public class PersonDto
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? NearestShelterId { get; set; }
        public double? NearestShelterDistance { get; set; }
        public bool IsManual { get; set; } = false;
    }

    public class ShelterDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Capacity { get; set; }
    }

    public class AssignmentDto
    {
        public int PersonId { get; set; }
        public int ShelterId { get; set; }
        public double Distance { get; set; }
        public bool IsWalkingDistance { get; set; } = false;
        public string RoutePolyline { get; set; }
        public List<double[]> RouteCoordinates { get; set; }
    }

    #endregion

    #region Route Information

    public class RouteInfo
    {
        public string OverviewPolyline { get; set; }
        public List<string> TextInstructions { get; set; }
        public double Distance { get; set; }
    }

    #endregion
}