using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FC_Server.Models;
using FC_Server.DAL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace FC_Server.Services
{
    /// <summary>
    /// Service that collects shelter allocation requests and processes them in batches
    /// using the same optimal algorithm as the simulation
    /// </summary>
    public class BatchShelterAllocationService : BackgroundService
    {
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ILogger<BatchShelterAllocationService> _logger;
        private readonly ConcurrentQueue<AllocationRequest> _pendingRequests = new();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<AllocationResult>> _completionSources = new();
        private readonly ManualResetEventSlim _processingSignal = new ManualResetEventSlim(false);

        // Batch processing settings
        private readonly TimeSpan _batchInterval = TimeSpan.FromSeconds(1); // Process every 1 second
        private readonly TimeSpan _maxWaitTime = TimeSpan.FromSeconds(3); // Max wait 3 seconds
        private readonly int _minBatchSize = 1; // Process immediately with 1 user
        private readonly int _maxBatchSize = 100; // Maximum requests to process at once

        // Constants from simulation
        private const double MAX_TRAVEL_TIME_MINUTES = 1.0;
        private const double WALKING_SPEED_KM_PER_MINUTE = 0.6;
        private const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE;
        private const double CUBE_SIZE_KM = 0.2;
        private const double CUBE_SIZE_LAT = CUBE_SIZE_KM / 111.0;
        private const double CUBE_SIZE_LON_APPROX = CUBE_SIZE_KM / 85.0;

        public BatchShelterAllocationService(
            IGoogleMapsService googleMapsService,
            ILogger<BatchShelterAllocationService> logger)
        {
            _googleMapsService = googleMapsService;
            _logger = logger;
            _logger.LogWarning("========== BatchShelterAllocationService CREATED ==========");
        }

        /// <summary>
        /// Public method called by the controller to request shelter allocation
        /// </summary>
        public async Task<AllocationResult> RequestShelterAllocation(
    User user, double userLat, double userLon,
    double alertLat, double alertLon, int alertId)
        {
            _logger.LogWarning($"========== RequestShelterAllocation called for user {user.UserId} ==========");

            // Create request object
            var request = new AllocationRequest
            {
                User = user,
                Latitude = userLat,
                Longitude = userLon,
                AlertLatitude = alertLat,
                AlertLongitude = alertLon,
                AlertId = alertId,
                RequestTime = DateTime.Now
            };

            // Create completion source for this request
            var completionSource = new TaskCompletionSource<AllocationResult>();
            _completionSources[user.UserId] = completionSource;

            // Add to queue
            _pendingRequests.Enqueue(request);
            _logger.LogInformation($"User {user.UserId} added to allocation queue. Queue size: {_pendingRequests.Count}");

            // IMPORTANT: Signal the background service to process immediately
            // Add a manual reset event to trigger immediate processing
            _processingSignal.Set();

            // Increase timeout to 10 seconds for debugging
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogError($"TIMEOUT waiting for allocation for user {user.UserId} after 10 seconds");
                _completionSources.TryRemove(user.UserId, out _);

                // Instead of fallback, try direct allocation
                return await DirectAllocation(request);
            }

            return await completionSource.Task;
        }

        /// <summary>
        /// Direct allocation method that tries to find a shelter immediately
        /// </summary>
        private async Task<AllocationResult> DirectAllocation(AllocationRequest request)
        {
            try
            {
                _logger.LogWarning($"Using direct allocation for user {request.User.UserId}");

                var dbShelter = new DBservicesShelter();
                var allShelters = dbShelter.GetActiveShelters();

                if (!allShelters.Any())
                {
                    return new AllocationResult
                    {
                        Success = false,
                        Message = "No active shelters available"
                    };
                }

                // Find nearest shelter with capacity
                var nearestShelter = allShelters
                    .Select(s => new
                    {
                        Shelter = s,
                        Distance = CalculateDistance(request.Latitude, request.Longitude, s.Latitude, s.Longitude),
                        CurrentOccupancy = dbShelter.GetCurrentOccupancy(s.ShelterId)
                    })
                    .Where(x => x.CurrentOccupancy < x.Shelter.Capacity && x.Distance <= MAX_DISTANCE_KM)
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                if (nearestShelter == null)
                {
                    return new AllocationResult
                    {
                        Success = false,
                        Message = "No available shelter within walking distance",
                        RecommendedAction = "Seek alternative shelter"
                    };
                }

                // Allocate directly
                var allocated = dbShelter.AllocateUserToShelter(
                    request.User.UserId,
                    nearestShelter.Shelter.ShelterId,
                    request.AlertId);

                if (allocated)
                {
                    var route = await GetWalkingRoute(
                        request.Latitude, request.Longitude,
                        nearestShelter.Shelter.Latitude, nearestShelter.Shelter.Longitude);

                    return new AllocationResult
                    {
                        Success = true,
                        Message = "Allocated successfully (direct)",
                        AllocatedShelterId = nearestShelter.Shelter.ShelterId,
                        ShelterName = nearestShelter.Shelter.Name,
                        Distance = nearestShelter.Distance,
                        EstimatedArrivalTime = CalculateArrivalTime(nearestShelter.Distance),
                        RoutePolyline = route?.OverviewPolyline,
                        RouteInstructions = route?.TextInstructions,
                        ShelterDetails = ConvertToShelterDetailsDto(nearestShelter.Shelter)
                    };
                }

                return new AllocationResult
                {
                    Success = false,
                    Message = "Failed to allocate shelter"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct allocation");
                return new AllocationResult
                {
                    Success = false,
                    Message = "Allocation error - please try again",
                    RecommendedAction = "Retry or seek alternative shelter"
                };
            }
        }

        /// <summary>
        /// Background service that processes batches periodically
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogWarning("========== BatchShelterAllocationService.ExecuteAsync called ==========");

                // Wait a bit for all services to be ready
                await Task.Delay(5000, stoppingToken);

                _logger.LogWarning("========== BatchShelterAllocationService background loop starting ==========");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Wait for signal OR timeout
                        var signaled = await Task.Run(() =>
                            _processingSignal.Wait(_batchInterval, stoppingToken),
                            stoppingToken);

                        // Reset the signal
                        _processingSignal.Reset();

                        if (_pendingRequests.Count > 0)
                        {
                            _logger.LogWarning($"========== PROCESSING BATCH: {_pendingRequests.Count} requests (signaled: {signaled}) ==========");
                            await ProcessBatch();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Batch processing loop canceled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in batch processing loop");
                        // Continue the loop even if there's an error
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BatchShelterAllocationService ExecuteAsync was canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in BatchShelterAllocationService ExecuteAsync");
                throw;
            }
        }

        /// <summary>
        /// Process a batch of allocation requests using the simulation algorithm
        /// </summary>
        private async Task ProcessBatch()
        {
            // Collect requests from queue
            var requests = new List<AllocationRequest>();
            var processedCount = 0;

            while (_pendingRequests.TryDequeue(out var request) && processedCount < _maxBatchSize)
            {
                // Skip old requests
                if (DateTime.Now - request.RequestTime > _maxWaitTime)
                {
                    _logger.LogWarning($"Skipping old request for user {request.User.UserId}");
                    if (_completionSources.TryRemove(request.User.UserId, out var tcs))
                    {
                        tcs.SetResult(new AllocationResult
                        {
                            Success = false,
                            Message = "Request timeout"
                        });
                    }
                    continue;
                }

                requests.Add(request);
                processedCount++;
            }

            if (requests.Count == 0)
            {
                return;
            }

            _logger.LogInformation($"Processing batch of {requests.Count} allocation requests");

            try
            {
                // Get all active shelters
                var dbShelter = new DBservicesShelter();
                var allShelters = dbShelter.GetActiveShelters();

                if (!allShelters.Any())
                {
                    CompleteAllRequests(requests, new AllocationResult
                    {
                        Success = false,
                        Message = "No active shelters available"
                    });
                    return;
                }

                // Use the simulation's optimal algorithm
                var assignments = await AssignPeopleToSheltersOptimal(requests, allShelters);

                // Process results
                foreach (var request in requests)
                {
                    var userId = request.User.UserId;
                    AllocationResult result;

                    if (assignments.TryGetValue(userId, out var assignment))
                    {
                        // Successful assignment
                        var shelter = allShelters.First(s => s.ShelterId == assignment.ShelterId);

                        // Allocate in database
                        var allocated = dbShelter.AllocateUserToShelter(
                            userId, assignment.ShelterId, request.AlertId);

                        if (allocated)
                        {
                            // Get walking route
                            var route = await GetWalkingRoute(
                                request.Latitude, request.Longitude,
                                shelter.Latitude, shelter.Longitude);

                            result = new AllocationResult
                            {
                                Success = true,
                                Message = "Allocated successfully",
                                AllocatedShelterId = assignment.ShelterId,
                                ShelterName = shelter.Name,
                                Distance = assignment.Distance,
                                EstimatedArrivalTime = CalculateArrivalTime(assignment.Distance),
                                RoutePolyline = route?.OverviewPolyline,
                                RouteInstructions = route?.TextInstructions,
                                ShelterDetails = ConvertToShelterDetailsDto(shelter)
                            };
                        }
                        else
                        {
                            result = new AllocationResult
                            {
                                Success = false,
                                Message = "Failed to allocate in database"
                            };
                        }
                    }
                    else
                    {
                        // No assignment found
                        result = new AllocationResult
                        {
                            Success = false,
                            Message = "No suitable shelter found",
                            RecommendedAction = "Seek alternative shelter"
                        };
                    }

                    // Complete the request
                    if (_completionSources.TryRemove(userId, out var completionSource))
                    {
                        completionSource.SetResult(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch processing");
                CompleteAllRequests(requests, new AllocationResult
                {
                    Success = false,
                    Message = "Processing error"
                });
            }
        }

        /// <summary>
        /// The optimal assignment algorithm from the simulation
        /// </summary>
        private async Task<Dictionary<int, AssignmentInfo>> AssignPeopleToSheltersOptimal(
            List<AllocationRequest> requests, List<Shelter> shelters)
        {
            _logger.LogInformation($"Running optimal assignment for {requests.Count} people and {shelters.Count} shelters");

            // ========== START DEBUG  ==========
            _logger.LogWarning($"=========== BATCH ALLOCATION DEBUG ===========");
            _logger.LogWarning($"Processing {requests.Count} requests");
            _logger.LogWarning($"Available shelters: {shelters.Count}");

            // Log first few shelters
            foreach (var shelter in shelters.Take(5))
            {
                _logger.LogWarning($"Shelter {shelter.ShelterId}: '{shelter.Name}' - Capacity: {shelter.Capacity}");
            }

            // Log user locations
            foreach (var req in requests)
            {
                _logger.LogWarning($"User {req.User.UserId} at ({req.Latitude}, {req.Longitude})");
            }
            _logger.LogWarning($"===========================================");
            // ========== END DEBUG ==========

            // Use center of alert for cube calculations
            var centerLat = requests.First().AlertLatitude;
            var centerLon = requests.First().AlertLongitude;

            // Build cube index for shelters
            var cubeToShelters = BuildCubeToShelterIndex(shelters, centerLat, centerLon);

            // Initialize tracking structures
            var assignments = new Dictionary<int, AssignmentInfo>();
            var assignedPeople = new HashSet<int>();
            var shelterCapacity = new Dictionary<int, int>();

            // Get current occupancy for each shelter
            var dbShelter = new DBservicesShelter();
            foreach (var shelter in shelters)
            {
                var currentOccupancy = dbShelter.GetCurrentOccupancy(shelter.ShelterId);
                shelterCapacity[shelter.ShelterId] = shelter.Capacity - currentOccupancy;
            }

            // Create priority queue (same as simulation)
            var pq = new PriorityQueue<AssignmentOption, double>();

            // Populate priority queue with cube-filtered assignments
            foreach (var request in requests)
            {
                // Get shelters in surrounding cubes
                var nearbyShelters = GetSheltersInSurroundingCubes(
                    request.Latitude, request.Longitude,
                    shelters, cubeToShelters, centerLat, centerLon);

                foreach (var shelter in nearbyShelters)
                {
                    // Skip if shelter is full
                    if (shelterCapacity[shelter.ShelterId] <= 0)
                        continue;

                    // Calculate distance
                    double distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        shelter.Latitude, shelter.Longitude);

                    // Only consider shelters within maximum distance
                    if (distance <= MAX_DISTANCE_KM)
                    {
                        var option = new AssignmentOption
                        {
                            UserId = request.User.UserId,
                            ShelterId = shelter.ShelterId,
                            Distance = distance,
                            VulnerabilityScore = CalculateVulnerabilityScore(request.User.Birthday)
                        };

                        // Calculate priority (lower value = higher priority)
                        double priority = distance;
                        priority -= option.VulnerabilityScore * 0.01;

                        pq.Enqueue(option, priority);
                    }
                }
            }

            // Process assignments in priority order
            while (pq.Count > 0)
            {
                var option = pq.Dequeue();

                // Skip if person already assigned
                if (assignedPeople.Contains(option.UserId))
                    continue;

                // Skip if shelter is full
                if (shelterCapacity[option.ShelterId] <= 0)
                    continue;

                // Make assignment
                assignments[option.UserId] = new AssignmentInfo
                {
                    UserId = option.UserId,
                    ShelterId = option.ShelterId,
                    Distance = option.Distance
                };

                assignedPeople.Add(option.UserId);
                shelterCapacity[option.ShelterId]--;

                _logger.LogInformation($"Assigned user {option.UserId} to shelter {option.ShelterId} at {option.Distance:F2}km");
            }

            _logger.LogInformation($"Batch assignment complete: {assignments.Count}/{requests.Count} assigned");
            return assignments;
        }

        #region Helper Methods (from simulation)

        private Dictionary<string, List<int>> BuildCubeToShelterIndex(
            List<Shelter> shelters, double centerLat, double centerLon)
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

            return cubeToShelters;
        }

        private string GetCubeKey(double lat, double lon, double centerLat, double centerLon)
        {
            int latIndex = (int)((lat - centerLat) / CUBE_SIZE_LAT);
            int lonIndex = (int)((lon - centerLon) / CUBE_SIZE_LON_APPROX);
            return $"{latIndex},{lonIndex}";
        }

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

        private List<Shelter> GetSheltersInSurroundingCubes(
            double personLat, double personLon,
            List<Shelter> allShelters,
            Dictionary<string, List<int>> cubeToShelters,
            double centerLat, double centerLon)
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

        private int CalculateVulnerabilityScore(DateTime? birthday)
        {
            if (!birthday.HasValue)
                return 6;

            int age = DateTime.Now.Year - birthday.Value.Year;
            if (DateTime.Now.DayOfYear < birthday.Value.DayOfYear)
                age--;

            if (age >= 70) return 10;
            else if (age <= 12) return 8;
            else return 6;
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

        private DateTime CalculateArrivalTime(double distanceKm)
        {
            double walkingTimeMinutes = distanceKm / WALKING_SPEED_KM_PER_MINUTE;
            return DateTime.Now.AddMinutes(walkingTimeMinutes);
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
                        Distance = (leg?.Distance?.Value ?? 0) / 1000.0
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

        private ShelterDetailsDto ConvertToShelterDetailsDto(Shelter shelter)
        {
            return new ShelterDetailsDto
            {
                ShelterId = shelter.ShelterId,
                Name = shelter.Name,
                Address = shelter.Address,
                Latitude = shelter.Latitude,
                Longitude = shelter.Longitude,
                Capacity = shelter.Capacity,
                IsAccessible = shelter.IsAccessible,
                PetsFriendly = shelter.PetsFriendly
            };
        }

        private void CompleteAllRequests(List<AllocationRequest> requests, AllocationResult result)
        {
            foreach (var request in requests)
            {
                if (_completionSources.TryRemove(request.User.UserId, out var completionSource))
                {
                    completionSource.SetResult(result);
                }
            }
        }

        private async Task<AllocationResult> FallbackIndividualAllocation(AllocationRequest request)
        {
            // Implement fallback logic for when batch processing fails
            _logger.LogWarning($"Using fallback individual allocation for user {request.User.UserId}");

            // This could call the original individual allocation logic
            return new AllocationResult
            {
                Success = false,
                Message = "Batch processing timeout - please try again",
                RecommendedAction = "Retry or seek alternative shelter"
            };
        }

        public async Task<RouteInfoDto> RecalculateRoute(int userId, double currentLat, double currentLon)
        {
            try
            {
                // Get user's current shelter assignment
                var dbs = new DBservicesShelter();
                var visit = dbs.GetActiveUserAllocation(userId);
                if (visit == null) return null;

                var shelter = Shelter.getShelter(visit.shelter_id);
                var route = await GetWalkingRoute(
                    currentLat, currentLon,
                    shelter.Latitude, shelter.Longitude);

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

        #endregion

        #region Helper Classes

        private class AssignmentOption
        {
            public int UserId { get; set; }
            public int ShelterId { get; set; }
            public double Distance { get; set; }
            public int VulnerabilityScore { get; set; }
        }

        #endregion

        public interface IShelterService
        {
            Task<UserAllocation> GetActiveAllocationForUser(int userId);
            Task<RouteInfoDto> RecalculateRoute(int userId, double currentLat, double currentLon);
            Task MarkUserAsArrived(int userId, int shelterId);
            Task ReleaseUserFromShelter(int userId);
            Task UpdateUserAllocationStatus(int userId, string status);
        }

        public override void Dispose()
        {
            _processingSignal?.Dispose();
            base.Dispose();
        }
    }
}