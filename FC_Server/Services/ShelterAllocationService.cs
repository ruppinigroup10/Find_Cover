using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FC_Server.Models;
using FC_Server.DAL;
using Microsoft.Extensions.Logging;

namespace FC_Server.Services
{
    /// <summary>
    /// שירות להקצאת מרחבים מוגנים למשתמשים בזמן אזעקה
    /// משתמש באלגוריתם אופטימלי עם תמיכה ב-Google Maps
    /// </summary>
    public class ShelterAllocationService
    {
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ILogger<ShelterAllocationService> _logger;
        private readonly object _allocationLock = new object();

        // קבועים לאלגוריתם
        private const double MAX_TRAVEL_TIME_MINUTES = 1.0;
        private const double WALKING_SPEED_KM_PER_MINUTE = 0.6;
        private const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE;
        private const double CUBE_SIZE_KM = 0.2; // 200 מטר
        private const double CUBE_SIZE_LAT = CUBE_SIZE_KM / 111.0;

        public ShelterAllocationService(IGoogleMapsService googleMapsService, ILogger<ShelterAllocationService> logger)
        {
            _googleMapsService = googleMapsService;
            _logger = logger;
        }

        /// <summary>
        /// הקצאת מרחב מוגן למשתמש בודד
        /// </summary>
        public async Task<ShelterAllocationResult> AllocateShelterForUserAsync(
            User user,
            double userLat,
            double userLon,
            double centerLat,
            double centerLon)
        {
            try
            {
                lock (_allocationLock)
                {
                    // קבלת כל המרחבים המוגנים הפעילים באזור
                    var shelters = GetActiveSheltersInArea(userLat, userLon, MAX_DISTANCE_KM);

                    if (!shelters.Any())
                    {
                        return new ShelterAllocationResult
                        {
                            Success = false,
                            Message = "לא נמצאו מרחבים מוגנים באזור",
                            RecommendedAction = "נסה להרחיב את רדיוס החיפוש"
                        };
                    }

                    // בדיקת תפוסה עדכנית
                    var availableShelters = shelters
                        .Where(s => GetCurrentOccupancy(s.ShelterId) < s.Capacity)
                        .ToList();

                    if (!availableShelters.Any())
                    {
                        // מציאת המרחב הקרוב ביותר עם תור המתנה
                        var nearestShelter = shelters
                            .OrderBy(s => CalculateDistance(userLat, userLon, s.Latitude, s.Longitude))
                            .First();

                        return new ShelterAllocationResult
                        {
                            Success = false,
                            Message = "כל המרחבים המוגנים באזור מלאים",
                            NearestShelterId = nearestShelter.ShelterId,
                            EstimatedWaitTime = CalculateEstimatedWaitTime(nearestShelter.ShelterId),
                            RecommendedAction = "המתן בתור או חפש מחסה חלופי"
                        };
                    }

                    // חישוב מרחקי הליכה אמיתיים עם Google Maps
                    var walkingDistances = Task.Run(async () =>
                        await CalculateWalkingDistances(user, userLat, userLon, availableShelters)
                    ).Result;

                    // מציאת המרחב האופטימלי
                    var optimalShelter = FindOptimalShelter(
                        user, userLat, userLon, availableShelters, walkingDistances);

                    if (optimalShelter == null)
                    {
                        return new ShelterAllocationResult
                        {
                            Success = false,
                            Message = "לא נמצא מרחב מוגן מתאים במרחק הליכה",
                            RecommendedAction = "חפש מחסה חלופי בסביבה"
                        };
                    }

                    // ביצוע ההקצאה
                    DBservicesShelter dbShelter = new DBservicesShelter();
                    var allocationSuccess = dbShelter.AllocateUserToShelter(
                        user.UserId, optimalShelter.ShelterId, 1); // alert_id = 1 לעכשיו

                    if (allocationSuccess)
                    {
                        // קבלת נתיב הליכה
                        var route = Task.Run(async () =>
                            await GetWalkingRoute(userLat, userLon,
                                optimalShelter.Latitude, optimalShelter.Longitude)
                        ).Result;

                        return new ShelterAllocationResult
                        {
                            Success = true,
                            Message = "הוקצה מרחב מוגן בהצלחה",
                            AllocatedShelterId = optimalShelter.ShelterId,
                            ShelterName = optimalShelter.Name,
                            Distance = walkingDistances[optimalShelter.ShelterId],
                            EstimatedArrivalTime = CalculateArrivalTime(walkingDistances[optimalShelter.ShelterId]),
                            RoutePolyline = route?.OverviewPolyline,
                            RouteInstructions = route?.TextInstructions,
                            ShelterDetails = new ShelterDetailsDto
                            {
                                ShelterId = optimalShelter.ShelterId,
                                Name = optimalShelter.Name,
                                Address = optimalShelter.Address,
                                Latitude = optimalShelter.Latitude,
                                Longitude = optimalShelter.Longitude,
                                Capacity = optimalShelter.Capacity,
                                IsAccessible = optimalShelter.IsAccessible,
                                PetsFriendly = optimalShelter.PetsFriendly
                            }
                        };
                    }
                    else
                    {
                        return new ShelterAllocationResult
                        {
                            Success = false,
                            Message = "המרחב המוגן התמלא ברגע האחרון",
                            RecommendedAction = "נסה שוב"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating shelter for user {UserId}", user.UserId);
                return new ShelterAllocationResult
                {
                    Success = false,
                    Message = "אירעה שגיאה בהקצאת מרחב מוגן",
                    RecommendedAction = "נסה שוב או פנה לתמיכה"
                };
            }
        }

        /// <summary>
        /// בדיקת הקצאה פעילה למשתמש
        /// </summary>
        public async Task<UserAllocation> GetActiveAllocationForUser(int userId)
        {
            try
            {
                DBservicesShelter dbs = new DBservicesShelter();
                // כאן צריך לממש שאילתה לקבלת הקצאה פעילה מ-shelter_visit
                // לדוגמה:
                var allocation = dbs.GetActiveUserAllocation(userId);
                if (allocation != null)
                {
                    var shelter = Shelter.getShelter(allocation.shelter_id);
                    return new UserAllocation
                    {
                        UserId = userId,
                        ShelterId = allocation.shelter_id,
                        AlertId = allocation.alert_id,
                        AllocationTime = allocation.arrival_time ?? DateTime.Now,
                        Status = allocation.status,
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
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active allocation for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// עדכון סטטוס משתמש שהגיע
        /// </summary>
        public async Task MarkUserAsArrived(int userId, int shelterId)
        {
            try
            {
                DBservicesShelter dbs = new DBservicesShelter();
                dbs.UpdateVisitStatus(userId, shelterId, "ARRIVED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking user as arrived");
            }
        }

        /// <summary>
        /// חישוב נתיב מחדש
        /// </summary>
        public async Task<RouteInfoDto> RecalculateRoute(int userId, double currentLat, double currentLon)
        {
            try
            {
                var allocation = await GetActiveAllocationForUser(userId);
                if (allocation == null) return null;

                var route = await GetWalkingRoute(
                    currentLat, currentLon,
                    allocation.ShelterDetails.Latitude,
                    allocation.ShelterDetails.Longitude);

                return new RouteInfoDto
                {
                    Distance = route?.Distance ?? 0,
                    RoutePolyline = route?.OverviewPolyline,
                    AllInstructions = route?.TextInstructions,
                    CurrentInstruction = route?.TextInstructions?.FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating route");
                return null;
            }
        }

        /// <summary>
        /// קבלת נתיב מעודכן
        /// </summary>
        public async Task<RouteInfoDto> GetUpdatedRoute(
            double currentLat, double currentLon,
            double destLat, double destLon)
        {
            try
            {
                // Call Google Maps API directly to get full route information
                var request = new DirectionsRequest
                {
                    Origin = new LocationPoint(currentLat, currentLon, "user"),
                    Destination = new LocationPoint(destLat, destLon, "shelter"),
                    Mode = TravelMode.Walking
                };

                var response = await _googleMapsService.GetDirectionsAsync(request);

                if (response.Success && response.Routes?.Any() == true)
                {
                    var route = response.Routes.First();
                    var leg = route.Legs?.FirstOrDefault();

                    return new RouteInfoDto
                    {
                        Distance = (leg?.Distance?.Value ?? 0) / 1000.0, // Convert meters to km
                        RoutePolyline = route.OverviewPolyline,
                        AllInstructions = leg?.Steps?.Select(s => s.HtmlInstructions).ToList(),
                        CurrentInstruction = leg?.Steps?.FirstOrDefault()?.HtmlInstructions,
                        EstimatedArrivalTime = DateTime.Now.AddSeconds(leg?.Duration?.Value ?? 0)
                    };
                }

                return new RouteInfoDto
                {
                    Distance = 0,
                    RoutePolyline = string.Empty,
                    AllInstructions = new List<string>(),
                    CurrentInstruction = "Unable to calculate route"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting updated route");
                return null;
            }
        }

        /// <summary>
        /// שחרור משתמש ממרחב
        /// </summary>
        public async Task ReleaseUserFromShelter(int userId)
        {
            try
            {
                DBservicesShelter dbs = new DBservicesShelter();
                dbs.ReleaseUserFromShelter(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing user from shelter");
            }
        }

        /// <summary>
        /// עדכון סטטוס הקצאה
        /// </summary>
        public async Task UpdateUserAllocationStatus(int userId, string status)
        {
            try
            {
                DBservicesShelter dbs = new DBservicesShelter();
                dbs.UpdateAllocationStatus(userId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating allocation status");
            }
        }

        #region Helper Methods

        private List<Shelter> GetActiveSheltersInArea(double lat, double lon, double radiusKm)
        {
            return Shelter.getActiveShelters()
                .Where(s => CalculateDistance(lat, lon, s.Latitude, s.Longitude) <= radiusKm)
                .ToList();
        }

        private int GetCurrentOccupancy(int shelterId)
        {
            DBservicesShelter dbs = new DBservicesShelter();
            return dbs.GetCurrentOccupancy(shelterId);
        }

        private async Task<Dictionary<int, double>> CalculateWalkingDistances(
            User user, double userLat, double userLon, List<Shelter> shelters)
        {
            var distances = new Dictionary<int, double>();

            // המרת למבנה הנתונים של ServerSimulation
            var personDto = new PersonDto
            {
                Id = user.UserId,
                Age = CalculateAge(user.CreatedAt),
                Latitude = userLat,
                Longitude = userLon
            };

            var shelterDtos = shelters.Select(s => new ShelterDto
            {
                Id = s.ShelterId,
                Name = s.Name,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Capacity = s.Capacity
            }).ToList();

            // קריאה לשירות Google Maps
            var walkingDistances = await _googleMapsService.CalculateShelterDistancesAsync(
                new List<PersonDto> { personDto },
                shelterDtos);

            // המרת התוצאות
            if (walkingDistances.ContainsKey(user.UserId.ToString()))
            {
                var userDistances = walkingDistances[user.UserId.ToString()];
                foreach (var shelter in shelters)
                {
                    if (userDistances.ContainsKey(shelter.ShelterId.ToString()))
                    {
                        distances[shelter.ShelterId] = userDistances[shelter.ShelterId.ToString()];
                    }
                    else
                    {
                        // אם אין מרחק הליכה, חשב מרחק אווירי
                        distances[shelter.ShelterId] = CalculateDistance(
                            userLat, userLon, shelter.Latitude, shelter.Longitude);
                    }
                }
            }

            return distances;
        }

        private Shelter FindOptimalShelter(
            User user, double userLat, double userLon,
            List<Shelter> availableShelters,
            Dictionary<int, double> walkingDistances)
        {
            // סינון מרחבים במרחק הליכה סביר
            var reachableShelters = availableShelters
                .Where(s => walkingDistances.ContainsKey(s.ShelterId) &&
                           walkingDistances[s.ShelterId] <= MAX_DISTANCE_KM)
                .ToList();

            if (!reachableShelters.Any())
                return null;

            // מיון לפי קריטריונים:
            // 1. מרחק הליכה
            // 2. נגישות (אם רלוונטי)
            // 3. תפוסה נוכחית
            return reachableShelters
                .OrderBy(s => walkingDistances[s.ShelterId])
                .ThenByDescending(s => s.IsAccessible && UserNeedsAccessibility(user))
                .ThenBy(s => GetOccupancyPercentage(s.ShelterId))
                .FirstOrDefault();
        }

        private async Task<RouteInfo> GetWalkingRoute(
            double originLat, double originLon,
            double destLat, double destLon)
        {
            var request = new DirectionsRequest
            {
                Origin = new LocationPoint(originLat, originLon, "user"),
                Destination = new LocationPoint(destLat, destLon, "shelter"),
                Mode = TravelMode.Walking
            };

            var response = await _googleMapsService.GetDirectionsAsync(request);

            if (response.Success && response.Routes?.Any() == true)
            {
                var route = response.Routes.First();
                var leg = route.Legs?.FirstOrDefault();

                return new RouteInfo
                {
                    OverviewPolyline = route.OverviewPolyline,
                    TextInstructions = leg?.Steps?
                        .Select(s => s.HtmlInstructions)
                        .ToList(),
                    Distance = (leg?.Distance?.Value ?? 0) / 1000.0 // Convert meters to km
                };
            }

            return null;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // רדיוס כדור הארץ בק"מ
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        private TimeSpan CalculateEstimatedWaitTime(int shelterId)
        {
            // לוגיקה לחישוב זמן המתנה משוער
            return TimeSpan.FromMinutes(5);
        }

        private DateTime CalculateArrivalTime(double distanceKm)
        {
            var walkingTimeMinutes = distanceKm / WALKING_SPEED_KM_PER_MINUTE;
            return DateTime.Now.AddMinutes(walkingTimeMinutes);
        }

        private bool UserNeedsAccessibility(User user)
        {
            // Get user preferences to check if accessibility is needed
            var preferences = UserPreferences.GetUserPreferences(user.UserId);
            return preferences?.AccessibilityNeeded ?? false;
        }

        private double GetOccupancyPercentage(int shelterId)
        {
            var shelter = Shelter.getShelter(shelterId);
            if (shelter == null || shelter.Capacity == 0) return 100;

            var currentOccupancy = GetCurrentOccupancy(shelterId);
            return (double)currentOccupancy / shelter.Capacity * 100;
        }

        #endregion
    }
}