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

                var people = GeneratePeople(request.PeopleCount, centerLat, centerLon, request.RadiusKm);
                var shelters = GenerateShelters(request.ShelterCount, centerLat, centerLon, request.RadiusKm);
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
        private List<ShelterDto> GenerateShelters(int count, double centerLat, double centerLon, double radiusKm)
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

                shelters.Add(new ShelterDto
                {
                    Id = i + 1,
                    Name = $"Shelter {i + 1}",
                    Latitude = centerLat + latOffset,
                    Longitude = centerLon + lonOffset,
                    Capacity = _random.Next(1, 6) // Capacity between 1 and 5
                });
            }

            return shelters;
        }

        // Main algorithm for assigning people to shelters with time constraints
        // Update the AssignPeopleToShelters method in your SimulationController.cs file

        private Dictionary<int, AssignmentDto> AssignPeopleToShelters(
            List<PersonDto> people,
            List<ShelterDto> shelters,
            PrioritySettingsDto prioritySettings)
        {
            Console.WriteLine("Starting global optimization shelter assignment...");

            // Constants defining time and distance constraints
            const double MAX_TRAVEL_TIME_MINUTES = 1.0; // Maximum travel time in minutes
            const double WALKING_SPEED_KM_PER_MINUTE = 0.6; // ~5 km/h = 0.6 km/min
            const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE;

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

            // Create a flattened list of all possible person-shelter assignments
            var allPossibleAssignments = distanceMatrix.SelectMany(x => x)
                .Where(entry => entry.IsReachable)
                .OrderBy(entry => prioritySettings?.EnableAgePriority == true ? -entry.VulnerabilityScore : 0)
                .ThenBy(entry => entry.Distance)
                .ToList();

            Console.WriteLine($"Found {allPossibleAssignments.Count} possible assignments within time constraints");

            // Step 4: Global assignment optimization 
            // Initialize assignment tracking
            var assignments = new Dictionary<int, AssignmentDto>();
            var assignedPeople = new HashSet<int>();
            var criticalPeople = new HashSet<int>();

            // First pass: Identify "critical" people who can only reach one shelter
            for (int i = 0; i < people.Count; i++)
            {
                var personOptions = distanceMatrix[i].Where(entry => entry.IsReachable).ToList();

                if (personOptions.Count == 1)
                {
                    criticalPeople.Add(people[i].Id);
                }
            }

            Console.WriteLine($"Identified {criticalPeople.Count} critical people with only one shelter option");

            // Second pass: Assign critical people first
            if (criticalPeople.Count > 0)
            {
                // Sort critical people by vulnerability if priority is enabled
                var sortedCriticalPeople = criticalPeople
                    .Select(id => people.First(p => p.Id == id))
                    .OrderByDescending(p => prioritySettings?.EnableAgePriority == true ?
                        CalculateVulnerabilityScore(p.Age) : 0)
                    .Select(p => p.Id)
                    .ToList();

                // Assign critical people to their only option
                foreach (var personId in sortedCriticalPeople)
                {
                    int personIndex = people.FindIndex(p => p.Id == personId);
                    var personOptions = distanceMatrix[personIndex].Where(entry => entry.IsReachable).ToList();

                    if (personOptions.Count == 1 && shelterCapacity[personOptions[0].ShelterId] > 0)
                    {
                        // Assign this person to their only available shelter
                        assignments[personId] = new AssignmentDto
                        {
                            PersonId = personId,
                            ShelterId = personOptions[0].ShelterId,
                            Distance = personOptions[0].Distance
                        };

                        // Update shelter capacity and tracking
                        shelterCapacity[personOptions[0].ShelterId]--;
                        assignedPeople.Add(personId);

                        Console.WriteLine($"Assigned critical person {personId} to shelter {personOptions[0].ShelterId}");
                    }
                }
            }

            // Third pass: Process the remaining people
            // Iterate through all possible assignments, sorted by priority and distance
            foreach (var entry in allPossibleAssignments)
            {
                int personId = entry.PersonId;
                int shelterId = entry.ShelterId;
                double distance = entry.Distance;

                // Skip if this person is already assigned or the shelter is full
                if (assignedPeople.Contains(personId) || shelterCapacity[shelterId] <= 0)
                {
                    continue;
                }

                // Before making the assignment, check if this is the best option
                // Does this person have other shelter options?
                int personIndex = people.FindIndex(p => p.Id == personId);
                var personOptions = distanceMatrix[personIndex]
                    .Where(opt => opt.IsReachable && shelterCapacity[opt.ShelterId] > 0)
                    .OrderBy(opt => opt.Distance)
                    .ToList();

                // Are there other people who can ONLY use this shelter?
                var peopleWhoNeedThisShelter = new List<int>();

                for (int i = 0; i < people.Count; i++)
                {
                    int pid = people[i].Id;
                    if (assignedPeople.Contains(pid)) continue;

                    var options = distanceMatrix[i]
                        .Where(opt => opt.IsReachable && shelterCapacity[opt.ShelterId] > 0)
                        .ToList();

                    if (options.Count == 1 && options[0].ShelterId == shelterId)
                    {
                        peopleWhoNeedThisShelter.Add(pid);
                    }
                }

                // If this person has multiple options but others can only use this shelter,
                // skip for now to save the spot for those who need it
                if (personOptions.Count > 1 &&
                    peopleWhoNeedThisShelter.Count > 0 &&
                    shelterCapacity[shelterId] <= peopleWhoNeedThisShelter.Count)
                {
                    continue;
                }

                // Make the assignment
                assignments[personId] = new AssignmentDto
                {
                    PersonId = personId,
                    ShelterId = shelterId,
                    Distance = distance
                };

                // Update shelter capacity and tracking
                shelterCapacity[shelterId]--;
                assignedPeople.Add(personId);
            }

            // Final pass: Try to assign any remaining people to any available shelter
            foreach (var person in people)
            {
                if (assignedPeople.Contains(person.Id)) continue; // Already assigned

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

        // Helper method to assign people to their nearest shelters
        private void AssignPeopleToNearestShelters(
            List<PersonDto> peopleToAssign,
            List<ShelterWithCapacity> availableShelters,
            Dictionary<int, AssignmentDto> assignments,
            double maxDistanceKm)
        {
            foreach (var person in peopleToAssign)
            {
                // Skip if already assigned
                if (assignments.ContainsKey(person.Id))
                    continue;

                // Find shelters with capacity within max distance
                var accessibleShelters = availableShelters
                    .Where(s => s.RemainingCapacity > 0)
                    .Select(shelter =>
                    {
                        double distance = CalculateDistance(
                            person.Latitude, person.Longitude,
                            shelter.Latitude, shelter.Longitude);
                        return (Shelter: shelter, Distance: distance);
                    })
                    .Where(pair => pair.Distance <= maxDistanceKm)
                    .OrderBy(pair => pair.Distance)
                    .ToList();

                if (accessibleShelters.Any())
                {
                    var (nearestShelter, distance) = accessibleShelters.First();

                    // Create assignment
                    assignments[person.Id] = new AssignmentDto
                    {
                        PersonId = person.Id,
                        ShelterId = nearestShelter.Id,
                        Distance = distance
                    };

                    // Update shelter capacity
                    nearestShelter.RemainingCapacity--;
                }
            }
        }

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