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

        // קבועים לאלגוריתם - SAME AS IN SIMULATION
        private const double MAX_TRAVEL_TIME_MINUTES = 1.0;
        private const double WALKING_SPEED_KM_PER_MINUTE = 0.6;
        private const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE;
        private const double CUBE_SIZE_KM = 0.2; // 200 מטר
        private const double CUBE_SIZE_LAT = CUBE_SIZE_KM / 111.0;
        private const double CUBE_SIZE_LON_APPROX = CUBE_SIZE_KM / 85.0; // Approximation for Beer Sheva latitude

        public ShelterAllocationService(IGoogleMapsService googleMapsService, ILogger<ShelterAllocationService> logger)
        {
            _googleMapsService = googleMapsService;
            _logger = logger;
        }

        /// <summary>
        /// הקצאת מרחב מוגן למשתמש בודד
        /// USING THE SAME ALGORITHM AS SIMULATION
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
                // First, perform all synchronous operations within the lock
                AssignmentOption bestOption = null;
                List<AssignmentOption> alternativeOptions = new List<AssignmentOption>();

                lock (_allocationLock)
                {
                    _logger.LogInformation($"Starting shelter allocation for user {user.UserId} at ({userLat}, {userLon})");

                    // STEP 1: GET ALL ACTIVE SHELTERS (same as simulation)
                    var allShelters = Shelter.getActiveShelters();

                    /////////////////////////
                    // Start debugging logs//
                    /////////////////////////

                    _logger.LogInformation($"Got {allShelters.Count} shelters from database");
                    // Log first 5 shelters
                    foreach (var s in allShelters.Take(5))
                    {
                        _logger.LogInformation($"Shelter: ID={s.ShelterId}, Name='{s.Name}', Lat={s.Latitude}, Lon={s.Longitude}, Capacity={s.Capacity}");
                    }

                    // Check if "holaaa" is always first
                    var holaaa = allShelters.FirstOrDefault(s => s.Name.Contains("holaaa"));
                    if (holaaa != null)
                    {
                        _logger.LogInformation($"Found 'holaaa' shelter at position {allShelters.IndexOf(holaaa)} in the list");
                    }
                    ///////////////////////
                    // End debugging logs//
                    ///////////////////////

                    if (!allShelters.Any())
                    {
                        return new ShelterAllocationResult
                        {
                            Success = false,
                            Message = "לא נמצאו מרחבים מוגנים פעילים",
                            RecommendedAction = "נסה שוב מאוחר יותר"
                        };
                    }

                    // STEP 2: BUILD CUBE INDEX FOR SHELTERS (same as simulation)
                    var cubeToShelters = BuildCubeToShelterIndex(allShelters, centerLat, centerLon);

                    // STEP 3: GET SHELTERS IN SURROUNDING CUBES (same as simulation)
                    var nearbyShelters = GetSheltersInSurroundingCubes(
                        userLat, userLon, allShelters, cubeToShelters, centerLat, centerLon);

                    _logger.LogInformation($"Found {nearbyShelters.Count} shelters in nearby cubes");

                    // ========== START DEBUG ==========
                    foreach (var s in nearbyShelters.Take(5))
                    {
                        _logger.LogInformation($"Nearby shelter: ID={s.ShelterId}, Name='{s.Name}'");
                    }
                    // ========== END DEBUG ============

                    if (!nearbyShelters.Any())
                    {
                        return new ShelterAllocationResult
                        {
                            Success = false,
                            Message = "לא נמצאו מרחבים מוגנים באזור",
                            RecommendedAction = "נסה להרחיב את רדיוס החיפוש"
                        };
                    }

                    // STEP 4: CREATE PRIORITY QUEUE OF OPTIONS (same as simulation)
                    var pq = new PriorityQueue<AssignmentOption, double>();

                    foreach (var shelter in nearbyShelters)
                    {
                        // Check current capacity
                        var currentOccupancy = GetCurrentOccupancy(shelter.ShelterId);
                        if (currentOccupancy >= shelter.Capacity)
                        {
                            _logger.LogInformation($"Shelter {shelter.ShelterId} is full ({currentOccupancy}/{shelter.Capacity})");
                            continue;
                        }

                        // Calculate distance
                        double distance = CalculateDistance(
                            userLat, userLon,
                            shelter.Latitude, shelter.Longitude);

                        // Only consider shelters within maximum distance
                        if (distance <= MAX_DISTANCE_KM)
                        {
                            var option = new AssignmentOption
                            {
                                PersonId = user.UserId,
                                ShelterId = shelter.ShelterId,
                                Distance = distance,
                                IsReachable = true,
                                VulnerabilityScore = CalculateVulnerabilityScore(user.Birthday)
                            };

                            // Calculate priority (same as simulation)
                            double priority = distance;

                            // Adjust priority for vulnerable people
                            priority -= option.VulnerabilityScore * 0.01;

                            pq.Enqueue(option, priority);
                            _logger.LogInformation($"Added shelter {shelter.ShelterId} '{shelter.Name}' to options (distance: {distance * 1000:F0}m, priority: {priority:F4})");
                        }
                    }

                    if (pq.Count == 0)
                    {
                        return new ShelterAllocationResult
                        {
                            Success = false,
                            Message = "לא נמצא מרחב מוגן מתאים במרחק הליכה",
                            RecommendedAction = "חפש מחסה חלופי בסביבה"
                        };
                    }

                    // STEP 5: PROCESS OPTIONS TO FIND BEST AVAILABLE
                    while (pq.Count > 0)
                    {
                        var option = pq.Dequeue();

                        // Double-check capacity at allocation time
                        var currentOccupancy = GetCurrentOccupancy(option.ShelterId);
                        var shelter = nearbyShelters.First(s => s.ShelterId == option.ShelterId);

                        if (currentOccupancy < shelter.Capacity)
                        {
                            // Try to allocate
                            DBservicesShelter dbShelter = new DBservicesShelter();
                            var allocationSuccess = dbShelter.AllocateUserToShelter(
                                user.UserId, option.ShelterId, 1); // alert_id = 1 for now

                            if (allocationSuccess)
                            {
                                bestOption = option;
                                _logger.LogInformation($"Successfully allocated user {user.UserId} to shelter {option.ShelterId}");
                                break;
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to allocate user {user.UserId} to shelter {option.ShelterId}, trying next option");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Shelter {option.ShelterId} became full, trying next option");
                        }

                        // Keep some alternatives in case we need them
                        if (alternativeOptions.Count < 3)
                        {
                            alternativeOptions.Add(option);
                        }
                    }
                } // End of lock

                // Now handle the async operations outside the lock
                if (bestOption != null)
                {
                    var shelter = Shelter.getShelter(bestOption.ShelterId);

                    // Get walking route (async operation)
                    var route = await GetWalkingRoute(userLat, userLon,
                        shelter.Latitude, shelter.Longitude);

                    return new ShelterAllocationResult
                    {
                        Success = true,
                        Message = "הוקצה מרחב מוגן בהצלחה",
                        AllocatedShelterId = bestOption.ShelterId,
                        ShelterName = shelter.Name,
                        Distance = bestOption.Distance,
                        EstimatedArrivalTime = CalculateArrivalTime(bestOption.Distance),
                        RoutePolyline = route?.OverviewPolyline,
                        RouteInstructions = route?.TextInstructions,
                        ShelterDetails = new ShelterDetailsDto
                        {
                            ShelterId = shelter.ShelterId,
                            Name = shelter.Name,
                            Address = shelter.Address,
                            Latitude = shelter.Latitude,
                            Longitude = shelter.Longitude,
                            Capacity = shelter.Capacity,
                            IsAccessible = shelter.IsAccessible,
                            PetsFriendly = shelter.PetsFriendly
                        }
                    };
                }

                // All options exhausted
                return new ShelterAllocationResult
                {
                    Success = false,
                    Message = "כל המרחבים המוגנים הקרובים מלאים",
                    RecommendedAction = "נסה שוב או חפש מחסה חלופי"
                };
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
        /// הקצאת מרחב מוגן למשתמש
        /// </summary>
        public async Task<AllocationResult> AllocateShelterForUser(
            User user,
            double userLat,
            double userLon,
            double centerLat,
            double centerLon)
        {
            // Call your updated method
            var result = await AllocateShelterForUserAsync(user, userLat, userLon, centerLat, centerLon);

            // Convert ShelterAllocationResult to AllocationResult
            return new AllocationResult
            {
                Success = result.Success,
                Message = result.Message,
                AllocatedShelterId = result.AllocatedShelterId,
                ShelterName = result.ShelterName,
                Distance = result.Distance,
                EstimatedArrivalTime = result.EstimatedArrivalTime,
                RoutePolyline = result.RoutePolyline,
                RouteInstructions = result.RouteInstructions,
                ShelterDetails = result.ShelterDetails,
                RecommendedAction = result.RecommendedAction
            };
        }

        /// <summary>
        /// בדיקת הקצאה פעילה למשתמש
        /// </summary>
        public async Task<UserAllocation> GetActiveAllocationForUser(int userId)
        {
            try
            {
                DBservicesShelter dbs = new DBservicesShelter();
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
                    CurrentInstruction = route?.TextInstructions?.FirstOrDefault(),
                    EstimatedArrivalTime = DateTime.Now.AddMinutes(route?.Distance ?? 0 / WALKING_SPEED_KM_PER_MINUTE)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating route");
                return null;
            }
        }

        /// <summary>
        /// קבלת נתיב מעודכן (wrapper for EmergencyResponseController)
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
                    EstimatedArrivalTime = DateTime.Now.AddMinutes(route?.Distance ?? 0 / WALKING_SPEED_KM_PER_MINUTE)
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

        #region Cube-based Optimization Methods (SAME AS SIMULATION)

        /// <summary>
        /// Get the cube key for a given coordinate
        /// </summary>
        private string GetCubeKey(double lat, double lon, double centerLat, double centerLon)
        {
            int latIndex = (int)((lat - centerLat) / CUBE_SIZE_LAT);
            int lonIndex = (int)((lon - centerLon) / CUBE_SIZE_LON_APPROX);
            return $"{latIndex},{lonIndex}";
        }

        /// <summary>
        /// Get all 9 surrounding cubes (including the center cube)
        /// </summary>
        private List<string> GetSurroundingCubes(double lat, double lon, double centerLat, double centerLon)
        {
            var cubes = new List<string>();
            int centerLatIndex = (int)((lat - centerLat) / CUBE_SIZE_LAT);
            int centerLonIndex = (int)((lon - centerLon) / CUBE_SIZE_LON_APPROX);

            for (int dLat = -1; dLat <= 1; dLat++)
            {
                for (int dLon = -1; dLon <= 1; dLon++)
                {
                    cubes.Add($"{centerLatIndex + dLat},{centerLonIndex + dLon}");
                }
            }

            return cubes;
        }

        /// <summary>
        /// Build an index mapping cubes to shelters
        /// </summary>
        private Dictionary<string, List<int>> BuildCubeToShelterIndex(List<Shelter> shelters, double centerLat, double centerLon)
        {
            var cubeToShelters = new Dictionary<string, List<int>>();

            foreach (var shelter in shelters)
            {
                string cubeKey = GetCubeKey(shelter.Latitude, shelter.Longitude, centerLat, centerLon);

                if (!cubeToShelters.ContainsKey(cubeKey))
                {
                    cubeToShelters[cubeKey] = new List<int>();
                }

                cubeToShelters[cubeKey].Add(shelter.ShelterId);
            }

            _logger.LogInformation($"Built cube index with {cubeToShelters.Count} cubes containing shelters");
            return cubeToShelters;
        }

        /// <summary>
        /// Get all shelters in the surrounding cubes for a person
        /// </summary>
        private List<Shelter> GetSheltersInSurroundingCubes(
            double personLat,
            double personLon,
            List<Shelter> allShelters,
            Dictionary<string, List<int>> cubeToShelters,
            double centerLat,
            double centerLon)
        {
            var surroundingCubes = GetSurroundingCubes(personLat, personLon, centerLat, centerLon);
            var shelterIds = new HashSet<int>();

            foreach (var cubeKey in surroundingCubes)
            {
                if (cubeToShelters.ContainsKey(cubeKey))
                {
                    foreach (var shelterId in cubeToShelters[cubeKey])
                    {
                        shelterIds.Add(shelterId);
                    }
                }
            }

            return allShelters.Where(s => shelterIds.Contains(s.ShelterId)).ToList();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Assignment option class (same as simulation)
        /// </summary>
        private class AssignmentOption
        {
            public int PersonId { get; set; }
            public int ShelterId { get; set; }
            public double Distance { get; set; }
            public bool IsReachable { get; set; }
            public int VulnerabilityScore { get; set; }
        }

        /// <summary>
        /// Calculate vulnerability score based on user age (from birthday)
        /// </summary>
        private int CalculateVulnerabilityScore(DateTime? birthday)
        {
            if (!birthday.HasValue)
            {
                return 6; // Default adult score if no birthday
            }

            int age = DateTime.Now.Year - birthday.Value.Year;
            if (DateTime.Now.DayOfYear < birthday.Value.DayOfYear)
                age--; // Adjust if birthday hasn't occurred this year

            if (age >= 70)
                return 10; // Elderly (70+): highest priority
            else if (age <= 12)
                return 8; // Children (0-12): second highest priority
            else
                return 6; // Adults (13-69): normal priority
        }

        private DateTime CalculateArrivalTime(double distanceKm)
        {
            double walkingTimeMinutes = distanceKm / WALKING_SPEED_KM_PER_MINUTE;
            return DateTime.Now.AddMinutes(walkingTimeMinutes);
        }

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

        private double GetOccupancyPercentage(int shelterId)
        {
            var shelter = Shelter.getShelter(shelterId);
            if (shelter == null || shelter.Capacity == 0) return 100;

            var currentOccupancy = GetCurrentOccupancy(shelterId);
            return (double)currentOccupancy / shelter.Capacity * 100;
        }

        private bool UserNeedsAccessibility(User user)
        {
            // This would need to be implemented based on user profile
            // For now, return false
            return false;
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

        private async Task<RouteInfo> GetWalkingRoute(
            double originLat, double originLon,
            double destLat, double destLon)
        {
            try
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
                        Distance = (leg?.Distance?.Value ?? 0) / 1000.0, // Convert meters to km
                        //Duration = leg?.Duration?.Value ?? 0
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting walking route");
                return null;
            }
        }

        private int CalculateAge(DateTime? birthday)
        {
            if (!birthday.HasValue)
            {
                return 30; // Default age if no birthday
            }

            int age = DateTime.Now.Year - birthday.Value.Year;
            if (DateTime.Now.DayOfYear < birthday.Value.DayOfYear)
                age--; // Adjust if birthday hasn't occurred this year

            return age;
        }

        private TimeSpan CalculateEstimatedWaitTime(int shelterId)
        {
            // This would need real implementation based on shelter turnover rate
            return TimeSpan.FromMinutes(5);
        }

        private async Task<Dictionary<int, double>> CalculateWalkingDistances(
            User user, double userLat, double userLon, List<Shelter> shelters)
        {
            var distances = new Dictionary<int, double>();

            // null/empty check
            if (user == null)
            {
                _logger.LogError("User is null in CalculateWalkingDistances");
                // Return aerial distances as fallback
                foreach (var shelter in shelters)
                {
                    distances[shelter.ShelterId] = CalculateDistance(userLat, userLon, shelter.Latitude, shelter.Longitude);
                }
                return distances;
            }

            // המרת למבנה הנתונים של ServerSimulation
            var personDto = new PersonDto
            {
                Id = user.UserId,
                Age = CalculateAge(user.Birthday), // Use helper method for age calculation
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

            try
            {
                // קריאה ל-Google Maps לחישוב מרחקי הליכה
                var walkingDistances = await _googleMapsService.CalculateShelterDistancesAsync(
                    new List<PersonDto> { personDto },
                    shelterDtos
                );

                // המרת התוצאות למבנה הנתונים המבוקש
                if (walkingDistances.ContainsKey(personDto.Id.ToString()))
                {
                    var userDistances = walkingDistances[personDto.Id.ToString()];
                    foreach (var kvp in userDistances)
                    {
                        if (int.TryParse(kvp.Key, out int shelterId))
                        {
                            distances[shelterId] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Google Maps service, falling back to aerial distances");
                // Fallback to aerial distances
                foreach (var shelter in shelters)
                {
                    distances[shelter.ShelterId] = CalculateDistance(
                        userLat, userLon, shelter.Latitude, shelter.Longitude);
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

        #endregion
    }
}