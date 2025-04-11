using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FindCover.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimulationController : ControllerBase
    {
        private static readonly Random _random = new Random();

        [HttpPost("run")]
        public ActionResult<SimulationResponseDto> RunSimulation([FromBody] SimulationRequestDto request)
        {
            try
            {
                // Validate input
                if (request == null)
                {
                    return BadRequest("Invalid simulation request data");
                }

                // Generate test data for Beer Sheva
                double centerLat = 31.2518; // Override with Beer Sheva coordinates
                double centerLon = 34.7913; // Override with Beer Sheva coordinates

                // Check if we should use custom people instead of generating them
                List<PersonDto> people;
                if (request.UseCustomPeople && request.CustomPeople != null && request.CustomPeople.Any())
                {
                    // Use the provided custom people
                    people = request.CustomPeople;
                }
                else
                {
                    // Generate people as usual
                    people = GeneratePeople(request.PeopleCount, centerLat, centerLon, request.RadiusKm);
                }

                // Check if we should use custom shelters
                List<ShelterDto> shelters;
                if (request.UseCustomShelters && request.CustomShelters != null && request.CustomShelters.Any())
                {
                    // Use provided custom shelters
                    shelters = request.CustomShelters;
                }
                else
                {
                    // Generate shelters as usual
                    shelters = GenerateShelters(
                        request.ShelterCount,
                        centerLat,
                        centerLon,
                        request.RadiusKm,
                        request.ZeroCapacityShelters);
                }

                //var people = GeneratePeople(request.PeopleCount, centerLat, centerLon, request.RadiusKm);
                var assignments = AssignPeopleToShelters(people, shelters, request.PrioritySettings);

                // Calculate statistics
                var assignedCount = assignments.Count;
                var averageDistance = assignments.Values.Count > 0 ? assignments.Values.Average(a => a.Distance) : 0;
                var maxDistance = assignments.Values.Count > 0 ? assignments.Values.Max(a => a.Distance) : 0;

                // Construct response
                var response = new SimulationResponseDto
                {
                    People = people,
                    Shelters = shelters,
                    Assignments = assignments,
                    Statistics = new SimulationStatisticsDto
                    {
                        ExecutionTimeMs = 0, // Not measuring for simplicity
                        AssignedCount = assignedCount,
                        UnassignedCount = people.Count - assignedCount,
                        AssignmentPercentage = people.Count > 0 ? (double)assignedCount / people.Count : 0,
                        TotalShelterCapacity = shelters.Sum(s => s.Capacity),
                        AverageDistance = averageDistance,
                        MaxDistance = maxDistance,
                        MinDistance = assignments.Values.Count > 0 ? assignments.Values.Min(a => a.Distance) : 0
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while running the simulation: {ex.Message}");
            }
        }

        // Helper method to generate people
        private List<PersonDto> GeneratePeople(int count, double centerLat, double centerLon, double radiusKm)
        {
            var people = new List<PersonDto>();

            // Convert radius from km to degrees for latitude and longitude
            double latDelta = radiusKm / 111.0;
            double lonDelta = radiusKm / (111.0 * Math.Cos(centerLat * Math.PI / 180));

            for (int i = 0; i < count; i++)
            {
                // Generate random location within the specified radius
                double angle = _random.NextDouble() * 2 * Math.PI;
                double distance = _random.NextDouble() * radiusKm / 111.0; // Convert to degrees

                double latOffset = distance * Math.Cos(angle);
                double lonOffset = distance * Math.Sin(angle);

                // Generate age with proper distribution - include children and elderly
                int age;
                double ageRandom = _random.NextDouble();
                if (ageRandom < 0.15) // 15% are children
                {
                    age = _random.Next(1, 19); // Ages 1-18
                }
                else if (ageRandom < 0.85) // 70% are adults
                {
                    age = _random.Next(19, 70); // Ages 19-69
                }
                else // 15% are elderly
                {
                    age = _random.Next(70, 95); // Ages 70-94
                }

                people.Add(new PersonDto
                {
                    Id = i + 1,
                    Age = age,
                    Latitude = centerLat + latOffset,
                    Longitude = centerLon + lonOffset
                });
            }

            return people;
        }

        // Helper method to generate shelters with capacity between 1 and 5
        private List<ShelterDto> GenerateShelters(int count, double centerLat, double centerLon, double radiusKm, bool zeroCapacityShelters = false)
        {
            var shelters = new List<ShelterDto>();

            // Convert radius from km to degrees for latitude and longitude
            double latDelta = radiusKm / 111.0;
            double lonDelta = radiusKm / (111.0 * Math.Cos(centerLat * Math.PI / 180));

            // Add some known Beer Sheva locations for realism
            var knownLocations = new List<(string Name, double Lat, double Lon)>
    {
        ("Ben Gurion University", 31.2634, 34.8044),
        ("Beer Sheva Central Station", 31.2434, 34.7980),
        ("Grand Canyon Mall", 31.2508, 34.7738),
        ("Soroka Medical Center", 31.2534, 34.8018)
    };

            // Add known locations first (if we have fewer shelters than known locations, take a subset)
            for (int i = 0; i < Math.Min(count, knownLocations.Count); i++)
            {
                var location = knownLocations[i];
                shelters.Add(new ShelterDto
                {
                    Id = i + 1,
                    Name = location.Name,
                    Latitude = location.Lat,
                    Longitude = location.Lon,
                    Capacity = _random.Next(1, 6) // Capacity between 1 and 5
                });
            }

            // Add remaining random shelters if needed
            for (int i = knownLocations.Count; i < count; i++)
            {
                // Generate random location (more central than the people)
                double angle = _random.NextDouble() * 2 * Math.PI;
                double distance = _random.NextDouble() * radiusKm * 0.7 / 111.0; // Convert to degrees, more central

                double latOffset = distance * Math.Cos(angle);
                double lonOffset = distance * Math.Sin(angle);

                // Determine capacity - handle zero capacity scenario
                int capacity;
                if (zeroCapacityShelters && _random.NextDouble() < 0.4) // 40% chance of zero capacity for random shelters
                {
                    capacity = 0; // Zero capacity
                }
                else
                {
                    capacity = _random.Next(1, 6); // Normal capacity between 1 and 5
                }

                shelters.Add(new ShelterDto
                {
                    Id = i + 1,
                    Name = capacity == 0 ? $"Closed Shelter {i + 1}" : $"Shelter {i + 1}",
                    Latitude = centerLat + latOffset,
                    Longitude = centerLon + lonOffset,
                    Capacity = capacity
                });
            }

            return shelters;
        }

        // Main algorithm for assigning people to shelters with time constraints
        // Using global assignment optimization 
        // Modified to prioritize by distance first, then age, and protect people with only one shelter option
        private Dictionary<int, AssignmentDto> AssignPeopleToShelters(
            List<PersonDto> people,
            List<ShelterDto> shelters,
            PrioritySettingsDto prioritySettings)
        {
            Console.WriteLine("Starting modified shelter assignment algorithm...");

            // Constants defining time and distance constraints
            const double MAX_TRAVEL_TIME_MINUTES = 1.0; // Maximum travel time in minutes
            const double WALKING_SPEED_KM_PER_MINUTE = 0.6; // ~5 km/h = 0.6 km/min
            const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE; // Should be about 0.6km

            Console.WriteLine($"Time constraint: Maximum distance = {MAX_DISTANCE_KM:F4} km");

            // Step 1: Create a distance matrix between all people and shelters
            Console.WriteLine("Building distance matrix...");
            var distanceMatrix = new List<List<AssignmentOption>>();

            foreach (var person in people)
            {
                var personDistances = new List<AssignmentOption>();

                foreach (var shelter in shelters)
                {
                    double distance = CalculateDistance(
                        person.Latitude, person.Longitude,
                        shelter.Latitude, shelter.Longitude);

                    // Store whether this shelter is reachable within time constraints
                    bool isReachable = distance <= MAX_DISTANCE_KM;

                    personDistances.Add(new AssignmentOption
                    {
                        PersonId = person.Id,
                        ShelterId = shelter.Id,
                        Distance = distance,
                        IsReachable = isReachable,
                        // Add vulnerability score if priority is enabled
                        VulnerabilityScore = prioritySettings?.EnableAgePriority == true
                            ? CalculateVulnerabilityScore(person.Age)
                            : 0
                    });
                }

                distanceMatrix.Add(personDistances);
            }

            // Step 2: Calculate total shelter capacity
            int totalCapacity = shelters.Sum(s => s.Capacity);
            int totalPeople = people.Count;

            Console.WriteLine($"Total shelter capacity: {totalCapacity}, Total people: {totalPeople}");

            // Step 3: Prepare working data structures
            // Track remaining capacity of each shelter
            var shelterCapacity = shelters.ToDictionary(s => s.Id, s => s.Capacity);

            // Initialize assignment tracking
            var assignments = new Dictionary<int, AssignmentDto>();
            var assignedPeople = new HashSet<int>();

            // Step 4: Analyze potential assignments and constraints

            // Identify people with only one shelter option (critical people)
            var criticalPeople = new List<int>();
            for (int i = 0; i < people.Count; i++)
            {
                var personOptions = distanceMatrix[i].Where(entry => entry.IsReachable).ToList();
                if (personOptions.Count == 1)
                {
                    criticalPeople.Add(people[i].Id);
                }
            }

            Console.WriteLine($"Identified {criticalPeople.Count} critical people with only one shelter option");

            // New approach: Group people by distance ranges for better prioritization
            // This allows us to consider all "close" people as a group first

            // Create a flattened list of all possible person-shelter assignments
            var allPossibleAssignments = distanceMatrix
                .SelectMany(x => x)
                .Where(entry => entry.IsReachable)
                .ToList();

            // First, sort by distance only (ignoring age priority)
            var assignmentsByDistance = allPossibleAssignments
                .OrderBy(entry => entry.Distance)
                .ToList();

            // Process people by distance ranges
            // Define distance segments for processing (e.g., 0-100m, 100-200m, etc.)
            const int DISTANCE_SEGMENTS = 6; // Divide our 600m max into 6 segments of 100m each
            double segmentSize = MAX_DISTANCE_KM / DISTANCE_SEGMENTS;

            Console.WriteLine($"Processing by distance segments of {segmentSize * 1000:F0}m");

            // Process each distance segment
            for (int segment = 0; segment < DISTANCE_SEGMENTS; segment++)
            {
                double minDistance = segment * segmentSize;
                double maxDistance = (segment + 1) * segmentSize;

                Console.WriteLine($"Processing segment {segment + 1}: {minDistance * 1000:F0}m to {maxDistance * 1000:F0}m");

                // Get all assignment options in this distance range
                var segmentAssignments = allPossibleAssignments
                    .Where(a => a.Distance >= minDistance && a.Distance < maxDistance)
                    .ToList();

                if (segmentAssignments.Count == 0)
                    continue;

                // Within this distance segment, prioritize by vulnerability (age) if enabled
                // and then by exact distance for tie-breaking
                var prioritizedSegmentAssignments = segmentAssignments
                    .OrderByDescending(a => prioritySettings?.EnableAgePriority == true ? a.VulnerabilityScore : 0)
                    .ThenBy(a => a.Distance)
                    .ToList();

                // Step 5: Process critical people first within this segment
                var criticalInSegment = prioritizedSegmentAssignments
                    .Where(a => criticalPeople.Contains(a.PersonId))
                    .ToList();

                foreach (var assignment in criticalInSegment)
                {
                    // Skip if this person is already assigned or the shelter is full
                    if (assignedPeople.Contains(assignment.PersonId) || shelterCapacity[assignment.ShelterId] <= 0)
                        continue;

                    // Make the assignment
                    assignments[assignment.PersonId] = new AssignmentDto
                    {
                        PersonId = assignment.PersonId,
                        ShelterId = assignment.ShelterId,
                        Distance = assignment.Distance
                    };

                    // Update tracking
                    shelterCapacity[assignment.ShelterId]--;
                    assignedPeople.Add(assignment.PersonId);

                    Console.WriteLine($"Assigned critical person {assignment.PersonId} to shelter {assignment.ShelterId} (distance: {assignment.Distance * 1000:F0}m)");
                }

                // Step 6: Process remaining people in this segment
                foreach (var assignment in prioritizedSegmentAssignments)
                {
                    // Skip if already processed in critical people loop
                    if (assignedPeople.Contains(assignment.PersonId) || shelterCapacity[assignment.ShelterId] <= 0)
                        continue;

                    // Special handling - check if this shelter is the only option for unassigned critical people
                    var shelterIsCriticalForOthers = false;
                    var shelterId = assignment.ShelterId;

                    // Find unassigned critical people who need this shelter
                    var unassignedCritical = criticalPeople
                        .Where(id => !assignedPeople.Contains(id))
                        .ToList();

                    foreach (var criticalId in unassignedCritical)
                    {
                        int criticalIndex = people.FindIndex(p => p.Id == criticalId);
                        var criticalOptions = distanceMatrix[criticalIndex]
                            .Where(opt => opt.IsReachable && opt.ShelterId == shelterId)
                            .ToList();

                        // If this critical person needs this shelter and the shelter is close to running out
                        if (criticalOptions.Count > 0 && shelterCapacity[shelterId] <= unassignedCritical.Count)
                        {
                            shelterIsCriticalForOthers = true;
                            break;
                        }
                    }

                    // Skip assignment if this would take a spot needed by a critical person
                    if (shelterIsCriticalForOthers)
                    {
                        // Check if current person has alternative options
                        int personIndex = people.FindIndex(p => p.Id == assignment.PersonId);
                        var alternatives = distanceMatrix[personIndex]
                            .Where(opt => opt.IsReachable && opt.ShelterId != shelterId && shelterCapacity[opt.ShelterId] > 0)
                            .OrderBy(opt => opt.Distance)
                            .ToList();

                        if (alternatives.Count > 0)
                        {
                            // Person has alternatives, assign to the next best option
                            var nextBest = alternatives.First();

                            assignments[assignment.PersonId] = new AssignmentDto
                            {
                                PersonId = assignment.PersonId,
                                ShelterId = nextBest.ShelterId,
                                Distance = nextBest.Distance
                            };

                            // Update tracking
                            shelterCapacity[nextBest.ShelterId]--;
                            assignedPeople.Add(assignment.PersonId);

                            Console.WriteLine($"Assigned person {assignment.PersonId} to alternative shelter {nextBest.ShelterId} (distance: {nextBest.Distance * 1000:F0}m)");
                            continue;
                        }
                    }

                    // Regular assignment
                    assignments[assignment.PersonId] = new AssignmentDto
                    {
                        PersonId = assignment.PersonId,
                        ShelterId = assignment.ShelterId,
                        Distance = assignment.Distance
                    };

                    // Update tracking
                    shelterCapacity[assignment.ShelterId]--;
                    assignedPeople.Add(assignment.PersonId);

                    Console.WriteLine($"Assigned person {assignment.PersonId} to shelter {assignment.ShelterId} (distance: {assignment.Distance * 1000:F0}m)");
                }
            }

            // Step 7: Final pass - ensure no available assignments were missed
            // This handles any edge cases that might not have been caught in the main logic
            foreach (var person in people)
            {
                if (assignedPeople.Contains(person.Id))
                    continue; // Already assigned

                // Find any shelter with remaining capacity within range
                int personIndex = people.IndexOf(person);
                var options = distanceMatrix[personIndex]
                    .Where(entry => entry.IsReachable && shelterCapacity[entry.ShelterId] > 0)
                    .OrderBy(entry => entry.Distance)
                    .ToList();

                if (options.Count > 0)
                {
                    var bestOption = options[0];
                    assignments[person.Id] = new AssignmentDto
                    {
                        PersonId = person.Id,
                        ShelterId = bestOption.ShelterId,
                        Distance = bestOption.Distance
                    };

                    // Update shelter capacity
                    shelterCapacity[bestOption.ShelterId]--;
                    assignedPeople.Add(person.Id);

                    Console.WriteLine($"Final pass: Assigned person {person.Id} to shelter {bestOption.ShelterId} (distance: {bestOption.Distance * 1000:F0}m)");
                }
            }

            Console.WriteLine($"Final assignments: {assignments.Count} people assigned");
            return assignments;
        }

        /**
         * Helper class to store assignment options with additional metadata
         */
        private class AssignmentOption
        {
            public int PersonId { get; set; }
            public int ShelterId { get; set; }
            public double Distance { get; set; }
            public bool IsReachable { get; set; }
            public int VulnerabilityScore { get; set; }
        }

        /**
         * Calculates a vulnerability score based on age
         * Higher scores indicate higher priority (elderly and children)
         */
        private int CalculateVulnerabilityScore(int age)
        {
            if (age >= 70)
            {
                // Elderly (70+): highest priority
                return 10;
            }
            else if (age <= 12)
            {
                // Children (0-12): second highest priority
                return 8;
            }
            else if (age >= 60)
            {
                // Older adults (60-69): medium-high priority
                return 6;
            }
            else if (age <= 18)
            {
                // Teenagers (13-18): medium priority
                return 4;
            }
            else
            {
                // Adults (19-59): lowest priority
                return 2;
            }
        }

        // // Helper method to assign people to their nearest shelters
        // private void AssignPeopleToNearestShelters(
        //     List<PersonDto> peopleToAssign,
        //     List<ShelterWithCapacity> availableShelters,
        //     Dictionary<int, AssignmentDto> assignments,
        //     double maxDistanceKm)
        // {
        //     foreach (var person in peopleToAssign)
        //     {
        //         // Skip if already assigned
        //         if (assignments.ContainsKey(person.Id))
        //             continue;

        //         // Find shelters with capacity within max distance
        //         var accessibleShelters = availableShelters
        //             .Where(s => s.RemainingCapacity > 0)
        //             .Select(shelter =>
        //             {
        //                 double distance = CalculateDistance(
        //                     person.Latitude, person.Longitude,
        //                     shelter.Latitude, shelter.Longitude);
        //                 return (Shelter: shelter, Distance: distance);
        //             })
        //             .Where(pair => pair.Distance <= maxDistanceKm)
        //             .OrderBy(pair => pair.Distance)
        //             .ToList();

        //         if (accessibleShelters.Any())
        //         {
        //             var (nearestShelter, distance) = accessibleShelters.First();

        //             // Create assignment
        //             assignments[person.Id] = new AssignmentDto
        //             {
        //                 PersonId = person.Id,
        //                 ShelterId = nearestShelter.Id,
        //                 Distance = distance
        //             };

        //             // Update shelter capacity
        //             nearestShelter.RemainingCapacity--;
        //         }
        //     }
        // }

        // Helper to calculate distance between points
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Earth radius in km
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        // Enhanced shelter capacity tracking class with more details
        private class ShelterWithCapacity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int Capacity { get; set; }
            public int RemainingCapacity { get; set; }
        }
    }

    // DTOs for requests and responses
    public class SimulationRequestDto
    {
        public int PeopleCount { get; set; } = 1000;
        public int ShelterCount { get; set; } = 20;
        public double CenterLatitude { get; set; } = 31.2518; // Beer Sheva
        public double CenterLongitude { get; set; } = 34.7913; // Beer Sheva
        public double RadiusKm { get; set; } = 5;
        public PrioritySettingsDto PrioritySettings { get; set; } = new PrioritySettingsDto();
        public bool UseCustomPeople { get; set; } = false;
        public List<PersonDto> CustomPeople { get; set; } = new List<PersonDto>();
        public bool ZeroCapacityShelters { get; set; } = false;
        public bool UseCustomShelters { get; set; } = false;
        public List<ShelterDto> CustomShelters { get; set; } = new List<ShelterDto>();
    }

    public class SimulationResponseDto
    {
        public List<PersonDto> People { get; set; }
        public List<ShelterDto> Shelters { get; set; }
        public Dictionary<int, AssignmentDto> Assignments { get; set; }
        public SimulationStatisticsDto Statistics { get; set; }
    }

    public class PersonDto
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
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
    }

    public class PrioritySettingsDto
    {
        public bool EnableAgePriority { get; set; } = true;
        public int ChildMaxAge { get; set; } = 12;
        public int ElderlyMinAge { get; set; } = 70;
    }

    public class SimulationStatisticsDto
    {
        public long ExecutionTimeMs { get; set; }
        public int AssignedCount { get; set; }
        public int UnassignedCount { get; set; }
        public double AssignmentPercentage { get; set; }
        public int TotalShelterCapacity { get; set; }
        public double AverageDistance { get; set; }
        public double MaxDistance { get; set; }
        public double MinDistance { get; set; }
    }
}