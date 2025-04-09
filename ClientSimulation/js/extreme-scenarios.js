/**
 * Extreme scenario handler functions for shelter simulation
 * These functions add testing capabilities for edge cases
 */

// Global variable to store family groups
let familyGroups = [];

/**
 * Adds an "Extreme Scenarios" panel to the simulation controls
 * This function should be called after the map is initialized
 */
function addExtremeScenarioControls() {
  // Check if extreme scenarios are already added
  if (document.querySelector(".scenario-heading")) {
    console.log("Extreme scenarios already added, skipping");
    return;
  }
  // Find the control container
  const controlContainer = document.querySelector(".control-container");
  if (!controlContainer) {
    console.error("Control container not found");
    return;
  }

  // Create a divider
  const divider = document.createElement("hr");
  divider.className = "control-divider";
  controlContainer.appendChild(divider);

  // Create a heading for the extreme scenarios
  const heading = document.createElement("h4");
  heading.textContent = "Extreme Scenarios";
  heading.className = "scenario-heading";
  controlContainer.appendChild(heading);

  // Create buttons for each extreme scenario
  const scenarioButtons = [
    {
      id: "scenario-zero-capacity",
      text: "Zero Capacity Shelters",
      handler: zeroCapacitySheltersScenario,
      color: "#e57373",
      hoverColor: "#ef5350",
    },
    {
      id: "scenario-overcrowd",
      text: "Overcrowd (500 People)",
      handler: runOvercrowdScenario,
    },
    {
      id: "scenario-families",
      text: "Add 5 Families",
      handler: addFamiliesScenario,
    },
    {
      id: "scenario-elderly",
      text: "Elderly Crisis (50% Elderly)",
      handler: elderlyScenario,
    },
    {
      id: "scenario-reset",
      text: "Reset to Default",
      handler: resetToDefault,
    },
  ];

  // Add the buttons to the control panel
  for (const button of scenarioButtons) {
    const buttonElement = document.createElement("button");
    buttonElement.id = button.id;
    buttonElement.textContent = button.text;
    buttonElement.className = "scenario-button control-button";

    // Add styling for colored buttons if provided
    if (button.color) {
      buttonElement.style.backgroundColor = button.color;
      buttonElement.style.borderColor = button.hoverColor || button.color;
    }

    // Add hover effect using event listeners if hover color provided
    if (button.hoverColor) {
      buttonElement.addEventListener("mouseenter", function () {
        this.style.backgroundColor = button.hoverColor;
      });

      buttonElement.addEventListener("mouseleave", function () {
        this.style.backgroundColor = button.color;
      });
    }

    buttonElement.addEventListener("click", button.handler);
    controlContainer.appendChild(buttonElement);
  }

  // Add family information display
  const familyInfoContainer = document.createElement("div");
  familyInfoContainer.id = "family-info-container";
  familyInfoContainer.className = "info-container";
  familyInfoContainer.innerHTML =
    '<h4>Family Groups</h4><div id="family-groups-list">No family groups created yet</div>';
  controlContainer.appendChild(familyInfoContainer);

  console.log("Extreme scenario controls added");
}

/**
 * Runs the overcrowding scenario - sets people count to 500
 * This tests how the system handles a large number of people
 */
function runOvercrowdScenario() {
  updateStatusMessage("Setting up overcrowd scenario...");

  // Set people count to 500
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "500";
  }

  // Keep shelter count relatively low to ensure overcrowding
  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "10";
  }

  // Run the simulation
  runServerSimulation();
}

/**
 * Adds family groups to the simulation
 * Creates 5 families with members who should stay together
 */
function addFamiliesScenario() {
  updateStatusMessage("Adding family groups...");

  // Clear previous family groups
  familyGroups = [];

  // Generate 5 families with 3-5 members each
  for (let i = 0; i < 5; i++) {
    const family = {
      id: i + 1,
      members: [],
      color: getRandomColor(),
    };

    // Family size: 3-5 people
    const familySize = Math.floor(Math.random() * 3) + 3;

    // Create family members with diverse ages
    for (let j = 0; j < familySize; j++) {
      let age;

      // First member is an adult (parent)
      if (j === 0) {
        age = Math.floor(Math.random() * 30) + 30; // 30-59 years
      }
      // Second member is also an adult (parent) or elderly (grandparent)
      else if (j === 1) {
        const isElderly = Math.random() > 0.7;
        age = isElderly
          ? Math.floor(Math.random() * 20) + 70 // 70-89 years (elderly)
          : Math.floor(Math.random() * 30) + 30; // 30-59 years (adult)
      }
      // Rest are children
      else {
        age = Math.floor(Math.random() * 17) + 1; // 1-17 years
      }

      family.members.push({
        id: 0, // Will be assigned by server
        age: age,
      });
    }

    familyGroups.push(family);
  }

  // Update the UI with family information
  updateFamilyInfo();

  // Set a reasonable number of regular people
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    // Family members are added in addition to regular people
    peopleCountInput.value = "100";
  }

  // Run the simulation with family groups
  runServerSimulationWithFamilies();
}

/**
 * Creates a scenario with a high percentage of elderly people
 * Tests how priority-based assignment handles vulnerable groups
 */
function elderlyScenario() {
  updateStatusMessage("Setting up elderly crisis scenario...");

  // Enable priority assignment to see the effect
  const prioritySelect = document.getElementById("priority");
  if (prioritySelect) {
    prioritySelect.value = "true";
  }

  // Set moderate population for clarity
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "150";
  }

  // Set shelter count
  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "10";
  }

  // We'll override the default age distribution when calling the API
  runServerSimulationWithElderlyFocus();
}

/**
 * Resets the simulation to default values
 */
function resetToDefault() {
  updateStatusMessage("Resetting to default values...");

  // Reset all inputs to default values
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "20";
  }

  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "10";
  }

  const radiusInput = document.getElementById("radius");
  if (radiusInput) {
    radiusInput.value = "0.5";
  }

  const prioritySelect = document.getElementById("priority");
  if (prioritySelect) {
    prioritySelect.value = "true";
  }

  // Clear family groups
  familyGroups = [];
  updateFamilyInfo();

  // Run the simulation
  runServerSimulation();
}

/**
 * Modified version of runServerSimulation that includes elderly focus
 * Uses the existing API but modifies the request data
 */
async function runServerSimulationWithElderlyFocus() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Running elderly crisis scenario...";
    statusElement.className = "status-message running";
  }

  try {
    // Get parameters from UI controls
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 100;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 8;
    const radius = parseFloat(document.getElementById("radius").value) || 5;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

    // Prepare request payload with simulation parameters
    // Add a custom flag for elderly focus
    const requestData = {
      peopleCount: peopleCount,
      shelterCount: shelterCount,
      centerLatitude: 31.2518, // Beer Sheva
      centerLongitude: 34.7913, // Beer Sheva
      radiusKm: radius,
      prioritySettings: {
        enableAgePriority: priorityEnabled,
        childMaxAge: 12,
        elderlyMinAge: 70,
      },
      elderlyFocus: true, // Custom flag for elderly scenario
      elderlyPercentage: 50, // 50% elderly
    };

    // Use fake server response for now since we can't modify server
    const data = generateElderlyScenarioData(requestData);

    // Get the visualizer instance
    const visualizer = window.visualizer || initializeVisualizer();

    // Clear any existing data
    visualizer.clearMap();

    // Display the results on the map using the visualizer
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Update status message
    if (statusElement) {
      statusElement.textContent = "Elderly crisis scenario running";
      statusElement.className = "status-message success";

      // Clear the status after a few seconds
      setTimeout(() => {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }, 3000);
    }
  } catch (error) {
    // Handle errors
    console.error("Simulation error:", error);
    if (statusElement) {
      statusElement.textContent = `Error: ${error.message}`;
      statusElement.className = "status-message error";
    }
  }
}

/**
 * Modified version of runServerSimulation that includes family groups
 * Because we can't modify the server, we'll generate a synthetic response
 */
async function runServerSimulationWithFamilies() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Running family groups scenario...";
    statusElement.className = "status-message running";
  }

  try {
    // Get parameters from UI controls
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 100;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 8;
    const radius = parseFloat(document.getElementById("radius").value) || 5;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

    // Prepare request payload with simulation parameters
    const requestData = {
      peopleCount: peopleCount,
      shelterCount: shelterCount,
      centerLatitude: 31.2518, // Beer Sheva
      centerLongitude: 34.7913, // Beer Sheva
      radiusKm: radius,
      prioritySettings: {
        enableAgePriority: priorityEnabled,
        childMaxAge: 12,
        elderlyMinAge: 70,
      },
      families: familyGroups, // Add family groups
    };

    // Since we can't modify the server, generate synthetic data
    const data = generateFamilyScenarioData(requestData);

    // Get the visualizer instance
    const visualizer = window.visualizer || initializeVisualizer();

    // Clear any existing data
    visualizer.clearMap();

    // Display the results on the map using the visualizer
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Update status message
    if (statusElement) {
      statusElement.textContent = "Family groups scenario running";
      statusElement.className = "status-message success";

      // Clear the status after a few seconds
      setTimeout(() => {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }, 3000);
    }
  } catch (error) {
    // Handle errors
    console.error("Simulation error:", error);
    if (statusElement) {
      statusElement.textContent = `Error: ${error.message}`;
      statusElement.className = "status-message error";
    }
  }
}

/**
 * Generates synthetic data for the family groups scenario
 * Creates data in the same format as what the server would return
 */
function generateFamilyScenarioData(requestData) {
  const random = new Random(Date.now());

  // Generate regular people first
  const people = [];
  for (let i = 0; i < requestData.peopleCount; i++) {
    let age;
    const ageRandom = random.nextDouble();

    if (ageRandom < 0.15) {
      age = random.nextInt(1, 19); // Children
    } else if (ageRandom < 0.85) {
      age = random.nextInt(19, 70); // Adults
    } else {
      age = random.nextInt(70, 95); // Elderly
    }

    people.push({
      id: i + 1,
      age: age,
      latitude:
        requestData.centerLatitude +
        ((random.nextDouble() - 0.5) * requestData.radiusKm) / 111.0,
      longitude:
        requestData.centerLongitude +
        ((random.nextDouble() - 0.5) * requestData.radiusKm) / 111.0,
    });
  }

  // Next ID to use for people
  let nextId = requestData.peopleCount + 1;

  // Now add family groups with members in close proximity
  const familyGroups = requestData.families || [];
  for (let i = 0; i < familyGroups.length; i++) {
    const family = familyGroups[i];

    // Generate a random location for the family
    const familyLat =
      requestData.centerLatitude +
      ((random.nextDouble() - 0.5) * requestData.radiusKm) / 111.0;
    const familyLon =
      requestData.centerLongitude +
      ((random.nextDouble() - 0.5) * requestData.radiusKm) / 111.0;

    // Add each family member nearby the family center
    for (let j = 0; j < family.members.length; j++) {
      const member = family.members[j];

      // A very small random offset so family members are nearby but not exactly in the same spot
      const offsetLat = (random.nextDouble() - 0.5) * 0.0005; // Very small offset
      const offsetLon = (random.nextDouble() - 0.5) * 0.0005;

      const personId = nextId++;
      people.push({
        id: personId,
        age: member.age,
        latitude: familyLat + offsetLat,
        longitude: familyLon + offsetLon,
        // Add a custom property to mark family members
        familyId: family.id,
      });

      // Store the assigned ID back to member
      member.id = personId;
    }
  }

  // Generate shelters
  const shelters = generateShelters(requestData);

  // Assign people to shelters
  // For families, we'll try to keep them together if possible
  const assignments = assignPeopleToShelters(
    people,
    shelters,
    requestData.prioritySettings,
    familyGroups
  );

  // Calculate statistics
  const assignedCount = Object.keys(assignments).length;
  const averageDistance =
    assignedCount > 0
      ? Object.values(assignments).reduce((sum, a) => sum + a.distance, 0) /
        assignedCount
      : 0;
  const maxDistance =
    assignedCount > 0
      ? Math.max(...Object.values(assignments).map((a) => a.distance))
      : 0;

  return {
    people: people,
    shelters: shelters,
    assignments: assignments,
    statistics: {
      executionTimeMs: 0,
      assignedCount: assignedCount,
      unassignedCount: people.length - assignedCount,
      assignmentPercentage:
        people.length > 0 ? assignedCount / people.length : 0,
      totalShelterCapacity: shelters.reduce((sum, s) => sum + s.capacity, 0),
      averageDistance: averageDistance,
      maxDistance: maxDistance,
      minDistance:
        assignedCount > 0
          ? Math.min(...Object.values(assignments).map((a) => a.distance))
          : 0,
    },
  };
}

/**
 * Generates synthetic data for the elderly crisis scenario
 */
function generateElderlyScenarioData(requestData) {
  const random = new Random(Date.now());

  // Generate people with 50% elderly
  const people = [];
  for (let i = 0; i < requestData.peopleCount; i++) {
    let age;
    const ageRandom = random.nextDouble();

    if (ageRandom < 0.5) {
      // 50% elderly
      age = random.nextInt(70, 95);
    } else if (ageRandom < 0.65) {
      // 15% children
      age = random.nextInt(1, 19);
    } else {
      // 35% adults
      age = random.nextInt(19, 70);
    }

    people.push({
      id: i + 1,
      age: age,
      latitude:
        requestData.centerLatitude +
        ((random.nextDouble() - 0.5) * requestData.radiusKm) / 111.0,
      longitude:
        requestData.centerLongitude +
        ((random.nextDouble() - 0.5) * requestData.radiusKm) / 111.0,
    });
  }

  // Generate shelters
  const shelters = generateShelters(requestData);

  // Assign people to shelters
  const assignments = assignPeopleToShelters(
    people,
    shelters,
    requestData.prioritySettings
  );

  // Calculate statistics
  const assignedCount = Object.keys(assignments).length;
  const averageDistance =
    assignedCount > 0
      ? Object.values(assignments).reduce((sum, a) => sum + a.distance, 0) /
        assignedCount
      : 0;
  const maxDistance =
    assignedCount > 0
      ? Math.max(...Object.values(assignments).map((a) => a.distance))
      : 0;

  return {
    people: people,
    shelters: shelters,
    assignments: assignments,
    statistics: {
      executionTimeMs: 0,
      assignedCount: assignedCount,
      unassignedCount: people.length - assignedCount,
      assignmentPercentage:
        people.length > 0 ? assignedCount / people.length : 0,
      totalShelterCapacity: shelters.reduce((sum, s) => sum + s.capacity, 0),
      averageDistance: averageDistance,
      maxDistance: maxDistance,
      minDistance:
        assignedCount > 0
          ? Math.min(...Object.values(assignments).map((a) => a.distance))
          : 0,
    },
  };
}

/**
 * Generate shelters for synthetic data
 */
function generateShelters(requestData) {
  const random = new Random(Date.now());
  const shelters = [];

  // Known Beer Sheva locations for realism
  const knownLocations = [
    { name: "Ben Gurion University", lat: 31.2634, lon: 34.8044 },
    { name: "Beer Sheva Central Station", lat: 31.2434, lon: 34.798 },
    { name: "Grand Canyon Mall", lat: 31.2508, lon: 34.7738 },
    { name: "Soroka Medical Center", lat: 31.2534, lon: 34.8018 },
  ];

  // Add known locations first
  for (
    let i = 0;
    i < Math.min(requestData.shelterCount, knownLocations.length);
    i++
  ) {
    const location = knownLocations[i];
    shelters.push({
      id: i + 1,
      name: location.name,
      latitude: location.lat,
      longitude: location.lon,
      capacity: random.nextInt(1, 6), // Capacity between 1 and 5
    });
  }

  // Add remaining random shelters if needed
  for (let i = knownLocations.length; i < requestData.shelterCount; i++) {
    const angle = random.nextDouble() * 2 * Math.PI;
    const distance = (random.nextDouble() * requestData.radiusKm * 0.7) / 111.0;

    const latOffset = distance * Math.cos(angle);
    const lonOffset = distance * Math.sin(angle);

    shelters.push({
      id: i + 1,
      name: `Shelter ${i + 1}`,
      latitude: requestData.centerLatitude + latOffset,
      longitude: requestData.centerLongitude + lonOffset,
      capacity: random.nextInt(1, 6), // Capacity between 1 and 5
    });
  }

  return shelters;
}

/**
 * Assign people to shelters, with special handling for families
 */
function assignPeopleToShelters(
  people,
  shelters,
  prioritySettings,
  families = []
) {
  const shelterCapacity = {};
  shelters.forEach((shelter) => {
    shelterCapacity[shelter.id] = shelter.capacity;
  });

  const assignments = {};

  // Constants for distance calculation
  const MAX_TRAVEL_TIME_MINUTES = 1.0;
  const WALKING_SPEED_KM_PER_MINUTE = 0.6;
  const MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE;

  // First, process family groups if any
  if (families && families.length > 0) {
    // List of families ordered by vulnerability (families with children and elderly first)
    const prioritizedFamilies = [...families].sort((a, b) => {
      const aScore = calculateFamilyVulnerabilityScore(a);
      const bScore = calculateFamilyVulnerabilityScore(b);
      return bScore - aScore;
    });

    // For each family, find a shelter with enough capacity for all members
    for (const family of prioritizedFamilies) {
      const familySize = family.members.length;

      // Find shelters with enough capacity for the whole family
      const suitableShelters = shelters
        .filter((shelter) => shelterCapacity[shelter.id] >= familySize)
        .map((shelter) => {
          // Calculate average distance from family members to this shelter
          let totalDistance = 0;
          for (const member of family.members) {
            const person = people.find((p) => p.id === member.id);
            if (person) {
              totalDistance += calculateDistance(
                person.latitude,
                person.longitude,
                shelter.latitude,
                shelter.longitude
              );
            }
          }
          const avgDistance = totalDistance / familySize;
          return { shelter, avgDistance };
        })
        .filter((item) => item.avgDistance <= MAX_DISTANCE_KM)
        .sort((a, b) => a.avgDistance - b.avgDistance);

      // If we found a suitable shelter, assign all family members to it
      if (suitableShelters.length > 0) {
        const { shelter, avgDistance } = suitableShelters[0];

        // Assign all family members to this shelter
        for (const member of family.members) {
          const person = people.find((p) => p.id === member.id);
          if (person) {
            const distance = calculateDistance(
              person.latitude,
              person.longitude,
              shelter.latitude,
              shelter.longitude
            );

            assignments[person.id] = {
              personId: person.id,
              shelterId: shelter.id,
              distance: distance,
            };
          }
        }

        // Update shelter capacity
        shelterCapacity[shelter.id] -= familySize;
      }
    }
  }

  // Process remaining individuals
  // First, create a list of all possible assignments within range
  const possibleAssignments = [];

  for (const person of people) {
    // Skip if already assigned (likely as part of a family)
    if (assignments[person.id]) continue;

    // Calculate vulnerability score if priority is enabled
    const vulnerabilityScore = prioritySettings?.enableAgePriority
      ? calculateVulnerabilityScore(person.age)
      : 0;

    for (const shelter of shelters) {
      if (shelterCapacity[shelter.id] <= 0) continue;

      const distance = calculateDistance(
        person.latitude,
        person.longitude,
        shelter.latitude,
        shelter.longitude
      );

      // Only consider shelters within maximum distance
      if (distance <= MAX_DISTANCE_KM) {
        possibleAssignments.push({
          personId: person.id,
          shelterId: shelter.id,
          distance: distance,
          vulnerabilityScore: vulnerabilityScore,
        });
      }
    }
  }

  // Sort by priority (if enabled) and then by distance
  possibleAssignments.sort((a, b) => {
    if (prioritySettings?.enableAgePriority) {
      if (a.vulnerabilityScore !== b.vulnerabilityScore) {
        return b.vulnerabilityScore - a.vulnerabilityScore;
      }
    }
    return a.distance - b.distance;
  });

  // Make assignments
  for (const assignment of possibleAssignments) {
    // Skip if this person is already assigned
    if (assignments[assignment.personId]) continue;

    // Skip if shelter is full
    if (shelterCapacity[assignment.shelterId] <= 0) continue;

    // Make the assignment
    assignments[assignment.personId] = {
      personId: assignment.personId,
      shelterId: assignment.shelterId,
      distance: assignment.distance,
    };

    // Update shelter capacity
    shelterCapacity[assignment.shelterId]--;
  }

  return assignments;
}

/**
 * Calculate a family's vulnerability score based on members' ages
 */
function calculateFamilyVulnerabilityScore(family) {
  let score = 0;
  for (const member of family.members) {
    score += calculateVulnerabilityScore(member.age);
  }
  return score / family.members.length;
}

/**
 * Calculate an individual's vulnerability score based on age
 */
function calculateVulnerabilityScore(age) {
  if (age >= 70) {
    // Elderly (70+): highest priority
    return 10;
  } else if (age <= 12) {
    // Children (0-12): second highest priority
    return 8;
  } else if (age >= 60) {
    // Older adults (60-69): medium-high priority
    return 6;
  } else if (age <= 18) {
    // Teenagers (13-18): medium priority
    return 4;
  } else {
    // Adults (19-59): lowest priority
    return 2;
  }
}

/**
 * Calculate distance between two points using the Haversine formula
 */
function calculateDistance(lat1, lon1, lat2, lon2) {
  const R = 6371; // Earth radius in km
  const dLat = ((lat2 - lat1) * Math.PI) / 180;
  const dLon = ((lon2 - lon1) * Math.PI) / 180;

  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos((lat1 * Math.PI) / 180) *
      Math.cos((lat2 * Math.PI) / 180) *
      Math.sin(dLon / 2) *
      Math.sin(dLon / 2);

  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

/**
 * Update family information in the UI
 */
function updateFamilyInfo() {
  const familyListElement = document.getElementById("family-groups-list");
  if (!familyListElement) return;

  if (familyGroups.length === 0) {
    familyListElement.innerHTML = "No family groups created yet";
    return;
  }

  let html = "";
  for (const family of familyGroups) {
    const childrenCount = family.members.filter((m) => m.age <= 12).length;
    const elderlyCount = family.members.filter((m) => m.age >= 70).length;
    const adultCount = family.members.length - childrenCount - elderlyCount;

    html += `
      <div class="family-group" style="border-left: 4px solid ${family.color}">
        <h5>Family ${family.id}</h5>
        <p>${family.members.length} members: ${adultCount} adults, ${childrenCount} children, ${elderlyCount} elderly</p>
      </div>
    `;
  }

  familyListElement.innerHTML = html;
}

/**
 * Update the status message in the UI
 */
function updateStatusMessage(message) {
  const statusElement = document.getElementById("simulation-status");
  if (statusElement) {
    statusElement.textContent = message;
    statusElement.className = "status-message running";
  }
}

/**
 * Generate a random color for family visualization
 */
function getRandomColor() {
  const letters = "0123456789ABCDEF";
  let color = "#";
  for (let i = 0; i < 6; i++) {
    color += letters[Math.floor(Math.random() * 16)];
  }
  return color;
}

/**
 * Simple Random Number Generator for deterministic outputs
 */
class Random {
  constructor(seed) {
    this.seed = seed % 2147483647;
    if (this.seed <= 0) this.seed += 2147483646;
  }

  next() {
    return (this.seed = (this.seed * 16807) % 2147483647);
  }

  nextDouble() {
    return (this.next() - 1) / 2147483646;
  }

  nextInt(min, max) {
    return Math.floor(this.nextDouble() * (max - min)) + min;
  }
}

// Add the zero capacity shelters scenario function
function zeroCapacitySheltersScenario() {
  updateStatusMessage("Setting up zero capacity shelters scenario...");

  // Set moderate population
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "150";
  }

  // Set many shelters (some will have zero capacity)
  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "15";
  }

  // Enable priority
  const prioritySelect = document.getElementById("priority");
  if (prioritySelect) {
    prioritySelect.value = "true";
  }

  // Run custom simulation with zero capacity shelters
  runZeroCapacitySheltersSimulation();
}

// Add the zero capacity shelters simulation function
async function runZeroCapacitySheltersSimulation() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Running zero capacity shelters scenario...";
    statusElement.className = "status-message running";
  }

  try {
    // Get parameters from UI
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 150;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 15;
    const radius = parseFloat(document.getElementById("radius").value) || 0.8;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

    // Create a modified request with zero capacity shelters flag
    const requestData = {
      peopleCount: peopleCount,
      shelterCount: shelterCount,
      centerLatitude: 31.2518, // Beer Sheva
      centerLongitude: 34.7913, // Beer Sheva
      radiusKm: radius,
      prioritySettings: {
        enableAgePriority: priorityEnabled,
        childMaxAge: 12,
        elderlyMinAge: 70,
      },
      zeroCapacityShelters: true, // Custom flag for zero capacity scenario
    };

    // Call server simulation with our special flag
    // Modified server-side handler will generate some shelters with zero capacity
    const response = await fetch(
      `https://localhost:${PORT}/api/Simulation/run`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(requestData),
      }
    );

    // Process response as normal
    if (!response.ok) {
      throw new Error(
        `Server responded with ${response.status}: ${response.statusText}`
      );
    }

    const data = await response.json();
    const visualizer = window.visualizer || initializeVisualizer();
    visualizer.clearMap();
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Update status
    if (statusElement) {
      statusElement.textContent = "Zero capacity shelters scenario running";
      statusElement.className = "status-message success";

      setTimeout(() => {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }, 3000);
    }
  } catch (error) {
    console.error("Simulation error:", error);
    if (statusElement) {
      statusElement.textContent = `Error: ${error.message}`;
      statusElement.className = "status-message error";
    }
  }
}

// Initialize the extreme scenario controls after the page loads
document.addEventListener("DOMContentLoaded", function () {
  // Add a small delay to ensure the map and other controls are ready
  setTimeout(() => {
    addExtremeScenarioControls();
  }, 1000);
});
