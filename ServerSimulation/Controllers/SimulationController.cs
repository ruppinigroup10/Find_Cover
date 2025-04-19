using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using ServerSimulation.Models;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Builder;

namespace FindCover.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimulationController : ControllerBase
    {
        private static readonly Random _random = new Random();

        //===================================
        // Simulation start here
        //===================================

        /// POST api/simulation/run
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
                    // Generate shelters, treating request.ShelterCount as ADDITIONAL shelters on top of DB ones
                    shelters = GenerateShelters(
                        request.ShelterCount, // Now this is the count of ADDITIONAL shelters
                        centerLat,
                        centerLon,
                        request.RadiusKm,
                        request.ZeroCapacityShelters,
                        request.UseDatabaseShelters);
                }

                //var people = GeneratePeople(request.PeopleCount, centerLat, centerLon, request.RadiusKm);
                var assignments = AssignPeopleToShelters(people, shelters, request.PrioritySettings);

                // Add nearest shelter info for unassigned people
                var unassignedPeople = people.Where(p => !assignments.ContainsKey(p.Id)).ToList();
                foreach (var person in unassignedPeople)
                {
                    // Find nearest shelter
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

                    // Add nearest shelter info to the person object
                    if (nearestShelter != null)
                    {
                        person.NearestShelterId = nearestShelter.Id;
                        person.NearestShelterDistance = nearestDistance;
                    }
                }

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
                        ShelterUsagePercentage = shelters.Sum(s => s.Capacity) > 0
                        ? (double)assignedCount / shelters.Sum(s => s.Capacity) * 100
                        : 0,
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

        //===================================
        // Helper method to generate people
        //===================================

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
                // Based on 2019 Israel demographics by age
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

        //===================================
        // Helper method to generate shelters
        //===================================
        private List<ShelterDto> GenerateShelters(int additionalCount, double centerLat, double centerLon, double radiusKm, bool zeroCapacityShelters = false, bool useDatabaseShelters = true)
        {
            var shelters = new List<ShelterDto>();
            bool usingDatabaseShelters = false;

            if (useDatabaseShelters)
            {
                try
                {
                    // Get shelters from the database
                    DBservices db = new DBservices();
                    List<Shelter> dbShelters = db.GetAllShelters();

                    // Check if we actually got shelters
                    if (dbShelters != null && dbShelters.Count > 0)
                    {
                        usingDatabaseShelters = true;
                        Console.WriteLine($"✅ Successfully retrieved {dbShelters.Count} shelters from database");

                        // Add database shelters to the result list
                        foreach (var dbShelter in dbShelters)
                        {
                            shelters.Add(ConvertToShelterDto(dbShelter));
                            Console.WriteLine($"Added shelter: {dbShelter.name} (ID: {dbShelter.shelter_id})");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No shelters found in database");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                    Console.Error.WriteLine($"⚠️ DATABASE ERROR: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Database shelters excluded by user request");
            }

            // Now generate additional random shelters
            // If no database shelters were loaded, we'll start with ID 1
            int baseId = shelters.Count > 0 ? shelters.Max(s => s.Id) + 1 : 1;

            // If no database shelters were used, add the requested number of shelters,
            // otherwise add the additionalCount
            int sheltersToAdd = usingDatabaseShelters ? additionalCount : additionalCount;

            Console.WriteLine($"Generating {sheltersToAdd} {(usingDatabaseShelters ? "additional" : "")} shelters starting with ID {baseId}");

            for (int i = 0; i < sheltersToAdd; i++)
            {
                // Generate random location
                double angle = _random.NextDouble() * 2 * Math.PI;
                double distance = _random.NextDouble() * radiusKm * 0.7 / 111.0;

                double latOffset = distance * Math.Cos(angle);
                double lonOffset = distance * Math.Sin(angle);

                // Determine capacity
                int capacity;
                if (zeroCapacityShelters && _random.NextDouble() < 0.4)
                {
                    capacity = 0; // Zero capacity
                }
                else
                {
                    capacity = _random.Next(3, 8); // Normal capacity between 3 and 7
                }

                shelters.Add(new ShelterDto
                {
                    Id = baseId + i,
                    Name = usingDatabaseShelters
                        ? (capacity == 0 ? $"Additional Closed Shelter {i + 1}" : $"Additional Shelter {i + 1}")
                        : (capacity == 0 ? $"Closed Shelter {i + 1}" : $"Shelter {i + 1}"),
                    Latitude = centerLat + latOffset,
                    Longitude = centerLon + lonOffset,
                    Capacity = capacity
                });
            }

            return shelters;
        }

        //===================================
        // Assign People To Shelters
        //===================================

        // Main algorithm for assigning people to shelters with time constraints
        // Using greedy approach with global optimization approach in the end
        // Modified to prioritize by distance first, then age only when necessary, and protect people with only one shelter option
        private Dictionary<int, AssignmentDto> AssignPeopleToShelters(
            List<PersonDto> people,
            List<ShelterDto> shelters,
            PrioritySettingsDto prioritySettings)
        {
            Console.WriteLine("Starting revised shelter assignment algorithm with 50m segments...");

            // Constants defining time and distance constraints
            const double MAX_TRAVEL_TIME_MINUTES = 1.0; // Maximum travel time in minutes
            const double WALKING_SPEED_KM_PER_MINUTE = 0.6; // ~5 km/h = 0.6 km/min
            const double MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE; // Should be about 0.6km
            //const int SEGMENT_SIZE_METERS = 50;
            //const int DISTANCE_SEGMENTS = 12; // 600m divided into 50m segments = 12 segments
            const int SEGMENT_SIZE_METERS = 5;
            const int DISTANCE_SEGMENTS = 120; // 600m divided into 5m segments = 120 segments
            const double SEGMENT_SIZE_KM = SEGMENT_SIZE_METERS / 1000.0;

            Console.WriteLine($"Time constraint: Maximum distance = {MAX_DISTANCE_KM:F4} km");
            Console.WriteLine($"Using {DISTANCE_SEGMENTS} segments of {SEGMENT_SIZE_METERS}m each");

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

            // Step 4: Identify people with only one shelter option (one-shelter-people)
            var oneShelterPeople = new List<int>();
            var oneShelterMap = new Dictionary<int, int>(); // Maps person ID to their only shelter option

            for (int i = 0; i < people.Count; i++)
            {
                var personOptions = distanceMatrix[i]
                    .Where(entry => entry.IsReachable)
                    .ToList();

                if (personOptions.Count == 1)
                {
                    int personId = people[i].Id;
                    int shelterId = personOptions[0].ShelterId;

                    oneShelterPeople.Add(personId);
                    oneShelterMap[personId] = shelterId;
                }
            }

            Console.WriteLine($"Identified {oneShelterPeople.Count} people with only one shelter option");

            // Step 5: Create all possible assignments and group by distance segment
            // Create a list of all possible assignments
            var allPossibleAssignments = distanceMatrix
                .SelectMany(x => x)
                .Where(entry => entry.IsReachable)
                .ToList();

            // Group assignments by distance segment (50m increments)
            var segmentAssignments = new List<List<AssignmentOption>>();
            for (int segment = 0; segment < DISTANCE_SEGMENTS; segment++)
            {
                double minDistance = segment * SEGMENT_SIZE_KM;
                double maxDistance = (segment + 1) * SEGMENT_SIZE_KM;

                var currentSegment = allPossibleAssignments
                    .Where(a => a.Distance >= minDistance && a.Distance < maxDistance)
                    .ToList();

                segmentAssignments.Add(currentSegment);

                Console.WriteLine($"Segment {segment + 1} ({minDistance * 1000:F0}m-{maxDistance * 1000:F0}m): {currentSegment.Count} possible assignments");
            }

            // Step 6: Process each distance segment in order (closest first)
            for (int segment = 0; segment < DISTANCE_SEGMENTS; segment++)
            {
                double minDistance = segment * SEGMENT_SIZE_KM;
                double maxDistance = (segment + 1) * SEGMENT_SIZE_KM;

                Console.WriteLine($"Processing segment {segment + 1}: {minDistance * 1000:F0}m to {maxDistance * 1000:F0}m");

                var currentSegment = segmentAssignments[segment];
                if (currentSegment.Count == 0)
                    continue;

                // Step 6.1: Count unique people in this segment
                var peopleInSegment = currentSegment
                    .Select(a => a.PersonId)
                    .Distinct()
                    .ToList();

                // Step 6.2: Count available shelter capacity relevant to this segment
                int relevantCapacity = 0;
                var sheltersInSegment = currentSegment
                    .Select(a => a.ShelterId)
                    .Distinct()
                    .ToList();

                foreach (var shelterId in sheltersInSegment)
                {
                    relevantCapacity += shelterCapacity[shelterId];
                }

                Console.WriteLine($"Segment has {peopleInSegment.Count} people and {relevantCapacity} relevant capacity");

                // Step 6.3: Check if all people in segment can be assigned
                bool allCanFit = peopleInSegment.Count <= relevantCapacity;

                if (allCanFit)
                {
                    // If all can fit, assign everyone in this segment without age priorities
                    Console.WriteLine($"All people in segment can fit - assigning without age priority");

                    // Find best shelter for each person
                    foreach (var personId in peopleInSegment)
                    {
                        // Skip if already assigned
                        if (assignedPeople.Contains(personId))
                            continue;

                        // Get person index
                        int personIndex = people.FindIndex(p => p.Id == personId);

                        // Find closest shelter with capacity for this person
                        var options = distanceMatrix[personIndex]
                            .Where(entry => entry.IsReachable &&
                                   entry.Distance < maxDistance &&
                                   entry.Distance >= minDistance &&
                                   shelterCapacity[entry.ShelterId] > 0)
                            .OrderBy(entry => entry.Distance)
                            .ToList();

                        if (options.Count > 0)
                        {
                            var bestOption = options[0];

                            // Make assignment - IMPORTANT FIX: Check if person is already assigned
                            if (!assignments.ContainsKey(personId))
                            {
                                assignments[personId] = new AssignmentDto
                                {
                                    PersonId = personId,
                                    ShelterId = bestOption.ShelterId,
                                    Distance = bestOption.Distance
                                };

                                // Update tracking
                                shelterCapacity[bestOption.ShelterId]--;
                                assignedPeople.Add(personId);

                                Console.WriteLine($"Assigned person {personId} to shelter {bestOption.ShelterId} (distance: {bestOption.Distance * 1000:F0}m)");
                            }
                        }
                    }
                }
                else
                {
                    // Not all can fit - prioritize by age within segment
                    Console.WriteLine($"Not all people can fit in segment - using age priority");

                    // Get all possible assignments in this segment and sort by age priority
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

                        // Update tracking
                        shelterCapacity[assignment.ShelterId]--;
                        assignedPeople.Add(assignment.PersonId);

                        Console.WriteLine($"Assigned person {assignment.PersonId} to shelter {assignment.ShelterId} (priority) (distance: {assignment.Distance * 1000:F0}m)");
                    }
                }
            }

            // Step 7: Handle one-shelter-people who haven't been assigned yet
            var unassignedOneShelterPeople = oneShelterPeople
                .Where(id => !assignedPeople.Contains(id))
                .ToList();

            if (unassignedOneShelterPeople.Count > 0)
            {
                Console.WriteLine($"Processing {unassignedOneShelterPeople.Count} unassigned one-shelter-people");

                foreach (var personId in unassignedOneShelterPeople)
                {
                    int shelterId = oneShelterMap[personId];

                    // If shelter still has capacity, assign directly
                    if (shelterCapacity[shelterId] > 0)
                    {
                        // Find distance
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

                        Console.WriteLine($"Assigned one-shelter person {personId} to shelter {shelterId} (distance: {option.Distance * 1000:F0}m)");
                    }
                    else
                    {
                        // Shelter is full - check if we can make room by reassigning someone
                        // Get all people assigned to this shelter
                        var peopleInShelter = assignments
                            .Where(a => a.Value.ShelterId == shelterId)
                            .Select(a => a.Key)
                            .ToList();

                        bool reassignmentMade = false;

                        // Find someone with an alternative
                        foreach (var candidateId in peopleInShelter)
                        {
                            // Skip one-shelter people
                            if (oneShelterPeople.Contains(candidateId))
                                continue;

                            // Get candidate's alternatives
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

                                // Update assignment
                                assignments[candidateId] = new AssignmentDto
                                {
                                    PersonId = candidateId,
                                    ShelterId = bestAlternative.ShelterId,
                                    Distance = bestAlternative.Distance
                                };

                                // Update shelter capacities
                                shelterCapacity[shelterId]++;
                                shelterCapacity[bestAlternative.ShelterId]--;

                                Console.WriteLine($"Reassigned person {candidateId} from shelter {shelterId} to {bestAlternative.ShelterId} to make room");

                                // Now assign the one-shelter person
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

                                Console.WriteLine($"Assigned one-shelter person {personId} to shelter {shelterId} after reassignment (distance: {option.Distance * 1000:F0}m)");

                                reassignmentMade = true;
                                break;
                            }
                        }

                        // check log for those who could not be reassigned
                        // if (!reassignmentMade)
                        // {
                        //     Console.WriteLine($"Could not assign one-shelter person {personId} - no reassignment options available");
                        // }
                    }
                }
            }

            // Step 8: ensure no available assignments were missed
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
            }

            // Verify we don't have any duplicate assignments
            var personIds = assignments.Keys.ToList();
            var duplicateCheck = personIds.GroupBy(id => id).Where(g => g.Count() > 1).ToList();
            if (duplicateCheck.Any())
            {
                Console.WriteLine($"WARNING: Found {duplicateCheck.Count} duplicate person assignments!");
                foreach (var group in duplicateCheck)
                {
                    Console.WriteLine($"Person {group.Key} is assigned multiple times");
                }
            }

            // After all assignments are done, run the optimization phase
            Console.WriteLine($"Initial assignments: {assignments.Count} people assigned, {people.Count - assignments.Count} unassigned");
            Console.WriteLine("Starting optimization phase to improve assignments...");

            // Only optimize if we have more than one assigned person
            if (assignments.Count > 1)
            {
                assignments = OptimizeAssignments(assignments, people, shelters);
            }

            Console.WriteLine($"Final assignments: {assignments.Count} people assigned, {people.Count - assignments.Count} unassigned");
            return assignments;
        }

        //===================================
        // Optimize Assignments
        //===================================

        // Optimize AssignPeopleToShelters after assignment phase
        // This method will swap people between shelters to minimize total distance
        private Dictionary<int, AssignmentDto> OptimizeAssignments(
            Dictionary<int, AssignmentDto> initialAssignments,
            List<PersonDto> people,
            List<ShelterDto> shelters)
        {
            Console.WriteLine("Starting post-assignment optimization phase...");

            // Create a copy of the assignments to work with
            var optimizedAssignments = new Dictionary<int, AssignmentDto>(initialAssignments);

            // Create lookup dictionaries for faster access
            var personLookup = people.ToDictionary(p => p.Id);
            var shelterLookup = shelters.ToDictionary(s => s.Id);

            // Track people assigned to each shelter for easier iteration
            var shelterAssignments = new Dictionary<int, List<int>>();
            foreach (var shelter in shelters)
            {
                shelterAssignments[shelter.Id] = new List<int>();
            }

            foreach (var assignment in optimizedAssignments)
            {
                int personId = assignment.Key;
                int shelterId = assignment.Value.ShelterId;
                shelterAssignments[shelterId].Add(personId);
            }

            // Track if any improvements were made
            bool improvementFound;
            int swapCount = 0;
            double totalDistanceImprovement = 0;

            Console.WriteLine("Starting swap optimization iterations...");
            // Repeat until no more improvements can be found
            do
            {
                improvementFound = false;

                // Iterate through all possible shelter pairs
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

                        // Try swapping people between these two shelters
                        foreach (int person1Id in shelterAssignments[shelter1Id])
                        {
                            var person1 = personLookup[person1Id];
                            double person1CurrentDistance = optimizedAssignments[person1Id].Distance;

                            foreach (int person2Id in shelterAssignments[shelter2Id])
                            {
                                var person2 = personLookup[person2Id];
                                double person2CurrentDistance = optimizedAssignments[person2Id].Distance;

                                // Calculate what the distances would be if we swap
                                double person1NewDistance = CalculateDistance(
                                    person1.Latitude, person1.Longitude,
                                    shelterLookup[shelter2Id].Latitude, shelterLookup[shelter2Id].Longitude);

                                double person2NewDistance = CalculateDistance(
                                    person2.Latitude, person2.Longitude,
                                    shelterLookup[shelter1Id].Latitude, shelterLookup[shelter1Id].Longitude);

                                // Calculate total current distance and potential new distance
                                double currentTotalDistance = person1CurrentDistance + person2CurrentDistance;
                                double newTotalDistance = person1NewDistance + person2NewDistance;

                                // If swapping would reduce total distance, do it
                                if (newTotalDistance < currentTotalDistance)
                                {
                                    // Calculate the improvement
                                    double improvement = currentTotalDistance - newTotalDistance;
                                    totalDistanceImprovement += improvement;

                                    // Update assignments
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

            } while (improvementFound);

            Console.WriteLine($"Optimization complete: Made {swapCount} swaps, reducing total distance by {totalDistanceImprovement:F4} km");

            return optimizedAssignments;
        }


        //===================================
        // Extras and Helpers
        //===================================

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
            // else if (age >= 60)
            // {
            //     // Older adults (60-69): medium-high priority
            //     return 6;
            // }
            // else if (age <= 18)
            // {
            //     // Teenagers (13-18): medium priority
            //     return 4;
            // }
            else
            {
                // Adults (13-59): lowest priority
                return 6;
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
        public bool UseDatabaseShelters { get; set; } = true; // Default to true
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
        public int? NearestShelterId { get; set; }
        public double? NearestShelterDistance { get; set; }
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
        public double ShelterUsagePercentage { get; set; }

    }


}
