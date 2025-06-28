using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FC_Server.Models;
using FC_Server.Services;
using FC_Server.DAL;

namespace FC_Server.Controllers
{
    /// <summary>
    /// קונטרולר לטיפול בתגובת חירום ומציאת מרחבים מוגנים
    /// נוצר כדי לא להתנגש עם AlertController הקיים
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EmergencyResponseController : ControllerBase
    {
        //private readonly ShelterAllocationService _allocationService;
        private readonly EmergencyAlertService _emergencyAlertService;
        private readonly UserLocationTrackingService _locationTrackingService;
        private readonly ILogger<EmergencyResponseController> _logger;
        private readonly BatchShelterAllocationService _batchAllocationService;

        public EmergencyResponseController(
            BatchShelterAllocationService batchAllocationService,
            //ShelterAllocationService allocationService,
            EmergencyAlertService emergencyAlertService,
            UserLocationTrackingService locationTrackingService,
            ILogger<EmergencyResponseController> logger)
        {
            _batchAllocationService = batchAllocationService;
            //_allocationService = allocationService;
            _emergencyAlertService = emergencyAlertService;
            _locationTrackingService = locationTrackingService;
            _logger = logger;
        }

        /// <summary>
        /// קבלת נתיב למרחב מוגן בזמן אזעקה
        /// </summary>
        [HttpPost("get-shelter-route")]
        public async Task<IActionResult> GetShelterRoute([FromBody] UserLocationRequest request)
        {
            try
            {
                // בדיקה אם יש אזעקה פעילה באזור
                var activeAlert = await _emergencyAlertService.GetActiveAlertForLocation(
                    request.Latitude,
                    request.Longitude);

                if (activeAlert == null)
                {
                    return Ok(new ShelterRouteResponse
                    {
                        Success = false,
                        Message = "אין התראה פעילה באזור שלך",
                        RequiresAction = false
                    });
                }


                //Check for existing allocation FOR THIS SPECIFIC ALERT
                DBservicesShelter dbs = new DBservicesShelter();
                var existingVisit = dbs.GetActiveUserAllocationForAlert(request.UserId, activeAlert.AlertId);
                UserAllocation existingAllocation = null;

                // בדיקה אם המשתמש כבר מוקצה למרחב מוגן
                // var existingAllocation = await _allocationService.GetActiveAllocationForUser(request.UserId);


                if (existingVisit != null && existingVisit.alert_id == activeAlert.AlertId)
                {
                    var shelter = Shelter.getShelter(existingVisit.shelter_id);
                    existingAllocation = new UserAllocation
                    {
                        UserId = request.UserId,
                        ShelterId = existingVisit.shelter_id,
                        AlertId = existingVisit.alert_id,
                        AllocationTime = existingVisit.arrival_time ?? DateTime.Now,
                        Status = existingVisit.status,
                        ShelterDetails = new ShelterDetailsDto
                        {
                            ShelterId = shelter.ShelterId,
                            Name = shelter.Name,
                            Address = shelter.Address,
                            Latitude = shelter.Latitude,
                            Longitude = shelter.Longitude
                        }
                    };
                }

                if (existingAllocation != null)
                {
                    // המשתמש כבר מוקצה - בדוק אם הגיע או בדרך
                    var locationStatus = await CheckUserLocationStatus(
                        request.UserId,
                        request.Latitude,
                        request.Longitude,
                        existingAllocation);

                    if (locationStatus.HasArrivedAtShelter)
                    {
                        return Ok(new ShelterRouteResponse
                        {
                            Success = true,
                            Message = "הגעת למרחב המוגן",
                            HasArrived = true,
                            ShelterDetails = existingAllocation.ShelterDetails,
                            RequiresAction = false
                        });
                    }

                    // עדיין בדרך - החזר נתיב מעודכן
                    var updatedRoute = await _batchAllocationService.RecalculateRoute(
                            request.UserId,
                            request.Latitude,
                            request.Longitude);

                    return Ok(new ShelterRouteResponse
                    {
                        Success = true,
                        Message = "המשך לנווט למרחב המוגן",
                        ShelterDetails = existingAllocation.ShelterDetails,
                        RouteInfo = updatedRoute,
                        RequiresAction = true,
                        ActionType = "NAVIGATE"
                    });
                }

                // משתמש חדש - הקצה מרחב מוגן
                var user = FC_Server.Models.User.getUser(request.UserId);
                if (user == null)
                {
                    return BadRequest(new { success = false, message = "משתמש לא נמצא" });
                }

                var allocationResult = await _batchAllocationService.RequestShelterAllocation(
                    user,
                    request.Latitude,
                    request.Longitude,
                    activeAlert.CenterLatitude,
                    activeAlert.CenterLongitude,
                    activeAlert.AlertId);

                if (!allocationResult.Success)
                {
                    return Ok(new ShelterRouteResponse
                    {
                        Success = false,
                        Message = allocationResult.Message,
                        RequiresAction = true,
                        ActionType = "FIND_ALTERNATIVE",
                        RecommendedAction = allocationResult.RecommendedAction
                    });
                }

                // הקצאה הצליחה - התחל מעקב אחר המשתמש
                await _locationTrackingService.StartTrackingUser(
                    request.UserId,
                    allocationResult.AllocatedShelterId.Value,
                    request.Latitude,
                    request.Longitude);

                return Ok(new ShelterRouteResponse
                {
                    Success = true,
                    Message = "הוקצה מרחב מוגן - התחל לנווט",
                    ShelterDetails = new ShelterDetailsDto
                    {
                        ShelterId = allocationResult.AllocatedShelterId.Value,
                        Name = allocationResult.ShelterName,
                        Address = allocationResult.ShelterDetails.Address,
                        Latitude = allocationResult.ShelterDetails.Latitude,
                        Longitude = allocationResult.ShelterDetails.Longitude,
                        Distance = allocationResult.Distance
                    },
                    RouteInfo = new RouteInfoDto
                    {
                        Distance = allocationResult.Distance.Value,
                        EstimatedArrivalTime = allocationResult.EstimatedArrivalTime,
                        RoutePolyline = allocationResult.RoutePolyline,
                        CurrentInstruction = allocationResult.RouteInstructions?.FirstOrDefault(),
                        AllInstructions = allocationResult.RouteInstructions
                    },
                    RequiresAction = true,
                    ActionType = "NAVIGATE"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shelter route for user {UserId}", request.UserId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "אירעה שגיאה במציאת מרחב מוגן"
                });
            }
        }

        /// <summary>
        /// עדכון מיקום משתמש - נקרא באופן תקופתי מהלקוח
        /// </summary>
        [HttpPost("update-location")]
        public async Task<IActionResult> UpdateUserLocation([FromBody] UserLocationRequest request)
        {
            try
            {
                // עדכון המיקום במערכת המעקב
                var trackingResult = await _locationTrackingService.UpdateUserLocation(
                    request.UserId,
                    request.Latitude,
                    request.Longitude);

                if (!trackingResult.IsBeingTracked)
                {
                    return Ok(new LocationUpdateResponse
                    {
                        Success = true,
                        Message = "אין מעקב פעיל",
                        RequiresAction = false
                    });
                }

                // בדיקה אם המשתמש הגיע למרחב המוגן
                if (trackingResult.HasArrivedAtShelter)
                {
                    // סמן את המשתמש כנמצא במרחב
                    await _batchAllocationService.MarkUserAsArrived(
                        request.UserId,
                        trackingResult.ShelterId);

                    return Ok(new LocationUpdateResponse
                    {
                        Success = true,
                        Message = "הגעת למרחב המוגן!",
                        HasArrived = true,
                        RequiresAction = false
                    });
                }

                // בדיקה אם המשתמש סטה מהנתיב
                if (trackingResult.HasDeviatedFromRoute)
                {
                    // חישוב נתיב מחדש
                    var newRoute = await _batchAllocationService.RecalculateRoute(
                        request.UserId,
                        request.Latitude,
                        request.Longitude);

                    return Ok(new LocationUpdateResponse
                    {
                        Success = true,
                        Message = "חושב נתיב מחדש",
                        UpdatedRoute = newRoute,
                        RequiresAction = true,
                        ActionType = "ROUTE_UPDATED"
                    });
                }

                // המשך רגיל בנתיב
                return Ok(new LocationUpdateResponse
                {
                    Success = true,
                    Message = "ממשיך במעקב",
                    DistanceRemaining = trackingResult.DistanceToShelter,
                    EstimatedTimeRemaining = trackingResult.EstimatedTimeToArrival,
                    CurrentInstruction = trackingResult.CurrentNavigationInstruction
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location for user {UserId}", request.UserId);
                return StatusCode(500, new { success = false, message = "שגיאה בעדכון מיקום" });
            }
        }

        /// <summary>
        ///DEBUGGING
        /// </summary>
        // [HttpPost("update-location")]
        // public async Task<IActionResult> UpdateUserLocation([FromBody] UserLocationRequest request)
        // {
        //     try
        //     {
        //         _logger.LogInformation($"UpdateUserLocation called: userId={request?.UserId}, lat={request?.Latitude}, lon={request?.Longitude}");

        //         // Test basic response first
        //         return Ok(new
        //         {
        //             success = true,
        //             message = "Test successful",
        //             data = request,
        //             timestamp = DateTime.Now
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error in UpdateUserLocation");
        //         // Return detailed error for debugging
        //         return StatusCode(500, new
        //         {
        //             success = false,
        //             message = "שגיאה בעדכון מיקום",
        //             error = ex.Message,
        //             stackTrace = ex.StackTrace // Remove this in production!
        //         });
        //     }
        // }

        /// <summary>
        /// בדיקת סטטוס אזעקה וטיפול אוטומטי
        /// </summary>
        [HttpGet("check-emergency-status")]
        public async Task<IActionResult> CheckEmergencyStatus([FromQuery] int userId)
        {
            try
            {
                var userAllocation = await _batchAllocationService.GetActiveAllocationForUser(userId);

                if (userAllocation == null)
                {
                    return Ok(new EmergencyStatusResponse
                    {
                        IsAlertActive = false,
                        UserStatus = "NOT_ALLOCATED"
                    });
                }

                // Null check for AlertId
                if (userAllocation.AlertId == 0)
                {
                    return Ok(new EmergencyStatusResponse
                    {
                        IsAlertActive = false,
                        UserStatus = "NO_ALERT",
                        Message = "No active alert"
                    });
                }

                // בדיקה אם האזעקה הסתיימה
                var alertStatus = await _emergencyAlertService.GetAlertStatus(userAllocation.AlertId);

                if (!alertStatus.IsActive)
                {
                    // האזעקה הסתיימה - שחרר את המשתמש אוטומטית
                    await HandleAlertEnded(userId, userAllocation);

                    return Ok(new EmergencyStatusResponse
                    {
                        IsAlertActive = false,
                        UserStatus = "ALERT_ENDED",
                        Message = "האזעקה הסתיימה - ניתן לצאת מהמרחב המוגן"
                    });
                }

                // בדיקה אם המשתמש עזב את המרחב (לפי מיקום)
                var lastLocation = await _locationTrackingService.GetLastKnownLocation(userId);
                if (lastLocation != null && userAllocation.Status == "ARRIVED")
                {
                    var distanceFromShelter = CalculateDistance(
                        lastLocation.Latitude,
                        lastLocation.Longitude,
                        userAllocation.ShelterDetails.Latitude,
                        userAllocation.ShelterDetails.Longitude);

                    if (distanceFromShelter > 0.05) // 50 מטר
                    {
                        // המשתמש עזב את המרחב
                        await HandleUserLeftShelter(userId, userAllocation);

                        return Ok(new EmergencyStatusResponse
                        {
                            IsAlertActive = true,
                            UserStatus = "LEFT_SHELTER",
                            Message = "זוהה שעזבת את המרחב המוגן",
                            RequiresAction = true,
                            ActionType = "RETURN_TO_SHELTER"
                        });
                    }
                }

                return Ok(new EmergencyStatusResponse
                {
                    IsAlertActive = true,
                    UserStatus = userAllocation.Status,
                    ShelterId = userAllocation.ShelterId,
                    TimeInShelter = userAllocation.ArrivalTime.HasValue
                        ? DateTime.Now - userAllocation.ArrivalTime.Value
                        : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking emergency status for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "שגיאה בבדיקת סטטוס" });
            }
        }

        /// <summary>
        /// קבלת סטטוס תפוסה של מרחבים מוגנים באזור
        /// </summary>
        // Temporarily replace your GetAreaSheltersStatus with this debug version to find the issue:
        // Temporarily replace your GetAreaSheltersStatus with this debug version to find the issue:

        [HttpGet("area-shelters-status")]
        public async Task<IActionResult> GetAreaSheltersStatus(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusKm = 2.0)
        {
            try
            {
                _logger.LogInformation($"GetAreaSheltersStatus called: lat={latitude}, lon={longitude}, radius={radiusKm}");

                DBservicesShelter dbShelter = new DBservicesShelter();

                // Step 1: Try to get shelters
                List<Shelter> allShelters = null;
                try
                {
                    _logger.LogInformation("Calling GetActiveShelters()...");
                    allShelters = dbShelter.GetActiveShelters();
                    _logger.LogInformation($"Got {allShelters?.Count ?? 0} shelters from database");
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Error getting shelters from database");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Error getting shelters from database",
                        error = dbEx.Message
                    });
                }

                if (allShelters == null || !allShelters.Any())
                {
                    _logger.LogWarning("No active shelters found in database");
                    return Ok(new AreaStatusResponse
                    {
                        TotalShelters = 0,
                        AvailableShelters = 0,
                        FullShelters = 0,
                        Shelters = new List<ShelterStatusDto>()
                    });
                }

                // Step 2: Filter by distance
                List<Shelter> sheltersInRadius = null;
                try
                {
                    _logger.LogInformation($"Filtering {allShelters.Count} shelters by distance...");
                    sheltersInRadius = allShelters
                        .Where(s =>
                        {
                            var distance = CalculateDistance(latitude, longitude, s.Latitude, s.Longitude);
                            _logger.LogDebug($"Shelter {s.Name}: distance = {distance} km");
                            return distance <= radiusKm;
                        })
                        .ToList();
                    _logger.LogInformation($"Found {sheltersInRadius.Count} shelters within {radiusKm} km");
                }
                catch (Exception filterEx)
                {
                    _logger.LogError(filterEx, "Error filtering shelters by distance");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Error filtering shelters",
                        error = filterEx.Message
                    });
                }

                // Step 3: Build status for each shelter
                var shelterStatuses = new List<ShelterStatusDto>();
                foreach (var shelter in sheltersInRadius)
                {
                    try
                    {
                        _logger.LogDebug($"Processing shelter {shelter.ShelterId}: {shelter.Name}");

                        var currentOccupancy = GetCurrentOccupancy(shelter.ShelterId);
                        var status = new ShelterStatusDto
                        {
                            ShelterId = shelter.ShelterId,
                            Name = shelter.Name,
                            Address = shelter.Address,
                            Latitude = shelter.Latitude,
                            Longitude = shelter.Longitude,
                            Capacity = shelter.Capacity,
                            CurrentOccupancy = currentOccupancy,
                            AvailableSpaces = Math.Max(0, shelter.Capacity - currentOccupancy),
                            OccupancyPercentage = shelter.Capacity > 0
                                ? (double)currentOccupancy / shelter.Capacity * 100
                                : 100,
                            Status = GetShelterStatus(currentOccupancy, shelter.Capacity),
                            Distance = CalculateDistance(latitude, longitude, shelter.Latitude, shelter.Longitude)
                        };

                        shelterStatuses.Add(status);
                    }
                    catch (Exception shelterEx)
                    {
                        _logger.LogError(shelterEx, $"Error processing shelter {shelter.ShelterId}");
                        // Continue with next shelter
                    }
                }

                var response = new AreaStatusResponse
                {
                    TotalShelters = shelterStatuses.Count,
                    AvailableShelters = shelterStatuses.Count(s => s.Status == "Available"),
                    FullShelters = shelterStatuses.Count(s => s.Status == "Full"),
                    Shelters = shelterStatuses.OrderBy(s => s.Distance).ToList()
                };

                _logger.LogInformation($"Returning {response.TotalShelters} shelters");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAreaSheltersStatus");
                return StatusCode(500, new
                {
                    success = false,
                    message = "אירעה שגיאה בקבלת סטטוס המרחבים המוגנים",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        ///DEBUGGING GetAreaSheltersStatus
        /// </summary>
        // [HttpGet("area-shelters-status")]
        // public async Task<IActionResult> GetAreaSheltersStatus(
        //     [FromQuery] double latitude,
        //     [FromQuery] double longitude,
        //     [FromQuery] double radiusKm = 2.0)
        // {
        //     try
        //     {
        //         _logger.LogInformation($"GetAreaSheltersStatus called: lat={latitude}, lon={longitude}, radius={radiusKm}");

        //         // Test basic response first
        //         return Ok(new
        //         {
        //             success = true,
        //             message = "Test successful",
        //             latitude = latitude,
        //             longitude = longitude,
        //             radiusKm = radiusKm,
        //             timestamp = DateTime.Now
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error in GetAreaSheltersStatus");
        //         // Return detailed error for debugging
        //         return StatusCode(500, new
        //         {
        //             success = false,
        //             message = "אירעה שגיאה בקבלת סטטוס התראות החירום",
        //             error = ex.Message,
        //             stackTrace = ex.StackTrace // Remove this in production!
        //         });
        //     }
        // }


        #region Private Helper Methods

        private async Task<UserLocationStatus> CheckUserLocationStatus(
            int userId,
            double currentLat,
            double currentLon,
            UserAllocation allocation)
        {
            var distanceToShelter = CalculateDistance(
                currentLat, currentLon,
                allocation.ShelterDetails.Latitude,
                allocation.ShelterDetails.Longitude);

            var status = new UserLocationStatus
            {
                DistanceToShelter = distanceToShelter,
                HasArrivedAtShelter = distanceToShelter < 0.01, // 10 מטר
                IsNearShelter = distanceToShelter < 0.05, // 50 מטר
                LastUpdateTime = DateTime.Now
            };

            await _locationTrackingService.UpdateLocationStatus(userId, status);

            return status;
        }

        private async Task HandleAlertEnded(int userId, UserAllocation allocation)
        {
            try
            {
                // Release user from shelter
                await _batchAllocationService.ReleaseUserFromShelter(userId);

                // Stop tracking
                await _locationTrackingService.StopTrackingUser(userId);

                //await SaveAllocationStatistics(userId, allocation);

                // Update database to mark visit as completed
                DBservicesShelter dbs = new DBservicesShelter();
                dbs.UpdateVisitStatus(userId, allocation.ShelterId, "COMPLETED");

                // Clear any old allocations
                dbs.ClearOldAllocations(userId);

                _logger.LogInformation($"Alert ended - released user {userId} from shelter {allocation.ShelterId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling alert end for user {userId}", userId);
            }
        }

        private async Task HandleUserLeftShelter(int userId, UserAllocation allocation)
        {
            try
            {
                await _batchAllocationService.UpdateUserAllocationStatus(userId, "LEFT_SHELTER");
                _logger.LogInformation($"User {userId} left shelter {allocation.ShelterId} during active alert");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user left shelter for user {userId}", userId);
            }
        }

        // private async Task SaveAllocationStatistics(int userId, UserAllocation allocation)
        // {
        //     var stats = new AllocationStatistics
        //     {
        //         UserId = userId,
        //         ShelterId = allocation.ShelterId,
        //         AllocationTime = allocation.AllocationTime,
        //         ArrivalTime = allocation.ArrivalTime,
        //         ReleaseTime = DateTime.Now,
        //         TotalTimeInShelter = allocation.ArrivalTime.HasValue
        //             ? DateTime.Now - allocation.ArrivalTime.Value
        //             : TimeSpan.Zero,
        //         WalkingDistance = allocation.WalkingDistance,
        //         ActualWalkingTime = allocation.ActualWalkingTime
        //     };

        //     DBservicesStatistics dbs = new DBservicesStatistics();
        //     await dbs.SaveAllocationStatistics(stats);
        // }

        private int GetCurrentOccupancy(int shelterId)
        {
            DBservicesShelter dbs = new DBservicesShelter();
            return dbs.GetCurrentOccupancy(shelterId);
        }

        private string GetShelterStatus(int currentOccupancy, int capacity)
        {
            if (currentOccupancy >= capacity) return "Full";
            if (currentOccupancy >= capacity * 0.8) return "AlmostFull";
            if (currentOccupancy >= capacity * 0.5) return "Moderate";
            return "Available";
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        #endregion
    }

}