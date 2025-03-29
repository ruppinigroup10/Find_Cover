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
                    age = _random.Next(1, 13); // Ages 1-12
                }
                else if (ageRandom < 0.85) // 70% are adults
                {
                    age = _random.Next(13, 70); // Ages 13-69
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

        // Helper method to generate shelters
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
                    Capacity = 1 + _random.Next(5) // 1-5 capacity
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
                    Capacity = 1 + _random.Next(5) // 1-5 capacity
                });
            }

            return shelters;
        }

        // Main algorithm for assigning people to shelters with time constraints
        private Dictionary<int, AssignmentDto> AssignPeopleToShelters(
            List<PersonDto> people,
            List<ShelterDto> shelters,
            PrioritySettingsDto prioritySettings)
        {
            var assignments = new Dictionary<int, AssignmentDto>();

            // Constants
            const double MAX_TRAVEL_TIME_MINUTES = 1.0; // Maximum travel time in minutes
            const double WALKING_SPEED_KM_PER_MINUTE = 0.6; // ~5 km/h = 0.6 km/min
            const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE; // ~600m in 1 minute

            // Working list of shelters with remaining capacity
            var remainingShelters = shelters.Select(s => new ShelterWithCapacity
            {
                Id = s.Id,
                Name = s.Name,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Capacity = s.Capacity,
                RemainingCapacity = s.Capacity
            }).ToList();

            // First, count how many shelters and total capacity are available
            int totalShelterCapacity = remainingShelters.Sum(s => s.Capacity);
            int totalPeople = people.Count;
            bool enoughSheltersForAll = totalShelterCapacity >= totalPeople;

            // Create a list of all valid person-shelter pairs within time constraint
            var allValidPairs = new List<(PersonDto Person, ShelterWithCapacity Shelter, double Distance)>();

            foreach (var person in people)
            {
                var accessibleShelters = remainingShelters
                    .Select(shelter =>
                    {
                        double distance = CalculateDistance(
                            person.Latitude, person.Longitude,
                            shelter.Latitude, shelter.Longitude);
                        return (Shelter: shelter, Distance: distance);
                    })
                    .Where(pair => pair.Distance <= MAX_DISTANCE_KM)
                    .OrderBy(pair => pair.Distance)
                    .ToList();

                foreach (var (shelter, distance) in accessibleShelters)
                {
                    allValidPairs.Add((person, shelter, distance));
                }
            }

            // If there are enough shelters for everyone and prioritization is enabled
            if (enoughSheltersForAll && prioritySettings?.EnableAgePriority == true)
            {
                // First, assign elderly to nearest shelters
                var elderlyPeople = people
                    .Where(p => p.Age >= prioritySettings.ElderlyMinAge)
                    .ToList();

                AssignPeopleToNearestShelters(elderlyPeople, remainingShelters, assignments, MAX_DISTANCE_KM);

                // Then, assign children to nearest shelters
                var childrenPeople = people
                    .Where(p => p.Age <= prioritySettings.ChildMaxAge)
                    .Where(p => !assignments.ContainsKey(p.Id))
                    .ToList();

                AssignPeopleToNearestShelters(childrenPeople, remainingShelters, assignments, MAX_DISTANCE_KM);

                // Finally, assign remaining adults to nearest shelters
                var adultPeople = people
                    .Where(p => p.Age > prioritySettings.ChildMaxAge && p.Age < prioritySettings.ElderlyMinAge)
                    .Where(p => !assignments.ContainsKey(p.Id))
                    .ToList();

                AssignPeopleToNearestShelters(adultPeople, remainingShelters, assignments, MAX_DISTANCE_KM);
            }
            else
            {
                // Not enough shelters for everyone or no priority - perform random selection

                // First, randomly select people who can reach a shelter within the time limit
                var eligiblePeople = allValidPairs
                    .Select(p => p.Person)
                    .Distinct()
                    .OrderBy(p => Guid.NewGuid()) // Random order
                    .ToList();

                // Limit to available total capacity
                int availableCapacity = remainingShelters.Sum(s => s.RemainingCapacity);
                eligiblePeople = eligiblePeople.Take(availableCapacity).ToList();

                // Prioritize elderly among the randomly selected people
                var selectedElderly = eligiblePeople
                    .Where(p => p.Age >= prioritySettings?.ElderlyMinAge)
                    .ToList();

                var selectedNonElderly = eligiblePeople
                    .Where(p => !selectedElderly.Any(e => e.Id == p.Id))
                    .ToList();

                // Assign elderly to their nearest shelters first
                AssignPeopleToNearestShelters(selectedElderly, remainingShelters, assignments, MAX_DISTANCE_KM);

                // Then assign remaining people to available shelters
                AssignPeopleToNearestShelters(selectedNonElderly, remainingShelters, assignments, MAX_DISTANCE_KM);
            }

            return assignments;
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