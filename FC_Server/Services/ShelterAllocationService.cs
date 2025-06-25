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
                        .Where(s => GetCurrentOccupancy(s.shelter_id) < s.capacity)
                        .ToList();

                    if (!availableShelters.Any())
                    {
                        // מציאת המרחב הקרוב ביותר עם תור המתנה
                        var nearestShelter = shelters
                            .OrderBy(s => CalculateDistance(userLat, userLon, s.latitude, s.longitude))
                            .First();

                        return new ShelterAllocationResult
                        {
                            Success = false,
                            Message = "כל המרחבים המוגנים באזור מלאים",
                            NearestShelterId = nearestShelter.shelter_id,
                            EstimatedWaitTime = CalculateEstimatedWaitTime(nearestShelter.shelter_id),
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
                        user.user_id, optimalShelter.shelter_id, 1); // alert_id = 1 לעכשיו

                    if (allocationSuccess)
                    {
                        // קבלת נתיב הליכה
                        var route = Task.Run(async () =>
                            await GetWalkingRoute(userLat, userLon,
                                optimalShelter.latitude, optimalShelter.longitude)
                        ).Result;

                        return new ShelterAllocationResult
                        {
                            Success = true,
                            Message = "הוקצה מרחב מוגן בהצלחה",
                            AllocatedShelterId = optimalShelter.shelter_id,
                            ShelterName = optimalShelter.name,
                            Distance = walkingDistances[optimalShelter.shelter_id],
                            EstimatedArrivalTime = CalculateArrivalTime(walkingDistances[optimalShelter.shelter_id]),
                            RoutePolyline = route?.OverviewPolyline,
                            RouteInstructions = route?.TextInstructions,
                            ShelterDetails = new ShelterDetailsDto
                            {
                                ShelterId = optimalShelter.shelter_id,
                                Name = optimalShelter.name,
                                Address = optimalShelter.address,
                                Latitude = optimalShelter.latitude,
                                Longitude = optimalShelter.longitude,
                                Capacity = optimalShelter.capacity,
                                IsAccessible = optimalShelter.is_accessible,
                                PetsFriendly = optimalShelter.pets_friendly
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
                _logger.LogError(ex, "Error allocating shelter for user {UserId}", user.user_id);
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
                        AllocationTime = allocation.entrance_time,
                        Status = allocation.status,
                        ShelterDetails = new ShelterDetailsDto
                        {
                            ShelterId = shelter.shelter_id,
                            Name = shelter.name,
                            Address = shelter.address,
                            Latitude = shelter.latitude,
                            Longitude = shelter.longitude
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
                var route = await GetWalkingRoute(currentLat, currentLon, destLat, destLon);
                return new RouteInfoDto
                {
                    Distance = route?.Distance ?? 0,
                    RoutePolyline = route?.OverviewPolyline,
                    AllInstructions = route?.TextInstructions,
                    CurrentInstruction = route?.TextInstructions?.FirstOrDefault(),
                    EstimatedArrivalTime = DateTime.Now.AddMinutes(route?.Duration ?? 0)
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
                .Where(s => CalculateDistance(lat, lon, s.latitude, s.longitude) <= radiusKm)
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
                Id = user.user_id,
                Age = CalculateAge(user.date_of_birth),
                Latitude = userLat,
                Longitude = userLon
            };

            var shelterDtos = shelters.Select(s => new ShelterDto
            {
                Id = s.shelter_id,
                Name = s.name,
                Latitude = s.latitude,
                Longitude = s.longitude,
                Capacity = s.capacity
            }).ToList();

            // קריאה לשירות Google Maps
            var walkingDistances = await _googleMapsService.CalculateShelterDistancesAsync(
                new List<PersonDto> { personDto },
                shelterDtos);

            // המרת התוצאות
            if (walkingDistances.ContainsKey(user.user_id.ToString()))
            {
                var userDistances = walkingDistances[user.user_id.ToString()];
                foreach (var shelter in shelters)
                {
                    if (userDistances.ContainsKey(shelter.shelter_id.ToString()))
                    {
                        distances[shelter.shelter_id] = userDistances[shelter.shelter_id.ToString()];
                    }
                    else
                    {
                        // אם אין מרחק הליכה, חשב מרחק אווירי
                        distances[shelter.shelter_id] = CalculateDistance(
                            userLat, userLon, shelter.latitude, shelter.longitude);
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
                .Where(s => walkingDistances.ContainsKey(s.shelter_id) &&
                           walkingDistances[s.shelter_id] <= MAX_DISTANCE_KM)
                .ToList();

            if (!reachableShelters.Any())
                return null;

            // מיון לפי קריטריונים:
            // 1. מרחק הליכה
            // 2. נגישות (אם רלוונטי)
            // 3. תפוסה נוכחית
            return reachableShelters
                .OrderBy(s => walkingDistances[s.shelter_id])
                .ThenByDescending(s => s.is_accessible && UserNeedsAccessibility(user))
                .ThenBy(s => GetOccupancyPercentage(s.shelter_id))
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
                        .ToList()
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
            return user.mobility_aids != null && user.mobility_aids.Any();
        }

        private double GetOccupancyPercentage(int shelterId)
        {
            var shelter = Shelter.getShelter(shelterId);
            if (shelter == null || shelter.capacity == 0) return 100;

            var currentOccupancy = GetCurrentOccupancy(shelterId);
            return (double)currentOccupancy / shelter.capacity * 100;
        }

        #endregion
    }
}