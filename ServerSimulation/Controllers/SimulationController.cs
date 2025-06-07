using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using ServerSimulation.Models;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ServerSimulation.Models.DTOs;
using ServerSimulation.DAL;

/*
 * This is SimulationController class which is responsible for handling simulation requests.
 * This controller runs simulations to determine the best assignment of people to shelters 
 * in emergency situations, such as during an alert when people need to quickly find shelter.
 */

namespace FindCover.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimulationController : ControllerBase
    {
        // Create a Random number generator that will be used throughout the controller
        // The static readonly modifier ensures that the same random number generator is used throughout the controller's lifetime, 
        // which is more efficient than creating a new random generator each time it's needed.
        private static readonly Random _random = new Random();

        // Declare the services that this controller will use
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ILogger<SimulationController> _logger;

        // For the cube calculation, we define the size of the cube in kilometers
        private const double CUBE_SIZE_KM = 0.2; // 200 meters in kilometers
        private const double CUBE_SIZE_LAT = CUBE_SIZE_KM / 111.0; // Convert to latitude degrees


        public SimulationController(IGoogleMapsService googleMapsService, ILogger<SimulationController> logger)
        {
            _googleMapsService = googleMapsService;
            _logger = logger;
        }

        //===================================
        // Simulation start here
        //===================================

        /// <summary>
        /// Main endpoint that runs the shelter assignment simulation
        /// Takes in simulation parameters and returns the results
        /// </summary>
        [HttpPost("run")]
        public ActionResult<SimulationResponseDto> RunSimulation([FromBody] SimulationRequestDto request)
        {
            try
            {
                // Validate that we received a proper request
                if (request == null)
                {
                    return BadRequest("Invalid simulation request data");
                }

                // Set the center coordinates for the simulation (Beer Sheva, Israel)
                double centerLat = 31.2518;
                double centerLon = 34.7913;

                // STEP 1: DETERMINE PEOPLE LOCATIONS
                // Either use custom people provided in the request, or generate random people
                List<PersonDto> people;
                if (request.UseCustomPeople && request.CustomPeople != null && request.CustomPeople.Any())
                {
                    // Use the custom people provided in the request
                    people = request.CustomPeople;
                }
                else
                {
                    // Generate random people around the center point
                    people = GeneratePeople(request.PeopleCount, centerLat, centerLon, request.RadiusKm);
                }

                // STEP 2: DETERMINE SHELTER LOCATIONS
                // Either use custom shelters provided in the request, 
                // or get them from database and/or generate random ones
                List<ShelterDto> shelters;
                if (request.UseCustomShelters && request.CustomShelters != null && request.CustomShelters.Any())
                {
                    // Use the custom shelters provided in the request
                    shelters = request.CustomShelters;
                }
                else
                {
                    // Get shelters from database and/or generate random ones
                    shelters = GenerateShelters(
                        request.ShelterCount, // Additional shelters to generate beyond what's in the database
                        centerLat,
                        centerLon,
                        request.RadiusKm,
                        request.ZeroCapacityShelters, // Whether to include shelters with zero capacity
                        request.UseDatabaseShelters); // Whether to include shelters from database
                }

                // STEP 3: DETERMINE OPTIMAL ASSIGNMENTS
                // Run the assignment algorithm to match people to the closest available shelters
                // var assignments = AssignPeopleToShelters(people, shelters, request.PrioritySettings); // old algorithm
                var assignments = AssignPeopleToSheltersOptimal(people, shelters, request.PrioritySettings); // new algorithm

                // STEP 4: HANDLE UNASSIGNED PEOPLE
                // For people who couldn't be assigned to any shelter, find their nearest shelter for reference
                // This is useful for reporting purposes, even if they couldn't be assigned
                var unassignedPeople = people.Where(p => !assignments.ContainsKey(p.Id)).ToList();
                foreach (var person in unassignedPeople)
                {
                    // Find the nearest shelter to this person, even if they couldn't be assigned to it
                    ShelterDto nearestShelter = null;
                    double nearestDistance = double.MaxValue;

                    foreach (var shelter in shelters)
                    {
                        double distance = CalculateDistance(
                            person.Latitude, person.Longitude,
                            shelter.Latitude, shelter.Longitude);

                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestShelter = shelter;
                        }
                    }

                    // Store the nearest shelter information with the person (though they're not assigned to it)
                    if (nearestShelter != null)
                    {
                        person.NearestShelterId = nearestShelter.Id;
                        person.NearestShelterDistance = nearestDistance;
                    }
                }

                // STEP 5: CALCULATE SIMULATION STATISTICS
                // Calculate overall statistics about the simulation results
                var assignedCount = assignments.Count;
                var averageDistance = assignments.Values.Count > 0 ? assignments.Values.Average(a => a.Distance) : 0;
                var maxDistance = assignments.Values.Count > 0 ? assignments.Values.Max(a => a.Distance) : 0;

                // STEP 6: PREPARE RESPONSE
                // Create the response object with all simulation results
                var response = new SimulationResponseDto
                {
                    People = people,
                    Shelters = shelters,
                    Assignments = assignments,
                    Statistics = new SimulationStatisticsDto
                    {
                        ExecutionTimeMs = 0, // Not measuring execution time for simplicity
                        AssignedCount = assignedCount, // Number of people successfully assigned to shelters
                        UnassignedCount = people.Count - assignedCount, // Number of people who couldn't be assigned
                        AssignmentPercentage = people.Count > 0 ? (double)assignedCount / people.Count : 0, // Percentage of people assigned
                        TotalShelterCapacity = shelters.Sum(s => s.Capacity), // Total capacity of all shelters
                        ShelterUsagePercentage = shelters.Sum(s => s.Capacity) > 0
                        ? (double)assignedCount / shelters.Sum(s => s.Capacity) * 100
                        : 0, // Percentage of shelter capacity utilized
                        AverageDistance = averageDistance, // Average distance from people to their assigned shelters
                        MaxDistance = maxDistance, // Maximum distance any person has to travel
                        MinDistance = assignments.Values.Count > 0 ? assignments.Values.Min(a => a.Distance) : 0 // Minimum distance any person has to travel
                    }
                };

                // Return the successful response with HTTP 200 OK status
                return Ok(response);
            }
            catch (Exception ex)
            {
                // If any error occurs during simulation, return a 500 Internal Server Error
                return StatusCode(500, $"An error occurred while running the simulation: {ex.Message}");
            }
        }

        //===================================
        // Helper method to generate people
        //===================================

        /// <summary>
        /// Generates random people distributed around a center point
        /// People have random ages that follow Israel's demographic distribution
        /// </summary>
        /// <param name="count">How many people to generate</param>
        /// <param name="centerLat">Center latitude</param>
        /// <param name="centerLon">Center longitude</param>
        /// <param name="radiusKm">Radius in kilometers within which to generate people</param>
        private List<PersonDto> GeneratePeople(int count, double centerLat, double centerLon, double radiusKm)
        {
            var people = new List<PersonDto>();

            // Convert radius from kilometers to latitude/longitude degrees
            // 111.0 km is approximately 1 degree of latitude (this value varies slightly by location)
            // To limit our simulation to a specific geographic radius (like 5km), 
            // we must convert kilometers to latitude/longitude degrees 
            // because maps use degrees while distances are measured in kilometers.
            double latDelta = radiusKm / 111.0; // 5 km = 0.045 degrees (5 km / 111 km)
            double lonDelta = radiusKm / (111.0 * Math.Cos(centerLat * Math.PI / 180)); // 5 km = 0.053 degrees (5 km / (111 km * cos(lat)))

            // Generate the specified number of people
            for (int i = 0; i < count; i++)
            {
                // Generate a random location within the specified radius
                // First, pick a random angle and distance from center

                // The angle is a random value between 0 and 2*PI (full circle) to determine the direction from the center point.
                double angle = _random.NextDouble() * 2 * Math.PI;
                // The distance is a random value between 0 and the specified radius
                double distance = _random.NextDouble() * radiusKm / 111.0; // Convert to degrees of latitude

                // Calculate the latitude and longitude offset from center using trigonometry
                double latOffset = distance * Math.Cos(angle); // north-south component (latitude)
                double lonOffset = distance * Math.Sin(angle); // east-west component (longitude)

                // Generate age with proper distribution based on Israel demographics (2019 statistics)
                // Different age groups have different probabilities
                int age;
                double ageRandom = _random.NextDouble();
                if (ageRandom < 0.282) // 28.2% are children
                {
                    age = _random.Next(1, 15); // Ages 1-14
                }
                else if (ageRandom < 0.282 + 0.151) // 28.2% + 15.1% = 43.3% for young adults
                {
                    age = _random.Next(15, 25); // Ages 15-24
                }
                else if (ageRandom < 0.282 + 0.151 + 0.364) // 28.2% + 15.1% + 36.4% = 79.7% for adults
                {
                    age = _random.Next(25, 55); // Ages 25-54
                }
                else if (ageRandom < 0.282 + 0.151 + 0.364 + 0.085) // 28.2% + 15.1% + 36.4% + 8.5% = 88.2% for older adults
                {
                    age = _random.Next(55, 65); // Ages 55-64
                }
                else // Remaining 11.8% are elderly
                {
                    age = _random.Next(65, 95); // Ages 65-94
                }

                // Create a new person and add them to the list
                people.Add(new PersonDto
                {
                    Id = i + 1, // Assign an ID starting from 1
                    Age = age, // Assign the randomly generated age
                    Latitude = centerLat + latOffset, // Calculate final latitude
                    Longitude = centerLon + lonOffset // Calculate final longitude
                });
            }

            return people;
        }

        //===================================
        // Helper method to generate shelters
        //===================================

        /// <summary>
        /// Gets shelters from database (if enabled) and generates additional random shelters around a center point
        /// </summary>
        /// <param name="additionalCount">How many additional shelters to generate beyond database ones</param>
        /// <param name="centerLat">Center latitude</param>
        /// <param name="centerLon">Center longitude</param>
        /// <param name="radiusKm">Radius in kilometers within which to generate shelters</param>
        /// <param name="zeroCapacityShelters">Whether to include some shelters with zero capacity (unavailable shelters)</param>
        /// <param name="useDatabaseShelters">Whether to include shelters from the database</param>
        private List<ShelterDto> GenerateShelters(int additionalCount, double centerLat, double centerLon, double radiusKm, bool zeroCapacityShelters = false, bool useDatabaseShelters = true)
        {
            var shelters = new List<ShelterDto>();
            bool usingDatabaseShelters = false;

            // STEP 1: GET SHELTERS FROM DATABASE (if enabled)
            if (useDatabaseShelters)
            {
                try
                {
                    // Connect to database and get all shelters
                    DBservices db = new DBservices();
                    List<Shelter> dbShelters = db.GetAllShelters();

                    // Check if we successfully got shelters from the database
                    if (dbShelters != null && dbShelters.Count > 0)
                    {
                        usingDatabaseShelters = true;
                        Console.WriteLine($"✅ Successfully retrieved {dbShelters.Count} shelters from database");

                        // Convert database shelter objects to ShelterDto objects and add to our list
                        foreach (var dbShelter in dbShelters)
                        {
                            shelters.Add(ConvertToShelterDto(dbShelter));
                            Console.WriteLine($"Added shelter: {dbShelter.name} (ID: {dbShelter.shelter_id})");
                        }
                    }
                    else
                    {
                        // If no shelters were found in the database, log a warning
                        Console.WriteLine("⚠️ No shelters found in database");
                    }
                }
                catch (Exception ex)
                {
                    // Log any database errors but continue with the simulation
                    Console.Error.WriteLine($"⚠️ DATABASE ERROR: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        // Log the inner exception if available
                        Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            else
            {
                // If database shelters are not used, log a message
                Console.WriteLine("Database shelters excluded by user request");
            }

            // STEP 2: GENERATE ADDITIONAL RANDOM SHELTERS
            // Determine the starting ID for generated shelters (after the database ones), if there are any 
            int baseId = shelters.Count > 0 ? shelters.Max(s => s.Id) + 1 : 1;

            // Determine how many shelters to add based on user request
            int sheltersToAdd = usingDatabaseShelters ? additionalCount : additionalCount;

            Console.WriteLine($"Generating {sheltersToAdd} {(usingDatabaseShelters ? "additional" : "")} shelters starting with ID {baseId}");

            // Generate the requested number of additional shelters
            for (int i = 0; i < sheltersToAdd; i++)
            {
                // Generate a random location within the specified radius
                double angle = _random.NextDouble() * 2 * Math.PI; // Random angle in radians, between 0 and 2*PI
                double distance = _random.NextDouble() * radiusKm * 0.7 / 111.0; // Convert to degrees of latitude, limit to 70% of radius

                double latOffset = distance * Math.Cos(angle); // North-south component (latitude)
                double lonOffset = distance * Math.Sin(angle); // East-west component (longitude)

                // Determine capacity - some shelters may have zero capacity if requested
                int capacity;
                if (zeroCapacityShelters && _random.NextDouble() < 0.4)
                {
                    capacity = 0; // 40% chance of a shelter with zero capacity if enabled
                }
                else
                {
                    capacity = _random.Next(3, 8); // Normal capacity between 3 and 7
                }

                // Create the shelter and add it to the list
                shelters.Add(new ShelterDto
                {
                    Id = baseId + i,
                    Name = usingDatabaseShelters
                        // If we have database shelters, name them based on their ID
                        ? (capacity == 0 ? $"Additional Closed Shelter {i + 1}" : $"Additional Shelter {i + 1}")
                        // If we don't have database shelters, name them based on their ID
                        : (capacity == 0 ? $"Closed Shelter {i + 1}" : $"Shelter {i + 1}"),
                    Latitude = centerLat + latOffset, // Calculate final latitude
                    Longitude = centerLon + lonOffset, // Calculate final longitude
                    Capacity = capacity
                });
            }

            return shelters;
        }




        //============================
        // All Cube Helpers Methods
        //============================

        /// <summary>
        /// Calculate the cube key for a given coordinate
        /// </summary>
        private string GetCubeKey(double latitude, double longitude, double centerLat)
        {
            // Calculate longitude cube size based on latitude (it varies by latitude)
            double cubeSizeLon = CUBE_SIZE_KM / (111.0 * Math.Cos(centerLat * Math.PI / 180));

            // Calculate grid indices
            int latIndex = (int)Math.Floor((latitude - centerLat) / CUBE_SIZE_LAT);
            int lonIndex = (int)Math.Floor((longitude - centerLat) / cubeSizeLon);

            return $"{latIndex}_{lonIndex}";
        }

        /// <summary>
        /// Get all 9 surrounding cube keys (including the center cube)
        /// </summary>
        private List<string> GetSurroundingCubes(double latitude, double longitude, double centerLat)
        {
            var cubes = new List<string>();
            double cubeSizeLon = CUBE_SIZE_KM / (111.0 * Math.Cos(centerLat * Math.PI / 180));

            // Get the center cube indices
            int centerLatIndex = (int)Math.Floor((latitude - centerLat) / CUBE_SIZE_LAT);
            int centerLonIndex = (int)Math.Floor((longitude - centerLat) / cubeSizeLon);

            // Add the 9 cubes (3x3 grid)
            for (int latOffset = -1; latOffset <= 1; latOffset++)
            {
                for (int lonOffset = -1; lonOffset <= 1; lonOffset++)
                {
                    int latIndex = centerLatIndex + latOffset;
                    int lonIndex = centerLonIndex + lonOffset;
                    cubes.Add($"{latIndex}_{lonIndex}");
                }
            }

            // Check if person is near edge and add buffer cubes
            double latInCube = ((latitude - centerLat) / CUBE_SIZE_LAT) - centerLatIndex;
            double lonInCube = ((longitude - centerLat) / cubeSizeLon) - centerLonIndex;

            const double EDGE_THRESHOLD = 0.25; // 25% from edge = 50m from edge

            // Add extra cubes if near edges
            if (latInCube < EDGE_THRESHOLD) // Near bottom edge
            {
                for (int lonOffset = -1; lonOffset <= 1; lonOffset++)
                {
                    cubes.Add($"{centerLatIndex - 2}_{centerLonIndex + lonOffset}");
                }
            }
            else if (latInCube > (1 - EDGE_THRESHOLD)) // Near top edge
            {
                for (int lonOffset = -1; lonOffset <= 1; lonOffset++)
                {
                    cubes.Add($"{centerLatIndex + 2}_{centerLonIndex + lonOffset}");
                }
            }

            if (lonInCube < EDGE_THRESHOLD) // Near left edge
            {
                for (int latOffset = -1; latOffset <= 1; latOffset++)
                {
                    cubes.Add($"{centerLatIndex + latOffset}_{centerLonIndex - 2}");
                }
            }
            else if (lonInCube > (1 - EDGE_THRESHOLD)) // Near right edge
            {
                for (int latOffset = -1; latOffset <= 1; latOffset++)
                {
                    cubes.Add($"{centerLatIndex + latOffset}_{centerLonIndex + 2}");
                }
            }

            return cubes.Distinct().ToList(); // Remove duplicates
        }

        /// <summary>
        /// Build an index mapping cubes to shelters for fast lookup
        /// </summary>
        private Dictionary<string, List<int>> BuildCubeToShelterIndex(List<ShelterDto> shelters, double centerLat)
        {
            var cubeToShelters = new Dictionary<string, List<int>>();

            foreach (var shelter in shelters)
            {
                string cubeKey = GetCubeKey(shelter.Latitude, shelter.Longitude, centerLat);

                if (!cubeToShelters.ContainsKey(cubeKey))
                {
                    cubeToShelters[cubeKey] = new List<int>();
                }

                cubeToShelters[cubeKey].Add(shelter.Id);
            }

            _logger.LogInformation($"Built cube index with {cubeToShelters.Count} cubes containing shelters");

            return cubeToShelters;
        }

        /// <summary>
        /// Get all shelters in the surrounding cubes for a person
        /// </summary>
        private List<ShelterDto> GetSheltersInSurroundingCubes(
            double personLat,
            double personLon,
            List<ShelterDto> allShelters,
            Dictionary<string, List<int>> cubeToShelters,
            double centerLat)
        {
            var surroundingCubes = GetSurroundingCubes(personLat, personLon, centerLat);
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

            // Return only the shelters in the surrounding cubes
            return allShelters.Where(s => shelterIds.Contains(s.Id)).ToList();
        }

        //=============================================================================
        // Assign People To Shelters Optimal - New Algorithm, based on Priority Queue
        //=============================================================================

        /// <summary>
        /// Main algorithm for assigning people to shelters based on distance and priorities
        /// The algorithm considers walking speed, time constraints, and may prioritize children and elderly
        /// </summary>
        /// <param name="people">List of people to assign</param>
        /// <param name="shelters">List of available shelters</param>
        /// <param name="prioritySettings">Settings for age-based priority (children, elderly)</param>
        // /// <returns>Dictionary of assignments with person ID as key and AssignmentDto as value</returns>
        private Dictionary<int, AssignmentDto> AssignPeopleToSheltersOptimal(
        List<PersonDto> people,
        List<ShelterDto> shelters,
        PrioritySettingsDto prioritySettings)
        {
            Console.WriteLine("Starting optimal shelter assignment algorithm with priority queue and cube optimization...");

            // STEP 1: DEFINE CONSTANTS
            const double MAX_TRAVEL_TIME_MINUTES = 1.0; // Maximum travel time in minutes
            const double WALKING_SPEED_KM_PER_MINUTE = 0.6; // ~5 km/h = 0.6 km/min
            const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE; // Should be about 0.6km
            const double CENTER_LAT = 31.2518; // Beer Sheva center for cube calculations

            Console.WriteLine($"Maximum distance constraint: {MAX_DISTANCE_KM:F4} km");

            // STEP 2: BUILD CUBE INDEX FOR SHELTERS
            var cubeToShelters = BuildCubeToShelterIndex(shelters, CENTER_LAT);

            // STEP 3: INITIALIZE TRACKING STRUCTURES
            var assignments = new Dictionary<int, AssignmentDto>();
            var assignedPeople = new HashSet<int>();
            var shelterCapacity = shelters.ToDictionary(s => s.Id, s => s.Capacity);

            // STEP 4: CREATE PRIORITY QUEUE
            // Lower priority value = higher priority (processed first)
            var pq = new PriorityQueue<AssignmentOption, double>();

            // Track API calls saved
            int totalPossibleChecks = people.Count * shelters.Count;
            int actualChecks = 0;

            // STEP 5: POPULATE PRIORITY QUEUE WITH CUBE-FILTERED ASSIGNMENTS
            Console.WriteLine("Building priority queue with cube-filtered person-shelter pairs...");

            foreach (var person in people)
            {
                // Get only shelters in surrounding cubes
                var nearbyShelters = GetSheltersInSurroundingCubes(
                    person.Latitude,
                    person.Longitude,
                    shelters,
                    cubeToShelters,
                    CENTER_LAT
                );

                Console.WriteLine($"Person {person.Id}: Checking {nearbyShelters.Count} shelters in nearby cubes (instead of {shelters.Count} total)");
                actualChecks += nearbyShelters.Count;

                foreach (var shelter in nearbyShelters)
                {
                    // Calculate distance between person and shelter
                    double distance = CalculateDistance(
                        person.Latitude, person.Longitude,
                        shelter.Latitude, shelter.Longitude);

                    // Only consider shelters within maximum distance
                    if (distance <= MAX_DISTANCE_KM)
                    {
                        // Create assignment option
                        var option = new AssignmentOption
                        {
                            PersonId = person.Id,
                            ShelterId = shelter.Id,
                            Distance = distance,
                            IsReachable = true,
                            VulnerabilityScore = CalculateVulnerabilityScore(person.Age)
                        };

                        // Calculate priority
                        double priority = distance;

                        // Adjust priority for vulnerable people if enabled
                        if (prioritySettings?.EnableAgePriority == true)
                        {
                            // Lower priority value = higher priority
                            // Vulnerable people get a boost (lower priority value)
                            priority -= option.VulnerabilityScore * 0.01;
                        }

                        // Add to priority queue
                        pq.Enqueue(option, priority);
                    }
                }
            }

            Console.WriteLine($"Cube optimization saved {totalPossibleChecks - actualChecks} distance calculations ({((double)(totalPossibleChecks - actualChecks) / totalPossibleChecks * 100):F1}% reduction)");
            Console.WriteLine($"Priority queue built with {pq.Count} possible assignments");

            // STEP 6: PROCESS ASSIGNMENTS IN PRIORITY ORDER
            Console.WriteLine("Processing assignments in priority order...");
            int processedCount = 0;

            while (pq.Count > 0)
            {
                // Get the highest priority assignment (shortest distance, adjusted for vulnerability)
                var option = pq.Dequeue();
                processedCount++;

                // Skip if person is already assigned
                if (assignedPeople.Contains(option.PersonId))
                    continue;

                // Skip if shelter is full
                if (shelterCapacity[option.ShelterId] <= 0)
                    continue;

                // Make the assignment
                assignments[option.PersonId] = new AssignmentDto
                {
                    PersonId = option.PersonId,
                    ShelterId = option.ShelterId,
                    Distance = option.Distance
                };

                // Update tracking
                assignedPeople.Add(option.PersonId);
                shelterCapacity[option.ShelterId]--;

                // Find the person's age for logging
                var person = people.First(p => p.Id == option.PersonId);
                string ageGroup = person.Age >= 70 ? "elderly" : person.Age <= 12 ? "child" : "adult";

                Console.WriteLine($"Assigned person {option.PersonId} ({ageGroup}, age {person.Age}) to shelter {option.ShelterId} (distance: {option.Distance * 1000:F0}m)");
            }

            // STEP 7: REPORT UNASSIGNED PEOPLE
            var unassignedPeople = people.Where(p => !assignedPeople.Contains(p.Id)).ToList();
            if (unassignedPeople.Any())
            {
                Console.WriteLine($"\n{unassignedPeople.Count} people could not be assigned:");
                foreach (var person in unassignedPeople)
                {
                    // Find their nearest shelter for reporting
                    var nearestShelter = shelters
                        .OrderBy(s => CalculateDistance(person.Latitude, person.Longitude, s.Latitude, s.Longitude))
                        .FirstOrDefault();

                    if (nearestShelter != null)
                    {
                        double distance = CalculateDistance(
                            person.Latitude, person.Longitude,
                            nearestShelter.Latitude, nearestShelter.Longitude);

                        Console.WriteLine($"Person {person.Id} (age {person.Age}) - nearest shelter {nearestShelter.Id} at {distance * 1000:F0}m");
                    }
                }
            }

            // STEP 8: FINAL STATISTICS
            Console.WriteLine($"\nAssignment complete:");
            Console.WriteLine($"- Processed {processedCount} assignment options");
            Console.WriteLine($"- Assigned {assignments.Count} people");
            Console.WriteLine($"- Unassigned {people.Count - assignments.Count} people");

            // STEP 9: OPTIMIZATION PHASE
            if (assignments.Count > 0)
            {
                Console.WriteLine("\nStarting optimization phase...");
                assignments = OptimizeAssignments(assignments, people, shelters);
            }

            return assignments;
        }



        //===================================
        // Optimize Assignments
        //===================================

        /// <summary>
        /// Optimizes shelter assignments by swapping people between shelters to minimize total distance
        /// This is a post-processing step after the initial assignment
        /// </summary>
        /// <param name="initialAssignments">The initial assignments to optimize</param>
        /// <param name="people">List of all people</param>
        /// <param name="shelters">List of all shelters</param>
        private Dictionary<int, AssignmentDto> OptimizeAssignments(
            Dictionary<int, AssignmentDto> initialAssignments,
            List<PersonDto> people,
            List<ShelterDto> shelters)
        {
            Console.WriteLine("Starting post-assignment optimization phase...");

            // Add early exit for large datasets, if required
            // if (people.Count * shelters.Count > 10000)
            // {
            //     Console.WriteLine("Dataset too large for optimization, skipping...");
            //     return initialAssignments;
            // }

            // STEP 1: CREATE A WORKING COPY OF ASSIGNMENTS
            // We'll modify this copy throughout the optimization
            var optimizedAssignments = new Dictionary<int, AssignmentDto>(initialAssignments);

            // STEP 2: CREATE LOOKUP DICTIONARIES
            // For faster access to people and shelter data
            var personLookup = people.ToDictionary(p => p.Id);
            var shelterLookup = shelters.ToDictionary(s => s.Id);

            // STEP 3: TRACK SHELTER ASSIGNMENTS
            // Keep track of which people are assigned to each shelter
            var shelterAssignments = new Dictionary<int, List<int>>();
            foreach (var shelter in shelters)
            {
                shelterAssignments[shelter.Id] = new List<int>();
            }

            // Populate the shelter assignments tracking
            foreach (var assignment in optimizedAssignments)
            {
                int personId = assignment.Key;
                int shelterId = assignment.Value.ShelterId;
                shelterAssignments[shelterId].Add(personId);
            }

            // STEP 4: PRIORITIZE ELDERLY OPTIMIZATION FIRST
            // This is the new step - optimize elderly assignments before general optimization
            int elderlyOptimizationCount = 0;

            // Find all elderly people who have been assigned
            var elderlyIds = personLookup.Values
                .Where(p => p.Age >= 70) // Using 70 as elderly threshold - adjust if you have different definition
                .Select(p => p.Id)
                .Where(id => optimizedAssignments.ContainsKey(id))
                .ToList();

            Console.WriteLine($"Attempting to optimize shelter assignments for {elderlyIds.Count} elderly people...");

            // Try to optimize for each elderly person
            foreach (var elderlyId in elderlyIds)
            {
                var currentAssignment = optimizedAssignments[elderlyId];
                int currentShelterId = currentAssignment.ShelterId;
                var elderly = personLookup[elderlyId];

                // Calculate current distance
                double currentDistance = currentAssignment.Distance;

                // Find all possible better shelters for this elderly person
                foreach (var shelter in shelters)
                {
                    // Skip if it's the same shelter or it's full
                    if (shelter.Id == currentShelterId ||
                        shelterAssignments[shelter.Id].Count >= shelter.Capacity)
                        continue;

                    // Calculate distance to this potential shelter
                    double newDistance = CalculateDistance(
                        elderly.Latitude, elderly.Longitude,
                        shelter.Latitude, shelter.Longitude);

                    // If this shelter is closer, try to assign the elderly person to it
                    if (newDistance < currentDistance)
                    {
                        // Assign elderly to the closer shelter
                        optimizedAssignments[elderlyId] = new AssignmentDto
                        {
                            PersonId = elderlyId,
                            ShelterId = shelter.Id,
                            Distance = newDistance
                        };

                        // Update tracking data
                        shelterAssignments[currentShelterId].Remove(elderlyId);
                        shelterAssignments[shelter.Id].Add(elderlyId);

                        elderlyOptimizationCount++;
                        Console.WriteLine($"Optimized: Elderly person {elderlyId} moved from shelter {currentShelterId} to {shelter.Id}, " +
                                         $"reducing distance from {currentDistance * 1000:F0}m to {newDistance * 1000:F0}m");

                        // Stop looking for better shelters for this elderly person
                        break;
                    }
                }
            }

            Console.WriteLine($"Elderly optimization complete: {elderlyOptimizationCount} elderly people relocated to closer shelters");

            // STEP 5: RUN OPTIMIZATION ITERATIONS
            // Track if any improvements were made
            bool improvementFound;
            int swapCount = 0;
            double totalDistanceImprovement = 0;

            // Maximum iterations limit - for better performance but less %, reduce the number of max iterations
            int maxIterations = 10;
            int currentIteration = 0;

            // Log the start of the optimization process
            Console.WriteLine("Starting swap optimization iterations...");

            // Repeat until no more improvements can be found
            do
            {
                improvementFound = false;
                currentIteration++;

                // Use ConcurrentBag to store potential swaps from parallel execution
                var potentialSwaps = new ConcurrentBag<SwapCandidate>();

                // STEP 5A: ITERATE THROUGH ALL POSSIBLE SHELTER PAIRS - PARALLELIZE
                Parallel.For(0, shelters.Count, i =>
        {
            int shelter1Id = shelters[i].Id;

            // Skip if this shelter has no assigned people
            if (!shelterAssignments.ContainsKey(shelter1Id) || shelterAssignments[shelter1Id].Count == 0)
                return;

            for (int j = i + 1; j < shelters.Count; j++)
            {
                int shelter2Id = shelters[j].Id;

                // Skip if this shelter has no assigned people
                if (!shelterAssignments.ContainsKey(shelter2Id) || shelterAssignments[shelter2Id].Count == 0)
                    continue;

                // Evaluate all possible swaps between these two shelters
                foreach (int person1Id in shelterAssignments[shelter1Id])
                {
                    var person1 = personLookup[person1Id];
                    double person1CurrentDistance = optimizedAssignments[person1Id].Distance;

                    foreach (int person2Id in shelterAssignments[shelter2Id])
                    {
                        var person2 = personLookup[person2Id];
                        double person2CurrentDistance = optimizedAssignments[person2Id].Distance;

                        // Calculate potential swap distances
                        double person1NewDistance = CalculateDistance(
                            person1.Latitude, person1.Longitude,
                            shelterLookup[shelter2Id].Latitude, shelterLookup[shelter2Id].Longitude);

                        double person2NewDistance = CalculateDistance(
                            person2.Latitude, person2.Longitude,
                            shelterLookup[shelter1Id].Latitude, shelterLookup[shelter1Id].Longitude);

                        double currentTotalDistance = person1CurrentDistance + person2CurrentDistance;
                        double newTotalDistance = person1NewDistance + person2NewDistance;
                        double improvement = currentTotalDistance - newTotalDistance;

                        bool person1IsElderly = person1.Age >= 70;
                        bool person2IsElderly = person2.Age >= 70;

                        if (person1IsElderly && person1NewDistance < person1CurrentDistance)
                        {
                            // Give extra weight to swaps that benefit elderly people
                            improvement += (person1CurrentDistance - person1NewDistance) * 0.5;
                        }

                        if (person2IsElderly && person2NewDistance < person2CurrentDistance)
                        {
                            // Give extra weight to swaps that benefit elderly people
                            improvement += (person2CurrentDistance - person2NewDistance) * 0.5;
                        }

                        // Store swap if it improves total distance
                        if (improvement > 0.001) // Only consider improvements > 1 meter
                        {
                            potentialSwaps.Add(new SwapCandidate
                            {
                                Person1Id = person1Id,
                                Person2Id = person2Id,
                                Shelter1Id = shelter1Id,
                                Shelter2Id = shelter2Id,
                                Person1NewDistance = person1NewDistance,
                                Person2NewDistance = person2NewDistance,
                                Improvement = improvement
                            });
                        }
                    }
                }
            }
        });

                // Process the best swaps sequentially to avoid conflicts
                var bestSwaps = potentialSwaps
                    .OrderByDescending(s => s.Improvement)
                    .Take(10) // Process top 10 swaps per iteration
                    .ToList();

                foreach (var swap in bestSwaps)
                {
                    // Verify the swap is still valid (people haven't been moved by another swap)
                    if (optimizedAssignments[swap.Person1Id].ShelterId == swap.Shelter1Id &&
                        optimizedAssignments[swap.Person2Id].ShelterId == swap.Shelter2Id)
                    {
                        // Apply the swap
                        optimizedAssignments[swap.Person1Id] = new AssignmentDto
                        {
                            PersonId = swap.Person1Id,
                            ShelterId = swap.Shelter2Id,
                            Distance = swap.Person1NewDistance
                        };

                        optimizedAssignments[swap.Person2Id] = new AssignmentDto
                        {
                            PersonId = swap.Person2Id,
                            ShelterId = swap.Shelter1Id,
                            Distance = swap.Person2NewDistance
                        };

                        // Update shelter assignments
                        shelterAssignments[swap.Shelter1Id].Remove(swap.Person1Id);
                        shelterAssignments[swap.Shelter1Id].Add(swap.Person2Id);
                        shelterAssignments[swap.Shelter2Id].Remove(swap.Person2Id);
                        shelterAssignments[swap.Shelter2Id].Add(swap.Person1Id);

                        swapCount++;
                        totalDistanceImprovement += swap.Improvement;
                        improvementFound = true;

                        Console.WriteLine($"Swap {swapCount}: Persons {swap.Person1Id} and {swap.Person2Id} between shelters {swap.Shelter1Id} and {swap.Shelter2Id}, saving {swap.Improvement:F4} km");
                    }
                }

                // Check if we should continue
                if (currentIteration >= maxIterations)
                {
                    Console.WriteLine($"Reached maximum iterations ({maxIterations})");
                    break;
                }

            } while (improvementFound);

            Console.WriteLine($"Optimization complete: Made {swapCount} swaps, reducing total distance by {totalDistanceImprovement:F4} km");

            return optimizedAssignments;
        }


        //==========================================================================================
        // Run Simulation with Walking Distances
        // This method runs the simulation with walking distances calculated from Google Maps API
        //==========================================================================================

        [HttpPost("run-with-walking-distances")]
        public async Task<ActionResult<SimulationResponseDto>> RunSimulationWithWalkingDistances([FromBody] SimulationRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Invalid simulation request data");
                }

                // Set the center coordinates for the simulation
                double centerLat = 31.2518;
                double centerLon = 34.7913;

                // STEP 1: Generate or use custom people and shelters (same as before)
                List<PersonDto> people;
                if (request.UseCustomPeople && request.CustomPeople != null && request.CustomPeople.Any())
                {
                    people = request.CustomPeople;
                }
                else
                {
                    people = GeneratePeople(request.PeopleCount, centerLat, centerLon, request.RadiusKm);
                }

                List<ShelterDto> shelters;
                if (request.UseCustomShelters && request.CustomShelters != null && request.CustomShelters.Any())
                {
                    shelters = request.CustomShelters;
                }
                else
                {
                    shelters = GenerateShelters(
                        request.ShelterCount,
                        centerLat,
                        centerLon,
                        request.RadiusKm,
                        request.ZeroCapacityShelters,
                        request.UseDatabaseShelters);
                }

                // STEP 2: Get walking distances from Google Maps
                _logger.LogInformation("Fetching walking distances from Google Maps...");
                var walkingDistances = await _googleMapsService.CalculateShelterDistancesAsync(people, shelters);

                // STEP 3: Run assignment algorithm with walking distances
                var assignments = await AssignPeopleToSheltersWithWalkingDistance(
                    people,
                    shelters,
                    request.PrioritySettings,
                    walkingDistances);

                // STEP 3.5: Get actual walking routes (ADD THIS SECTION)
                _logger.LogInformation("Fetching walking routes from Google Maps...");
                var routes = await _googleMapsService.GetRoutesForPeople(people, shelters, assignments);

                // Update assignments with route information
                foreach (var assignment in assignments)
                {
                    var routeKey = $"{assignment.Key}-{assignment.Value.ShelterId}";
                    if (routes.ContainsKey(routeKey) && routes[routeKey].Routes?.Any() == true)
                    {
                        var route = routes[routeKey].Routes.First();
                        if (!string.IsNullOrEmpty(route.OverviewPolyline))
                        {
                            assignment.Value.RoutePolyline = route.OverviewPolyline;
                            _logger.LogInformation($"Added route polyline for person {assignment.Key} to shelter {assignment.Value.ShelterId}");
                        }
                    }
                }

                // STEP 4: Handle unassigned people (same as before but with walking distances)
                var unassignedPeople = people.Where(p => !assignments.ContainsKey(p.Id)).ToList();
                foreach (var person in unassignedPeople)
                {
                    ShelterDto nearestShelter = null;
                    double nearestDistance = double.MaxValue;

                    foreach (var shelter in shelters)
                    {
                        // Try to use walking distance first
                        var walkingDist = walkingDistances.ContainsKey(person.Id.ToString()) &&
                                         walkingDistances[person.Id.ToString()].ContainsKey(shelter.Id.ToString())
                            ? walkingDistances[person.Id.ToString()][shelter.Id.ToString()]
                            : -1;

                        // Fall back to air distance if walking distance not available
                        double distance = walkingDist > 0 ? walkingDist : CalculateDistance(
                            person.Latitude, person.Longitude,
                            shelter.Latitude, shelter.Longitude);

                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestShelter = shelter;
                        }
                    }

                    if (nearestShelter != null)
                    {
                        person.NearestShelterId = nearestShelter.Id;
                        person.NearestShelterDistance = nearestDistance;
                    }
                }

                // STEP 5: Calculate statistics
                var assignedCount = assignments.Count;
                var averageDistance = assignments.Values.Count > 0 ? assignments.Values.Average(a => a.Distance) : 0;
                var maxDistance = assignments.Values.Count > 0 ? assignments.Values.Max(a => a.Distance) : 0;

                // STEP 6: Prepare response
                var response = new SimulationResponseDto
                {
                    People = people,
                    Shelters = shelters,
                    Assignments = assignments,
                    Statistics = new SimulationStatisticsDto
                    {
                        ExecutionTimeMs = 0,
                        AssignedCount = assignedCount,
                        UnassignedCount = people.Count - assignedCount,
                        AssignmentPercentage = people.Count > 0 ? (double)assignedCount / people.Count : 0,
                        TotalShelterCapacity = shelters.Sum(s => s.Capacity),
                        ShelterUsagePercentage = shelters.Sum(s => s.Capacity) > 0
                            ? (double)assignedCount / shelters.Sum(s => s.Capacity) * 100
                            : 0,
                        AverageDistance = averageDistance,
                        MaxDistance = maxDistance,
                        MinDistance = assignments.Values.Count > 0 ? assignments.Values.Min(a => a.Distance) : 0
                    }
                };

                foreach (var assignment in assignments)
                {
                    _logger.LogInformation($"Final assignment: Person {assignment.Key} -> Shelter {assignment.Value.ShelterId}, Distance: {assignment.Value.Distance}km, IsWalkingDistance: {assignment.Value.IsWalkingDistance}");
                }


                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RunSimulationWithWalkingDistances");
                return StatusCode(500, $"An error occurred while running the simulation: {ex.Message}");
            }
        }

        //=====================================================================================================
        // Assign People to Shelters with Walking Distance
        // This method assigns people to shelters based on walking distances calculated from Google Maps API
        //=====================================================================================================

        private async Task<Dictionary<int, AssignmentDto>> AssignPeopleToSheltersWithWalkingDistance(
    List<PersonDto> people,
    List<ShelterDto> shelters,
    PrioritySettingsDto prioritySettings,
    Dictionary<string, Dictionary<string, double>> walkingDistances)
        {
            _logger.LogInformation("Starting shelter assignment with walking distances and cube optimization...");

            // Constants for walking time constraints
            const double MAX_TRAVEL_TIME_MINUTES = 1.0; // Maximum travel time in minutes
            const double WALKING_SPEED_KM_PER_MINUTE = 0.6; // ~5 km/h = 0.6 km/min
            const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE; // Should be about 0.6km
            const double CENTER_LAT = 31.2518; // Beer Sheva center for cube calculations

            // Build cube index for shelters
            var cubeToShelters = BuildCubeToShelterIndex(shelters, CENTER_LAT);

            // LOG THE WALKING DISTANCES TO SEE WHAT YOU'RE GETTING
            int validDistances = 0;
            int invalidDistances = 0;

            foreach (var personEntry in walkingDistances)
            {
                foreach (var shelterEntry in personEntry.Value)
                {
                    if (shelterEntry.Value > 0)
                    {
                        validDistances++;
                        _logger.LogInformation($"Valid distance: Person {personEntry.Key} to Shelter {shelterEntry.Key} = {shelterEntry.Value} km");
                    }
                    else
                    {
                        invalidDistances++;
                        _logger.LogWarning($"Invalid distance: Person {personEntry.Key} to Shelter {shelterEntry.Key} = {shelterEntry.Value}");
                    }
                }
            }

            _logger.LogInformation($"Walking distances summary: {validDistances} valid, {invalidDistances} invalid");

            // Initialize tracking structures
            var assignments = new Dictionary<int, AssignmentDto>();
            var assignedPeople = new HashSet<int>();
            var shelterCapacity = shelters.ToDictionary(s => s.Id, s => s.Capacity);

            foreach (var shelter in shelters)
            {
                _logger.LogInformation($"Shelter {shelter.Id} has capacity {shelter.Capacity}");
            }

            // Create priority queue
            var pq = new PriorityQueue<AssignmentOption, double>();

            // Track API calls saved
            int totalPossibleCalls = people.Count * shelters.Count;
            int actualCalls = 0;

            // Populate priority queue with cube-filtered walking distances
            foreach (var person in people)
            {
                // Get only shelters in surrounding cubes
                var nearbyShelters = GetSheltersInSurroundingCubes(
                    person.Latitude,
                    person.Longitude,
                    shelters,
                    cubeToShelters,
                    CENTER_LAT
                );

                _logger.LogInformation($"Person {person.Id}: Checking {nearbyShelters.Count} shelters in nearby cubes (instead of {shelters.Count} total)");
                actualCalls += nearbyShelters.Count;

                foreach (var shelter in nearbyShelters)
                {
                    // First check air distance as a quick filter
                    double airDistance = CalculateDistance(
                        person.Latitude, person.Longitude,
                        shelter.Latitude, shelter.Longitude);

                    // Skip if air distance is already too far (with buffer for walking routes)
                    if (airDistance > MAX_DISTANCE_KM * 1.5) // 1.5x buffer for walking vs air distance
                    {
                        _logger.LogInformation($"Skipping shelter {shelter.Id} for person {person.Id} - air distance {airDistance:F3}km exceeds buffer");
                        continue;
                    }

                    double distance;

                    // Try to get walking distance from pre-calculated data
                    if (walkingDistances.ContainsKey(person.Id.ToString()) &&
                        walkingDistances[person.Id.ToString()].ContainsKey(shelter.Id.ToString()))
                    {
                        distance = walkingDistances[person.Id.ToString()][shelter.Id.ToString()];

                        // Skip if no route available (indicated by -1)
                        if (distance < 0)
                        {
                            _logger.LogInformation($"No walking route available from person {person.Id} to shelter {shelter.Id}");
                            continue;
                        }
                    }
                    else
                    {
                        // Fall back to air distance calculation
                        _logger.LogInformation($"Using air distance for person {person.Id} to shelter {shelter.Id}");
                        distance = airDistance;
                    }

                    // Only consider shelters within maximum walking distance
                    if (distance <= MAX_DISTANCE_KM)
                    {
                        var option = new AssignmentOption
                        {
                            PersonId = person.Id,
                            ShelterId = shelter.Id,
                            Distance = distance,
                            IsReachable = true,
                            VulnerabilityScore = CalculateVulnerabilityScore(person.Age)
                        };

                        // Calculate priority
                        double priority = distance;

                        // Adjust priority for vulnerable people if enabled
                        if (prioritySettings?.EnableAgePriority == true)
                        {
                            priority -= option.VulnerabilityScore * 0.01;
                        }

                        pq.Enqueue(option, priority);
                    }
                }
            }

            _logger.LogInformation($"API calls saved: {totalPossibleCalls - actualCalls} ({((double)(totalPossibleCalls - actualCalls) / totalPossibleCalls * 100):F1}% reduction)");
            _logger.LogInformation($"Priority queue built with {pq.Count} possible assignments using walking distances");

            // Process assignments in priority order
            int processedCount = 0;
            while (pq.Count > 0)
            {
                var option = pq.Dequeue();
                processedCount++;

                if (assignedPeople.Contains(option.PersonId))
                    continue;

                if (shelterCapacity[option.ShelterId] <= 0)
                    continue;

                // Make the assignment
                assignments[option.PersonId] = new AssignmentDto
                {
                    PersonId = option.PersonId,
                    ShelterId = option.ShelterId,
                    Distance = option.Distance,
                    IsWalkingDistance = true
                };

                _logger.LogInformation($"Assignment made: Person {option.PersonId} -> Shelter {option.ShelterId}, Total assignments: {assignments.Count}");

                assignedPeople.Add(option.PersonId);
                shelterCapacity[option.ShelterId]--;

                var person = people.First(p => p.Id == option.PersonId);
                string ageGroup = person.Age >= 70 ? "elderly" : person.Age <= 12 ? "child" : "adult";

                _logger.LogInformation($"Assigned person {option.PersonId} ({ageGroup}, age {person.Age}) to shelter {option.ShelterId} (walking distance: {option.Distance * 1000:F0}m)");
            }

            _logger.LogInformation($"Assignment complete: {assignments.Count} assigned, {people.Count - assignments.Count} unassigned");

            _logger.LogInformation($"Returning {assignments.Count} assignments");
            foreach (var kvp in assignments)
            {
                _logger.LogInformation($"Assignment: Person {kvp.Key} -> Shelter {kvp.Value.ShelterId} at {kvp.Value.Distance}km");
            }

            return assignments;
        }

        //===================================
        // Extras and Helpers
        //===================================

        /**
         * Helper class to store assignment options with additional metadata
         * Used internally by the assignment algorithm to track options for each person
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
         * Used for prioritizing vulnerable populations in shelter assignments
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
            else
            {
                // Adults (13-59): lowest priority
                return 6;
            }
        }

        /**
         * Calculates distance between two geographical points using the Haversine formula.
         * The Haversine formula is a mathematical equation used to calculate the shortest distance, 
         * between two points on the surface of a sphere (like Earth) using their latitude and longitude coordinates.
         * This gives the "as-the-crow-flies" distance between two points on earth
         */
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Earth radius in km
            var dLat = (lat2 - lat1) * Math.PI / 180; // Convert difference to radians
            var dLon = (lon2 - lon1) * Math.PI / 180;

            // Haversine formula for calculating great-circle distance between two points on a sphere
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c; // Distance in kilometers
        }

        /**
         * Helper class to track shelter capacity during assignment process
         * Includes additional metadata about each shelter
         */
        private class ShelterWithCapacity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int Capacity { get; set; }
            public int RemainingCapacity { get; set; }
        }

        /**
         * Converts a database Shelter object to a ShelterDto object
         * Used when getting shelters from the database
         */
        private ShelterDto ConvertToShelterDto(Shelter dbShelter)
        {
            return new ShelterDto
            {
                Id = dbShelter.shelter_id,
                Name = dbShelter.name ?? $"Shelter {dbShelter.shelter_id}", // Handle null names
                Latitude = dbShelter.latitude,
                Longitude = dbShelter.longitude,
                Capacity = dbShelter.capacity
            };
        }
    }

    // DATA TRANSFER OBJECTS (DTOs) - These classes define the structure of data sent to and from the API

    /**
     * SimulationRequestDto - Structure of the request sent to the API
     * Contains all parameters needed to run a simulation
     */
    public class SimulationRequestDto
    {
        public int PeopleCount { get; set; } = 1000; // Number of people to generate by default
        public int ShelterCount { get; set; } = 20; // Number of shelters to generate by default
        public double CenterLatitude { get; set; } = 31.2518; // Default center latitude (Beer Sheva)
        public double CenterLongitude { get; set; } = 34.7913; // Default center longitude (Beer Sheva)
        public double RadiusKm { get; set; } = 5; // Default radius in kilometers
        public PrioritySettingsDto PrioritySettings { get; set; } = new PrioritySettingsDto(); // Settings for priority-based assignment
        public bool UseCustomPeople { get; set; } = false; // Whether to use provided custom people instead of generating them
        public List<PersonDto> CustomPeople { get; set; } = new List<PersonDto>(); // List of custom people if provided
        public bool ZeroCapacityShelters { get; set; } = false; // Whether to include some shelters with zero capacity
        public bool UseCustomShelters { get; set; } = false; // Whether to use provided custom shelters instead of generating them
        public List<ShelterDto> CustomShelters { get; set; } = new List<ShelterDto>(); // List of custom shelters if provided
        public bool UseDatabaseShelters { get; set; } = true; // Whether to include shelters from the database
    }

    /**
     * SimulationResponseDto - Structure of the response returned by the API
     * Contains all results of the simulation
     */
    public class SimulationResponseDto
    {
        public List<PersonDto> People { get; set; } // List of all people in the simulation
        public List<ShelterDto> Shelters { get; set; } // List of all shelters in the simulation
        public Dictionary<int, AssignmentDto> Assignments { get; set; } // Mapping of person IDs to their shelter assignments
        public SimulationStatisticsDto Statistics { get; set; } // Statistics about the simulation results
    }



    /**
     * PrioritySettingsDto - Settings for prioritizing vulnerable populations
     * Used to configure how age affects priority in shelter assignments
     */
    public class PrioritySettingsDto
    {
        public bool EnableAgePriority { get; set; } = true; // Whether to prioritize by age (children and elderly)
        public int ChildMaxAge { get; set; } = 12; // Maximum age to be considered a child
        public int ElderlyMinAge { get; set; } = 70; // Minimum age to be considered elderly
    }

    /**
     * SimulationStatisticsDto - Statistics about the simulation results
     * Used for analysis and visualization
     */
    public class SimulationStatisticsDto
    {
        public long ExecutionTimeMs { get; set; } // How long the simulation took to run
        public int AssignedCount { get; set; } // Number of people successfully assigned to shelters
        public int UnassignedCount { get; set; } // Number of people who couldn't be assigned
        public double AssignmentPercentage { get; set; } // Percentage of people assigned
        public int TotalShelterCapacity { get; set; } // Total capacity of all shelters
        public double AverageDistance { get; set; } // Average distance from people to their assigned shelters
        public double MaxDistance { get; set; } // Maximum distance any person has to travel
        public double MinDistance { get; set; } // Minimum distance any person has to travel
        public double ShelterUsagePercentage { get; set; } // Percentage of shelter capacity utilized

    }

    /**
    * helper class for storing swap candidates
    **/
    public class SwapCandidate
    {
        public int Person1Id { get; set; }
        public int Person2Id { get; set; }
        public int Shelter1Id { get; set; }
        public int Shelter2Id { get; set; }
        public double Person1NewDistance { get; set; }
        public double Person2NewDistance { get; set; }
        public double Improvement { get; set; }
    }
}
