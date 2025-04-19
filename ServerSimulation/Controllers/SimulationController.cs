using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using ServerSimulation.Models;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Builder;

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
                // Either use custom shelters provided in the request, or get them from database and/or generate random ones
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
                var assignments = AssignPeopleToShelters(people, shelters, request.PrioritySettings);

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

        //===================================
        // Assign People To Shelters
        //===================================

        /// <summary>
        /// Main algorithm for assigning people to shelters based on distance and priorities
        /// The algorithm considers walking speed, time constraints, and may prioritize children and elderly
        /// </summary>
        /// <param name="people">List of people to assign</param>
        /// <param name="shelters">List of available shelters</param>
        /// <param name="prioritySettings">Settings for age-based priority (children, elderly)</param>
        private Dictionary<int, AssignmentDto> AssignPeopleToShelters(
            List<PersonDto> people,
            List<ShelterDto> shelters,
            PrioritySettingsDto prioritySettings)
        {
            Console.WriteLine("Starting revised shelter assignment algorithm with 50m segments...");

            // STEP 1: DEFINE CONSTANTS
            // Setting time and distance constraints based on real-world running speed
            // In an emergency, people typically have about 1 minute to reach shelter in Beer Sheva = around 600m if running
            const double MAX_TRAVEL_TIME_MINUTES = 1.0; // Maximum travel time in minutes
            const double WALKING_SPEED_KM_PER_MINUTE = 0.6; // ~5 km/h = 0.6 km/min
            const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE; // Should be about 0.6km

            // Dividing the maximum distance into small segments for processing
            // The segments can be increased, if we want to make the algorithm faster
            const int SEGMENT_SIZE_METERS = 5;
            const int DISTANCE_SEGMENTS = 120; // 600m divided into 5m segments = 120 segments
            const double SEGMENT_SIZE_KM = SEGMENT_SIZE_METERS / 1000.0;

            // Log the simulation parameters
            Console.WriteLine($"Time constraint: Maximum distance = {MAX_DISTANCE_KM:F4} km");
            Console.WriteLine($"Using {DISTANCE_SEGMENTS} segments of {SEGMENT_SIZE_METERS}m each");

            // STEP 2: CREATE DISTANCE MATRIX
            // Calculate the distance between each person and each shelter
            Console.WriteLine("Building distance matrix...");
            var distanceMatrix = new List<List<AssignmentOption>>();

            foreach (var person in people)
            {
                var personDistances = new List<AssignmentOption>();

                foreach (var shelter in shelters)
                {
                    // Calculate the straight-line distance between this person and shelter
                    double distance = CalculateDistance(
                        person.Latitude, person.Longitude,
                        shelter.Latitude, shelter.Longitude);

                    // Determine if this shelter is reachable within the time constraint
                    bool isReachable = distance <= MAX_DISTANCE_KM;

                    // Add this option to the person's list of potential shelters
                    personDistances.Add(new AssignmentOption
                    {
                        PersonId = person.Id,
                        ShelterId = shelter.Id,
                        Distance = distance,
                        IsReachable = isReachable,
                        // Calculate vulnerability score for priority assignment if enabled
                        VulnerabilityScore = prioritySettings?.EnableAgePriority == true
                            ? CalculateVulnerabilityScore(person.Age)
                            : 0
                    });
                }

                // Add this person's distance options to the overall matrix
                distanceMatrix.Add(personDistances);
            }

            // STEP 3: CALCULATE TOTAL CAPACITY
            // Determine if there's enough shelter capacity for everyone
            int totalCapacity = shelters.Sum(s => s.Capacity);
            int totalPeople = people.Count;

            // Log the total capacity and number of people
            Console.WriteLine($"Total shelter capacity: {totalCapacity}, Total people: {totalPeople}");

            // STEP 4: PREPARE WORKING DATA STRUCTURES
            // Dictionary to track remaining capacity of each shelter
            var shelterCapacity = shelters.ToDictionary(s => s.Id, s => s.Capacity);

            // Initialize assignment tracking
            var assignments = new Dictionary<int, AssignmentDto>();
            var assignedPeople = new HashSet<int>();

            // STEP 5: IDENTIFY ONE-SHELTER PEOPLE
            // These are people who can only reach one shelter - they need special handling
            var oneShelterPeople = new List<int>();
            var oneShelterMap = new Dictionary<int, int>(); // Maps person ID to their only shelter option

            for (int i = 0; i < people.Count; i++)
            {
                // Get all reachable shelters for this person
                var personOptions = distanceMatrix[i]
                    .Where(entry => entry.IsReachable)
                    .ToList();

                // If they only have one option, record them as a one-shelter person
                if (personOptions.Count == 1)
                {
                    int personId = people[i].Id;
                    int shelterId = personOptions[0].ShelterId;

                    oneShelterPeople.Add(personId);
                    oneShelterMap[personId] = shelterId;
                }
            }

            // Log the number of one-shelter people identified
            Console.WriteLine($"Identified {oneShelterPeople.Count} people with only one shelter option");

            // STEP 6: GROUP ASSIGNMENTS BY DISTANCE SEGMENT
            // Create a list of all possible assignments
            var allPossibleAssignments = distanceMatrix
                .SelectMany(x => x)
                .Where(entry => entry.IsReachable)
                .ToList();

            // Group assignments by distance segment for processing in order of proximity
            // This will help us process the closest people first
            var segmentAssignments = new List<List<AssignmentOption>>();
            for (int segment = 0; segment < DISTANCE_SEGMENTS; segment++)
            {
                // Calculate the minimum and maximum distance for this segment
                double minDistance = segment * SEGMENT_SIZE_KM;
                double maxDistance = (segment + 1) * SEGMENT_SIZE_KM;

                // Find all assignments in this distance segment
                var currentSegment = allPossibleAssignments
                    .Where(a => a.Distance >= minDistance && a.Distance < maxDistance)
                    .ToList();

                // Add this segment to our list of assignments
                segmentAssignments.Add(currentSegment);

                // Log the number of assignments in this segment
                Console.WriteLine($"Segment {segment + 1} ({minDistance * 1000:F0}m-{maxDistance * 1000:F0}m): {currentSegment.Count} possible assignments");
            }

            // STEP 7: PROCESS EACH DISTANCE SEGMENT IN ORDER
            // Process closest segments first (people closest to shelters get assigned first)
            for (int segment = 0; segment < DISTANCE_SEGMENTS; segment++)
            {
                // Calculate the minimum and maximum distance for this segment
                double minDistance = segment * SEGMENT_SIZE_KM;
                double maxDistance = (segment + 1) * SEGMENT_SIZE_KM;

                // Log the current segment being processed
                Console.WriteLine($"Processing segment {segment + 1}: {minDistance * 1000:F0}m to {maxDistance * 1000:F0}m");

                // Get the assignments for this segment
                var currentSegment = segmentAssignments[segment];
                if (currentSegment.Count == 0)
                    continue; // Skip empty segments

                // Count unique people and available shelter capacity in this segment
                var peopleInSegment = currentSegment
                    .Select(a => a.PersonId)
                    .Distinct()
                    .ToList();

                // Calculate available capacity in shelters relevant to this segment
                int relevantCapacity = 0;
                var sheltersInSegment = currentSegment
                    .Select(a => a.ShelterId)
                    .Distinct()
                    .ToList();

                // Sum the capacity of shelters in this segment
                foreach (var shelterId in sheltersInSegment)
                {
                    relevantCapacity += shelterCapacity[shelterId];
                }

                // Log the number of people and relevant capacity in this segment
                Console.WriteLine($"Segment has {peopleInSegment.Count} people and {relevantCapacity} relevant capacity");

                // Check if all people in this segment can fit in available shelters
                bool allCanFit = peopleInSegment.Count <= relevantCapacity;

                if (allCanFit)
                {
                    // STEP 7A: IF ALL CAN FIT, ASSIGN BY DISTANCE ONLY
                    // When everyone can fit, prioritize by shortest distance
                    Console.WriteLine($"All people in segment can fit - assigning without age priority");

                    // Find best shelter for each person in this segment
                    foreach (var personId in peopleInSegment)
                    {
                        // Skip if already assigned
                        if (assignedPeople.Contains(personId))
                            continue;

                        // Get person index in the list
                        int personIndex = people.FindIndex(p => p.Id == personId);

                        // Find closest shelter with capacity for this person in this segment
                        var options = distanceMatrix[personIndex]
                            .Where(entry => entry.IsReachable &&
                                   entry.Distance < maxDistance &&
                                   entry.Distance >= minDistance &&
                                   shelterCapacity[entry.ShelterId] > 0)
                            .OrderBy(entry => entry.Distance) // Sort by closest first
                            .ToList();

                        // Log the number of options available for this person
                        if (options.Count > 0)
                        {
                            var bestOption = options[0]; // Get closest shelter

                            // Make assignment - checking if person is already assigned
                            if (!assignments.ContainsKey(personId))
                            {
                                assignments[personId] = new AssignmentDto
                                {
                                    PersonId = personId,
                                    ShelterId = bestOption.ShelterId,
                                    Distance = bestOption.Distance
                                };

                                // Update tracking - reduce shelter capacity and mark person as assigned
                                shelterCapacity[bestOption.ShelterId]--;
                                assignedPeople.Add(personId);

                                // Log the assignment    
                                Console.WriteLine($"Assigned person {personId} to shelter {bestOption.ShelterId} (distance: {bestOption.Distance * 1000:F0}m)");
                            }
                        }
                    }
                }
                else // if not all can fit
                {
                    // STEP 7B: IF NOT ALL CAN FIT, USE AGE PRIORITY
                    // When capacity is limited, prioritize by vulnerability (age) then distance
                    Console.WriteLine($"Not all people can fit in segment - using age priority");

                    // Sort assignments by vulnerability score (elderly and children first), then by distance
                    var prioritizedSegmentAssignments = currentSegment
                        .OrderByDescending(a => prioritySettings?.EnableAgePriority == true ? a.VulnerabilityScore : 0)
                        .ThenBy(a => a.Distance)
                        .ToList();

                    // Process assignments in priority order
                    foreach (var assignment in prioritizedSegmentAssignments)
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

                        // Update tracking - reduce shelter capacity and mark person as assigned
                        shelterCapacity[assignment.ShelterId]--;
                        assignedPeople.Add(assignment.PersonId);

                        // Log the assignment
                        Console.WriteLine($"Assigned person {assignment.PersonId} to shelter {assignment.ShelterId} (priority) (distance: {assignment.Distance * 1000:F0}m)");
                    }
                }
            }

            // STEP 8: HANDLE ONE-SHELTER-PEOPLE WHO HAVEN'T BEEN ASSIGNED YET
            // These are special cases: people who can only reach one shelter but haven't been assigned yet
            var unassignedOneShelterPeople = oneShelterPeople
                .Where(id => !assignedPeople.Contains(id))
                .ToList();

            // Log the number of unassigned one-shelter people
            if (unassignedOneShelterPeople.Count > 0)
            {
                // Log the number of unassigned one-shelter people
                Console.WriteLine($"Processing {unassignedOneShelterPeople.Count} unassigned one-shelter-people");

                // Try to assign them to their only shelter option
                foreach (var personId in unassignedOneShelterPeople)
                {
                    int shelterId = oneShelterMap[personId];

                    // STEP 8A: IF SHELTER STILL HAS CAPACITY, ASSIGN DIRECTLY
                    if (shelterCapacity[shelterId] > 0)
                    {
                        // Find distance for this assignment
                        int personIndex = people.FindIndex(p => p.Id == personId);
                        var option = distanceMatrix[personIndex]
                            .First(o => o.ShelterId == shelterId);

                        // Make assignment
                        assignments[personId] = new AssignmentDto
                        {
                            PersonId = personId,
                            ShelterId = shelterId,
                            Distance = option.Distance
                        };

                        // Update tracking
                        shelterCapacity[shelterId]--;
                        assignedPeople.Add(personId);

                        // Log the assignment
                        Console.WriteLine($"Assigned one-shelter person {personId} to shelter {shelterId} (distance: {option.Distance * 1000:F0}m)");
                    }
                    else
                    {
                        // STEP 8B: IF SHELTER IS FULL, TRY TO REASSIGN SOMEONE ELSE
                        // Get all people assigned to this shelter
                        var peopleInShelter = assignments
                            .Where(a => a.Value.ShelterId == shelterId)
                            .Select(a => a.Key)
                            .ToList();

                        // record if we made a reassignment
                        bool reassignmentMade = false;

                        // Try to find someone in the shelter who could go elsewhere
                        foreach (var candidateId in peopleInShelter)
                        {
                            // Skip one-shelter people - they can't be moved
                            if (oneShelterPeople.Contains(candidateId))
                                continue;

                            // Find alternative shelters for this candidate
                            int candidateIndex = people.FindIndex(p => p.Id == candidateId);
                            var alternatives = distanceMatrix[candidateIndex]
                                .Where(opt => opt.IsReachable &&
                                       opt.ShelterId != shelterId &&
                                       shelterCapacity[opt.ShelterId] > 0)
                                .OrderBy(opt => opt.Distance)
                                .ToList();

                            if (alternatives.Count > 0)
                            {
                                // Found someone who can be reassigned
                                var bestAlternative = alternatives[0];

                                // Record current assignment for logging
                                var currentAssignment = assignments[candidateId];

                                // Update assignment - move this person to an alternative shelter
                                assignments[candidateId] = new AssignmentDto
                                {
                                    PersonId = candidateId,
                                    ShelterId = bestAlternative.ShelterId,
                                    Distance = bestAlternative.Distance
                                };

                                // Update shelter capacities
                                shelterCapacity[shelterId]++;
                                shelterCapacity[bestAlternative.ShelterId]--;

                                // Update tracking
                                Console.WriteLine($"Reassigned person {candidateId} from shelter {shelterId} to {bestAlternative.ShelterId} to make room");

                                // Now assign the one-shelter person to the shelter they need
                                int personIndex = people.FindIndex(p => p.Id == personId);
                                var option = distanceMatrix[personIndex]
                                    .First(o => o.ShelterId == shelterId);

                                // Make assignment
                                assignments[personId] = new AssignmentDto
                                {
                                    PersonId = personId,
                                    ShelterId = shelterId,
                                    Distance = option.Distance
                                };

                                // Update tracking
                                shelterCapacity[shelterId]--;
                                assignedPeople.Add(personId);

                                // Log the assignment
                                Console.WriteLine($"Assigned one-shelter person {personId} to shelter {shelterId} after reassignment (distance: {option.Distance * 1000:F0}m)");

                                reassignmentMade = true;
                                break;
                            }
                        }

                        // log if no reassignment was possible
                        // if (!reassignmentMade)
                        // {
                        //     Console.WriteLine($"Could not assign one-shelter person {personId} - no reassignment options available");
                        // }
                    }
                }
            }

            // STEP 9: FINAL CHECK FOR UNASSIGNED PEOPLE
            // Try to assign any remaining people who could still find a shelter
            var remainingPeople = people
                .Where(p => !assignedPeople.Contains(p.Id))
                .ToList();

            if (remainingPeople.Count > 0)
            {
                Console.WriteLine($"Final pass - attempting to assign {remainingPeople.Count} remaining people");

                foreach (var person in remainingPeople)
                {
                    // Find any shelter with remaining capacity within range
                    int personIndex = people.IndexOf(person);
                    var options = distanceMatrix[personIndex]
                        .Where(entry => entry.IsReachable && shelterCapacity[entry.ShelterId] > 0)
                        .OrderBy(entry => entry.Distance)
                        .ToList();

                    // If we have options, assign the closest one
                    if (options.Count > 0)
                    {
                        // Get the best option (closest shelter)
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

                        // Log the assignment
                        Console.WriteLine($"Final pass: Assigned person {person.Id} to shelter {bestOption.ShelterId} (distance: {bestOption.Distance * 1000:F0}m)");
                    }
                }
            }

            // STEP 10: VERIFY NO DUPLICATE ASSIGNMENTS
            // Check for any errors in our assignment process
            var personIds = assignments.Keys.ToList();
            var duplicateCheck = personIds.GroupBy(id => id).Where(g => g.Count() > 1).ToList();
            if (duplicateCheck.Any())
            {
                // Log any duplicate assignments found
                Console.WriteLine($"WARNING: Found {duplicateCheck.Count} duplicate person assignments!");
                foreach (var group in duplicateCheck)
                {
                    // Log the person ID and number of assignments
                    Console.WriteLine($"Person {group.Key} is assigned multiple times");
                }
            }

            // STEP 11: OPTIMIZATION PHASE
            // Try to improve assignments by swapping people between shelters
            Console.WriteLine($"Initial assignments: {assignments.Count} people assigned, {people.Count - assignments.Count} unassigned");
            Console.WriteLine("Starting optimization phase to improve assignments...");

            // Only optimize if we have one assigned person or more
            if (assignments.Count > 0)
            {
                assignments = OptimizeAssignments(assignments, people, shelters);
            }

            Console.WriteLine($"Final assignments: {assignments.Count} people assigned, {people.Count - assignments.Count} unassigned");
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

            // STEP 4: RUN OPTIMIZATION ITERATIONS
            // Track if any improvements were made
            bool improvementFound;
            int swapCount = 0;
            double totalDistanceImprovement = 0;

            // Log the start of the optimization process
            Console.WriteLine("Starting swap optimization iterations...");

            // Repeat until no more improvements can be found
            do
            {
                improvementFound = false;

                // STEP 4A: ITERATE THROUGH ALL POSSIBLE SHELTER PAIRS
                for (int i = 0; i < shelters.Count; i++)
                {
                    int shelter1Id = shelters[i].Id;

                    // Skip if this shelter has no assigned people
                    if (shelterAssignments[shelter1Id].Count == 0)
                        continue;

                    for (int j = i + 1; j < shelters.Count; j++)
                    {
                        int shelter2Id = shelters[j].Id;

                        // Skip if this shelter has no assigned people
                        if (shelterAssignments[shelter2Id].Count == 0)
                            continue;

                        // STEP 4B: TRY SWAPPING PEOPLE BETWEEN SHELTERS
                        foreach (int person1Id in shelterAssignments[shelter1Id])
                        {
                            var person1 = personLookup[person1Id];
                            double person1CurrentDistance = optimizedAssignments[person1Id].Distance;

                            foreach (int person2Id in shelterAssignments[shelter2Id])
                            {
                                var person2 = personLookup[person2Id];
                                double person2CurrentDistance = optimizedAssignments[person2Id].Distance;

                                // STEP 4C: CALCULATE POTENTIAL SWAP DISTANCES
                                // Calculate what the distances would be if we swap these two people
                                double person1NewDistance = CalculateDistance(
                                    person1.Latitude, person1.Longitude,
                                    shelterLookup[shelter2Id].Latitude, shelterLookup[shelter2Id].Longitude);

                                double person2NewDistance = CalculateDistance(
                                    person2.Latitude, person2.Longitude,
                                    shelterLookup[shelter1Id].Latitude, shelterLookup[shelter1Id].Longitude);

                                // Calculate total current distance and potential new distance
                                double currentTotalDistance = person1CurrentDistance + person2CurrentDistance;
                                double newTotalDistance = person1NewDistance + person2NewDistance;

                                // STEP 4D: IF SWAP REDUCES TOTAL DISTANCE, DO IT
                                if (newTotalDistance < currentTotalDistance)
                                {
                                    // Calculate the improvement
                                    double improvement = currentTotalDistance - newTotalDistance;
                                    totalDistanceImprovement += improvement;

                                    // Update assignments for both people
                                    optimizedAssignments[person1Id] = new AssignmentDto
                                    {
                                        PersonId = person1Id,
                                        ShelterId = shelter2Id,
                                        Distance = person1NewDistance
                                    };

                                    optimizedAssignments[person2Id] = new AssignmentDto
                                    {
                                        PersonId = person2Id,
                                        ShelterId = shelter1Id,
                                        Distance = person2NewDistance
                                    };

                                    // Update our shelter assignment tracking
                                    shelterAssignments[shelter1Id].Remove(person1Id);
                                    shelterAssignments[shelter1Id].Add(person2Id);
                                    shelterAssignments[shelter2Id].Remove(person2Id);
                                    shelterAssignments[shelter2Id].Add(person1Id);

                                    swapCount++;
                                    improvementFound = true;

                                    Console.WriteLine($"Swap {swapCount}: Persons {person1Id} and {person2Id} between shelters {shelter1Id} and {shelter2Id}, saving {improvement:F4} km");

                                    // Break out of inner loop once we find an improvement
                                    break;
                                }
                            }

                            // Break out of outer person loop if we found an improvement
                            if (improvementFound)
                                break;
                        }

                        // Break out of shelter loop if we found an improvement
                        if (improvementFound)
                            break;
                    }

                    // Break out of outer shelter loop if we found an improvement
                    if (improvementFound)
                        break;
                }

            } while (improvementFound); // Repeat until no more improvements can be made

            // Log the results of the optimization
            Console.WriteLine($"Optimization complete: Made {swapCount} swaps, reducing total distance by {totalDistanceImprovement:F4} km");

            return optimizedAssignments;
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
     * PersonDto - Represents a person in the simulation
     * Includes their location and age
     */
    public class PersonDto
    {
        public int Id { get; set; } // Unique identifier for this person
        public int Age { get; set; } // Age of the person (affects priority)
        public double Latitude { get; set; } // Latitude coordinate
        public double Longitude { get; set; } // Longitude coordinate
        public int? NearestShelterId { get; set; } // ID of the nearest shelter (even if not assigned)
        public double? NearestShelterDistance { get; set; } // Distance to the nearest shelter
    }

    /**
     * ShelterDto - Represents a shelter in the simulation
     * Includes location and capacity information
     */
    public class ShelterDto
    {
        public int Id { get; set; } // Unique identifier for this shelter
        public string Name { get; set; } // Name or description of the shelter
        public double Latitude { get; set; } // Latitude coordinate
        public double Longitude { get; set; } // Longitude coordinate
        public int Capacity { get; set; } // How many people this shelter can hold
    }

    /**
     * AssignmentDto - Represents the assignment of a person to a shelter
     * Includes the distance between them
     */
    public class AssignmentDto
    {
        public int PersonId { get; set; } // ID of the assigned person
        public int ShelterId { get; set; } // ID of the shelter they're assigned to
        public double Distance { get; set; } // Distance between the person and shelter in kilometers
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


}