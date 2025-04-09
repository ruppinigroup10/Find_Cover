/**
 * Advanced extreme scenarios for shelter simulation
 * These functions test edge cases and additional critical scenarios
 */

/**
 * Add additional extreme scenario buttons to the UI
 * This extends the existing extreme scenarios panel
 */
function addAdvancedExtremeScenarios() {
  // Check if extreme scenarios are already added
  // if (document.querySelector(".scenario-heading")) {
  //   console.log("Extreme scenarios already added, skipping");
  //   return;
  // }

  // Find the existing extreme scenarios section
  const controlContainer = document.querySelector(".control-container");
  const existingButtons = document.querySelectorAll(".scenario-button");

  // If we already have buttons or can't find the container, exit
  if (!controlContainer || existingButtons.length === 0) {
    console.error("Cannot find control container or existing buttons");
    return;
  }

  // Get the last button (usually Reset)
  const lastButton = existingButtons[existingButtons.length - 1];

  // Create a divider after the existing buttons
  const divider = document.createElement("hr");
  divider.className = "control-divider";

  // Insert divider after the last button
  lastButton.insertAdjacentElement("afterend", divider);

  // Create a heading for advanced scenarios
  const heading = document.createElement("h4");
  heading.textContent = "Advanced Edge Cases";
  heading.className = "scenario-heading";
  divider.insertAdjacentElement("afterend", heading);

  // Define the advanced scenarios
  const advancedScenarios = [
    {
      id: "scenario-zero-capacity",
      text: "Zero Capacity Shelters",
      handler: zeroCapacitySheltersScenario,
      color: "#e57373",
      hoverColor: "#ef5350",
    },
    {
      id: "scenario-barriers",
      text: "Geographic Barriers",
      handler: geographicBarriersScenario,
      color: "#81c784",
      hoverColor: "#66bb6a",
    },
    {
      id: "scenario-walking-distance",
      text: "Limited Mobility (200m)",
      handler: limitedMobilityScenario,
      color: "#64b5f6",
      hoverColor: "#42a5f5",
    },
    {
      id: "scenario-population-cluster",
      text: "Population Cluster",
      handler: populationClusterScenario,
      color: "#ba68c8",
      hoverColor: "#ab47bc",
    },
    {
      id: "scenario-mixed-1",
      text: "Mixed Case: Elderly & Limited Range",
      handler: mixedElderlyLimitedRangeScenario,
      color: "#ffb74d",
      hoverColor: "#ffa726",
    },
  ];

  // Add each advanced scenario button
  let lastElement = heading;
  for (const scenario of advancedScenarios) {
    const button = document.createElement("button");
    button.id = scenario.id;
    button.textContent = scenario.text;
    button.className =
      "scenario-button advanced-scenario-button control-button";
    button.style.backgroundColor = scenario.color;
    button.style.borderColor = scenario.hoverColor;

    // Add hover effect using event listeners
    button.addEventListener("mouseenter", function () {
      this.style.backgroundColor = scenario.hoverColor;
    });

    button.addEventListener("mouseleave", function () {
      this.style.backgroundColor = scenario.color;
    });

    button.addEventListener("click", scenario.handler);

    // Insert after the previous element
    lastElement.insertAdjacentElement("afterend", button);
    lastElement = button;
  }

  // Add a placeholder for visualization options
  const vizOptionsDiv = document.createElement("div");
  vizOptionsDiv.id = "visualization-options";
  vizOptionsDiv.className = "info-container";
  vizOptionsDiv.innerHTML = `
    <h4>Visualization Options</h4>
    <div class="viz-option">
      <label class="toggle-switch">
        <input type="checkbox" id="toggle-animation" checked>
        <span class="toggle-slider"></span>
      </label>
      <span>Animate assignments</span>
    </div>
    <div class="viz-option">
      <label class="toggle-switch">
        <input type="checkbox" id="toggle-highlight-critical">
        <span class="toggle-slider"></span>
      </label>
      <span>Highlight critical people</span>
    </div>
  `;

  lastElement.insertAdjacentElement("afterend", vizOptionsDiv);

  // Add event listeners for toggle switches
  setTimeout(() => {
    const animationToggle = document.getElementById("toggle-animation");
    if (animationToggle) {
      animationToggle.addEventListener("change", function () {
        window.animateAssignments = this.checked;
      });
      window.animateAssignments = animationToggle.checked;
    }

    const highlightToggle = document.getElementById(
      "toggle-highlight-critical"
    );
    if (highlightToggle) {
      highlightToggle.addEventListener("change", function () {
        window.highlightCritical = this.checked;
        if (this.checked) {
          highlightCriticalPeople();
        } else {
          resetHighlights();
        }
      });
      window.highlightCritical = highlightToggle.checked;
    }
  }, 500);

  // Add the necessary CSS
  const style = document.createElement("style");
  style.textContent = `
    .advanced-scenario-button {
      color: white;
      font-weight: bold;
      text-shadow: 0 1px 1px rgba(0,0,0,0.2);
    }
    
    .toggle-switch {
      position: relative;
      display: inline-block;
      width: 40px;
      height: 20px;
    }
    
    .toggle-switch input {
      opacity: 0;
      width: 0;
      height: 0;
    }
    
    .toggle-slider {
      position: absolute;
      cursor: pointer;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: #ccc;
      transition: .4s;
      border-radius: 20px;
    }
    
    .toggle-slider:before {
      position: absolute;
      content: "";
      height: 16px;
      width: 16px;
      left: 2px;
      bottom: 2px;
      background-color: white;
      transition: .4s;
      border-radius: 50%;
    }
    
    input:checked + .toggle-slider {
      background-color: #2196F3;
    }
    
    input:checked + .toggle-slider:before {
      transform: translateX(20px);
    }
    
    .viz-option {
      display: flex;
      align-items: center;
      margin: 8px 0;
    }
    
    .viz-option span {
      margin-left: 10px;
      font-size: 14px;
    }
    
    .critical-person {
      border: 3px solid red !important;
      box-shadow: 0 0 10px red !important;
      z-index: 1000 !important;
    }
    
    .barrier {
      stroke-width: 3;
      stroke: red;
      stroke-dasharray: 10, 5;
    }
  `;
  document.head.appendChild(style);
}

/**
 * Scenario with some shelters having zero capacity
 * Tests how the algorithm handles completely unavailable shelters
 */
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

  // Set small radius for more constrained conditions
  const radiusInput = document.getElementById("radius");
  if (radiusInput) {
    radiusInput.value = "0.8";
  }

  // Run custom simulation with zero capacity shelters
  runZeroCapacitySheltersSimulation();
}

/**
 * Custom simulation with some shelters having zero capacity
 */
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
      zeroCapacityShelters: true,
    };

    // Generate synthetic data with zero capacity shelters
    const data = generateZeroCapacitySheltersData(requestData);

    // Get visualizer
    const visualizer = window.visualizer || initializeVisualizer();

    // Clear existing data
    visualizer.clearMap();

    // Display results
    visualizeWithAnimation(data, visualizer);

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

/**
 * Generate data for zero capacity shelters scenario
 */
function generateZeroCapacitySheltersData(requestData) {
  const random = new Random(Date.now());

  // Generate people
  const people = generateRandomPeople(requestData, random);

  // Generate shelters with some having zero capacity
  const shelters = [];

  // Known Beer Sheva locations
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
    // These known shelters will have normal capacity
    shelters.push({
      id: i + 1,
      name: location.name,
      latitude: location.lat,
      longitude: location.lon,
      capacity: random.nextInt(3, 6), // Capacity between 3 and 5
    });
  }

  // Add remaining random shelters
  for (let i = knownLocations.length; i < requestData.shelterCount; i++) {
    const angle = random.nextDouble() * 2 * Math.PI;
    const distance = (random.nextDouble() * requestData.radiusKm * 0.7) / 111.0;

    const latOffset = distance * Math.cos(angle);
    const lonOffset = distance * Math.sin(angle);

    // 40% chance of zero capacity for random shelters
    const capacity = random.nextDouble() < 0.4 ? 0 : random.nextInt(1, 4);

    shelters.push({
      id: i + 1,
      name: capacity === 0 ? `Closed Shelter ${i + 1}` : `Shelter ${i + 1}`,
      latitude: requestData.centerLatitude + latOffset,
      longitude: requestData.centerLongitude + lonOffset,
      capacity: capacity,
    });
  }

  // Assign people to shelters
  const assignments = assignPeopleToShelters(
    people,
    shelters,
    requestData.prioritySettings
  );

  // Calculate statistics
  const stats = calculateStatistics(people, shelters, assignments);

  return {
    people: people,
    shelters: shelters,
    assignments: assignments,
    statistics: stats,
  };
}

/**
 * Scenario with geographic barriers dividing the area
 * Tests how algorithm handles non-continuous walkable areas
 */
function geographicBarriersScenario() {
  updateStatusMessage("Setting up geographic barriers scenario...");

  // Set moderate population
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "120";
  }

  // Set moderate number of shelters
  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "12";
  }

  // Enable priority
  const prioritySelect = document.getElementById("priority");
  if (prioritySelect) {
    prioritySelect.value = "true";
  }

  // Set moderate radius
  const radiusInput = document.getElementById("radius");
  if (radiusInput) {
    radiusInput.value = "1.5";
  }

  // Run custom simulation with geographic barriers
  runGeographicBarriersSimulation();
}

/**
 * Custom simulation with geographic barriers
 */
async function runGeographicBarriersSimulation() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Running geographic barriers scenario...";
    statusElement.className = "status-message running";
  }

  try {
    // Get parameters from UI
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 120;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 12;
    const radius = parseFloat(document.getElementById("radius").value) || 1.5;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

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
      barriers: [
        // Define a line barrier running east-west through the center
        {
          start: { lat: 31.2518, lon: 34.7913 - radius / 111.0 },
          end: { lat: 31.2518, lon: 34.7913 + radius / 111.0 },
        },
      ],
    };

    // Generate synthetic data with barriers
    const data = generateGeographicBarriersData(requestData);

    // Get visualizer
    const visualizer = window.visualizer || initializeVisualizer();

    // Clear existing data
    visualizer.clearMap();

    // Display results
    visualizeWithAnimation(data, visualizer);

    // Draw the barrier on the map
    drawBarriers(requestData.barriers, visualizer);

    // Update status
    if (statusElement) {
      statusElement.textContent = "Geographic barriers scenario running";
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

/**
 * Draw barriers on the map
 */
function drawBarriers(barriers, visualizer) {
  if (!barriers || !visualizer) return;

  barriers.forEach((barrier) => {
    const line = L.polyline(
      [
        [barrier.start.lat, barrier.start.lon],
        [barrier.end.lat, barrier.end.lon],
      ],
      {
        color: "red",
        weight: 4,
        opacity: 0.8,
        dashArray: "10, 5",
        className: "barrier",
      }
    );

    visualizer.pathLines.addLayer(line);
  });
}

/**
 * Generate data for geographic barriers scenario
 */
function generateGeographicBarriersData(requestData) {
  const random = new Random(Date.now());

  // Generate people on both sides of the barrier
  const people = [];

  // Define barrier line
  const barrier = requestData.barriers[0];

  // North-south division for simplicity
  // Generate points on both sides of the barrier
  for (let i = 0; i < requestData.peopleCount; i++) {
    // Generate a random point
    let lat, lon;
    const side = random.nextDouble() < 0.5 ? "north" : "south";

    if (side === "north") {
      // North of the barrier
      lat =
        barrier.start.lat +
        (random.nextDouble() * requestData.radiusKm * 0.8) / 111.0;
      lon =
        barrier.start.lon +
        ((random.nextDouble() - 0.5) * requestData.radiusKm) / 111.0;
    } else {
      // South of the barrier
      lat =
        barrier.start.lat -
        (random.nextDouble() * requestData.radiusKm * 0.8) / 111.0;
      lon =
        barrier.start.lon +
        ((random.nextDouble() - 0.5) * requestData.radiusKm) / 111.0;
    }

    // Generate age with proper distribution
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
      latitude: lat,
      longitude: lon,
      side: side, // Store which side of the barrier they're on
    });
  }

  // Generate shelters on both sides of the barrier
  const shelters = [];

  // Known Beer Sheva locations (adjusted to be on different sides of the barrier)
  const northLocations = [
    { name: "Ben Gurion University", lat: 31.2634, lon: 34.8044 },
    { name: "Soroka Medical Center", lat: 31.2534, lon: 34.8018 },
  ];

  const southLocations = [
    { name: "Beer Sheva Central Station", lat: 31.2434, lon: 34.798 },
    { name: "Grand Canyon Mall", lat: 31.2508 - 0.02, lon: 34.7738 },
  ];

  // Add known locations
  let shelterId = 1;

  // Add north locations
  for (const location of northLocations) {
    shelters.push({
      id: shelterId++,
      name: location.name,
      latitude: location.lat,
      longitude: location.lon,
      capacity: random.nextInt(3, 6), // Capacity between 3 and 5
      side: "north",
    });
  }

  // Add south locations
  for (const location of southLocations) {
    shelters.push({
      id: shelterId++,
      name: location.name,
      latitude: location.lat,
      longitude: location.lon,
      capacity: random.nextInt(3, 6), // Capacity between 3 and 5
      side: "south",
    });
  }

  // Add remaining random shelters
  const remainingShelters =
    requestData.shelterCount - northLocations.length - southLocations.length;

  // Distribute remaining shelters evenly
  for (let i = 0; i < remainingShelters; i++) {
    const side = i % 2 === 0 ? "north" : "south";

    let lat, lon;
    if (side === "north") {
      // North of the barrier
      lat =
        barrier.start.lat +
        (random.nextDouble() * requestData.radiusKm * 0.6) / 111.0;
      lon =
        barrier.start.lon +
        ((random.nextDouble() - 0.5) * requestData.radiusKm * 0.8) / 111.0;
    } else {
      // South of the barrier
      lat =
        barrier.start.lat -
        (random.nextDouble() * requestData.radiusKm * 0.6) / 111.0;
      lon =
        barrier.start.lon +
        ((random.nextDouble() - 0.5) * requestData.radiusKm * 0.8) / 111.0;
    }

    shelters.push({
      id: shelterId++,
      name: `${side === "north" ? "North" : "South"} Shelter ${i + 1}`,
      latitude: lat,
      longitude: lon,
      capacity: random.nextInt(2, 5), // Capacity between 2 and 4
      side: side,
    });
  }

  // Function to check if a path crosses the barrier
  function crossesBarrier(lat1, lon1, lat2, lon2, barrier) {
    // Simplified barrier check - just check if points are on opposite sides
    // For a real implementation, you'd need actual line intersection calculation
    const point1Side = lat1 > barrier.start.lat ? "north" : "south";
    const point2Side = lat2 > barrier.start.lat ? "north" : "south";

    return point1Side !== point2Side;
  }

  // Assign people to shelters, respecting the barrier
  const assignments = {};
  const shelterCapacity = {};

  // Initialize shelter capacities
  shelters.forEach((shelter) => {
    shelterCapacity[shelter.id] = shelter.capacity;
  });

  // Constants for distance calculation
  const MAX_TRAVEL_TIME_MINUTES = 1.0;
  const WALKING_SPEED_KM_PER_MINUTE = 0.6;
  const MAX_DISTANCE_KM = MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE;

  // First, create a list of all possible assignments within range and not crossing the barrier
  const possibleAssignments = [];

  for (const person of people) {
    for (const shelter of shelters) {
      // Skip if shelter capacity is zero
      if (shelterCapacity[shelter.id] <= 0) continue;

      // Calculate distance
      const distance = calculateDistance(
        person.latitude,
        person.longitude,
        shelter.latitude,
        shelter.longitude
      );

      // Check if within range and doesn't cross the barrier
      const withinRange = distance <= MAX_DISTANCE_KM;
      const doesNotCrossBarrier = !crossesBarrier(
        person.latitude,
        person.longitude,
        shelter.latitude,
        shelter.longitude,
        barrier
      );

      if (withinRange && doesNotCrossBarrier) {
        // Calculate vulnerability score
        const vulnerabilityScore = requestData.prioritySettings
          ?.enableAgePriority
          ? calculateVulnerabilityScore(person.age)
          : 0;

        possibleAssignments.push({
          personId: person.id,
          shelterId: shelter.id,
          distance: distance,
          vulnerabilityScore: vulnerabilityScore,
        });
      }
    }
  }

  // Sort by priority and distance
  possibleAssignments.sort((a, b) => {
    if (requestData.prioritySettings?.enableAgePriority) {
      if (a.vulnerabilityScore !== b.vulnerabilityScore) {
        return b.vulnerabilityScore - a.vulnerabilityScore;
      }
    }
    return a.distance - b.distance;
  });

  // Make assignments
  for (const assignment of possibleAssignments) {
    // Skip if person already assigned or shelter full
    if (
      assignments[assignment.personId] ||
      shelterCapacity[assignment.shelterId] <= 0
    ) {
      continue;
    }

    // Make the assignment
    assignments[assignment.personId] = {
      personId: assignment.personId,
      shelterId: assignment.shelterId,
      distance: assignment.distance,
    };

    // Update shelter capacity
    shelterCapacity[assignment.shelterId]--;
  }

  // Calculate statistics
  const stats = calculateStatistics(people, shelters, assignments);

  return {
    people: people,
    shelters: shelters,
    assignments: assignments,
    statistics: stats,
    barriers: requestData.barriers,
  };
}

/**
 * Scenario with limited mobility (shorter walking distance)
 * Tests how algorithm handles people with mobility issues
 */
function limitedMobilityScenario() {
  updateStatusMessage("Setting up limited mobility scenario...");

  // Set moderate population
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "150";
  }

  // Set fewer shelters to create more challenges
  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "10";
  }

  // Enable priority
  const prioritySelect = document.getElementById("priority");
  if (prioritySelect) {
    prioritySelect.value = "true";
  }

  // Set larger radius for more spread out population
  const radiusInput = document.getElementById("radius");
  if (radiusInput) {
    radiusInput.value = "1.2";
  }

  // Run custom simulation with limited mobility
  runLimitedMobilitySimulation();
}

/**
 * Custom simulation with limited mobility
 */
async function runLimitedMobilitySimulation() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Running limited mobility scenario...";
    statusElement.className = "status-message running";
  }

  try {
    // Get parameters from UI
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 150;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 10;
    const radius = parseFloat(document.getElementById("radius").value) || 1.2;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

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
      limitedMobility: {
        elderlyRange: 0.2, // 200 meters for elderly
        childrenRange: 0.4, // 400 meters for children
        adultRange: 0.6, // 600 meters for adults (standard)
      },
    };

    // Generate synthetic data
    const data = generateLimitedMobilityData(requestData);

    // Get visualizer
    const visualizer = window.visualizer || initializeVisualizer();

    // Clear existing data
    visualizer.clearMap();

    // Display results
    visualizeWithAnimation(data, visualizer);

    // Update status
    if (statusElement) {
      statusElement.textContent = "Limited mobility scenario running";
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

/**
 * Generate data for limited mobility scenario
 */
function generateLimitedMobilityData(requestData) {
  const random = new Random(Date.now());

  // Generate people
  const people = generateRandomPeople(requestData, random);

  // Generate shelters
  const shelters = generateShelters(requestData, random);

  // Assign people to shelters with limited mobility constraints
  const assignments = {};
  const shelterCapacity = {};

  // Initialize shelter capacities
  shelters.forEach((shelter) => {
    shelterCapacity[shelter.id] = shelter.capacity;
  });

  // First, create a list of all possible assignments within mobility range
  const possibleAssignments = [];

  for (const person of people) {
    // Determine max range based on age
    let maxRangeKm;
    if (person.age >= 70) {
      // Elderly have shortest range
      maxRangeKm = requestData.limitedMobility.elderlyRange;
    } else if (person.age <= 12) {
      // Children have medium range
      maxRangeKm = requestData.limitedMobility.childrenRange;
    } else {
      // Adults have standard range
      maxRangeKm = requestData.limitedMobility.adultRange;
    }

    // Add mobility range property to person for visualization
    person.mobilityRange = maxRangeKm;

    for (const shelter of shelters) {
      // Skip if shelter capacity is zero
      if (shelterCapacity[shelter.id] <= 0) continue;

      // Calculate distance
      const distance = calculateDistance(
        person.latitude,
        person.longitude,
        shelter.latitude,
        shelter.longitude
      );

      // Check if within this person's range
      if (distance <= maxRangeKm) {
        // Calculate vulnerability score
        const vulnerabilityScore = requestData.prioritySettings
          ?.enableAgePriority
          ? calculateVulnerabilityScore(person.age)
          : 0;

        possibleAssignments.push({
          personId: person.id,
          shelterId: shelter.id,
          distance: distance,
          vulnerabilityScore: vulnerabilityScore,
        });
      }
    }
  }

  // Sort by priority and distance
  possibleAssignments.sort((a, b) => {
    if (requestData.prioritySettings?.enableAgePriority) {
      if (a.vulnerabilityScore !== b.vulnerabilityScore) {
        return b.vulnerabilityScore - a.vulnerabilityScore;
      }
    }
    return a.distance - b.distance;
  });

  // Make assignments
  for (const assignment of possibleAssignments) {
    // Skip if person already assigned or shelter full
    if (
      assignments[assignment.personId] ||
      shelterCapacity[assignment.shelterId] <= 0
    ) {
      continue;
    }

    // Make the assignment
    assignments[assignment.personId] = {
      personId: assignment.personId,
      shelterId: assignment.shelterId,
      distance: assignment.distance,
    };

    // Update shelter capacity
    shelterCapacity[assignment.shelterId]--;
  }

  // Calculate statistics
  const stats = calculateStatistics(people, shelters, assignments);

  return {
    people: people,
    shelters: shelters,
    assignments: assignments,
    statistics: stats,
    limitedMobility: true,
  };
}

/**
 * Scenario with population cluster (high density area)
 * Tests how algorithm handles highly concentrated groups of people
 */
function populationClusterScenario() {
  updateStatusMessage("Setting up population cluster scenario...");

  // Set high population
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "250";
  }

  // Set moderate number of shelters
  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "12";
  }

  // Enable priority
  const prioritySelect = document.getElementById("priority");
  if (prioritySelect) {
    prioritySelect.value = "true";
  }

  // Set moderate radius
  const radiusInput = document.getElementById("radius");
  if (radiusInput) {
    radiusInput.value = "1.0";
  }

  // Run custom simulation with population cluster
  runPopulationClusterSimulation();
}

/**
 * Custom simulation with population cluster
 */
async function runPopulationClusterSimulation() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Running population cluster scenario...";
    statusElement.className = "status-message running";
  }

  try {
    // Get parameters from UI
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 250;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 12;
    const radius = parseFloat(document.getElementById("radius").value) || 1.0;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

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
      clusters: [
        // Define a high-density residential area (like an apartment complex)
        {
          center: { lat: 31.2518 + 0.005, lon: 34.7913 + 0.005 },
          radius: 0.15, // 150 meters radius
          density: 0.7, // 70% of population in this small area
        },
      ],
    };

    // Generate synthetic data with population cluster
    const data = generatePopulationClusterData(requestData);

    // Get visualizer
    const visualizer = window.visualizer || initializeVisualizer();

    // Clear existing data
    visualizer.clearMap();

    // Display results
    visualizeWithAnimation(data, visualizer);

    // Mark the cluster area on the map
    drawClusterCircles(requestData.clusters, visualizer);

    // Update status
    if (statusElement) {
      statusElement.textContent = "Population cluster scenario running";
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

/**
 * Draw circles representing population clusters on the map
 */
function drawClusterCircles(clusters, visualizer) {
  if (!clusters || !visualizer) return;

  clusters.forEach((cluster) => {
    // Draw a circle representing the cluster
    const circle = L.circle([cluster.center.lat, cluster.center.lon], {
      radius: cluster.radius * 1000, // Convert km to meters
      color: "purple",
      fillColor: "#9c27b0",
      fillOpacity: 0.2,
      weight: 2,
      dashArray: "5, 5",
    });

    // Add label
    circle.bindTooltip("High-density residential area", {
      permanent: true,
      direction: "center",
      className: "cluster-label",
    });

    visualizer.pathLines.addLayer(circle);
  });
}

/**
 * Generate data for population cluster scenario
 */
function generatePopulationClusterData(requestData) {
  const random = new Random(Date.now());

  // Generate people with clustering
  const people = [];
  const cluster = requestData.clusters[0];

  // Determine how many people go in the cluster
  const clusterPopulation = Math.floor(
    requestData.peopleCount * cluster.density
  );
  const regularPopulation = requestData.peopleCount - clusterPopulation;

  // Generate people in the cluster
  for (let i = 0; i < clusterPopulation; i++) {
    // Generate a point within the cluster
    // Use a circular distribution around the cluster center
    const angle = random.nextDouble() * 2 * Math.PI;
    const distance = random.nextDouble() * cluster.radius;

    // Convert to x,y offsets
    const latOffset = (distance * Math.cos(angle)) / 111.0;
    const lonOffset =
      (distance * Math.sin(angle)) /
      (111.0 * Math.cos((cluster.center.lat * Math.PI) / 180));

    // Calculate final coordinates
    const lat = cluster.center.lat + latOffset;
    const lon = cluster.center.lon + lonOffset;

    // Generate age with higher percentage of families and elderly in residential area
    let age;
    const ageRandom = random.nextDouble();

    if (ageRandom < 0.2) {
      age = random.nextInt(1, 19); // 20% Children
    } else if (ageRandom < 0.7) {
      age = random.nextInt(19, 70); // 50% Adults
    } else {
      age = random.nextInt(70, 95); // 30% Elderly (higher than normal)
    }

    people.push({
      id: i + 1,
      age: age,
      latitude: lat,
      longitude: lon,
      inCluster: true,
    });
  }

  // Generate remaining people in regular distribution
  for (let i = 0; i < regularPopulation; i++) {
    // Generate a random point outside the cluster
    let lat, lon;
    let insideCluster = true;

    // Keep trying until we get a point outside the cluster
    while (insideCluster) {
      // Standard distribution around center
      const angle = random.nextDouble() * 2 * Math.PI;
      const distance = (random.nextDouble() * requestData.radiusKm) / 111.0;

      lat = requestData.centerLatitude + distance * Math.cos(angle);
      lon = requestData.centerLongitude + distance * Math.sin(angle);

      // Check if inside cluster
      const distToCluster = calculateDistance(
        lat,
        lon,
        cluster.center.lat,
        cluster.center.lon
      );

      insideCluster = distToCluster <= cluster.radius;
    }

    // Generate age with normal distribution
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
      id: clusterPopulation + i + 1,
      age: age,
      latitude: lat,
      longitude: lon,
      inCluster: false,
    });
  }

  // Generate shelters - avoid placing them too close to the cluster center
  const shelters = [];

  // Known Beer Sheva locations
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
      capacity: random.nextInt(3, 8), // Slightly larger capacity (3-7)
    });
  }

  // Add remaining random shelters
  for (let i = knownLocations.length; i < requestData.shelterCount; i++) {
    // Generate random location
    let lat, lon;
    let tooCloseToCluster = true;

    // Keep trying until we get a point not too close to the cluster
    // This creates a scarcity of shelters near the densely populated area
    while (tooCloseToCluster) {
      const angle = random.nextDouble() * 2 * Math.PI;
      const distance =
        (random.nextDouble() * requestData.radiusKm * 0.7) / 111.0;

      lat = requestData.centerLatitude + distance * Math.cos(angle);
      lon = requestData.centerLongitude + distance * Math.sin(angle);

      // Check distance to cluster center
      const distToCluster = calculateDistance(
        lat,
        lon,
        cluster.center.lat,
        cluster.center.lon
      );

      // Not too close but not too far either
      tooCloseToCluster = distToCluster < 0.25; // 250m minimum distance
    }

    shelters.push({
      id: i + 1,
      name: `Shelter ${i + 1}`,
      latitude: lat,
      longitude: lon,
      capacity: random.nextInt(3, 8), // Slightly larger capacity (3-7)
    });
  }

  // Standard shelter assignment
  const assignments = assignPeopleToShelters(
    people,
    shelters,
    requestData.prioritySettings
  );

  // Calculate statistics
  const stats = calculateStatistics(people, shelters, assignments);

  return {
    people: people,
    shelters: shelters,
    assignments: assignments,
    statistics: stats,
    clusters: requestData.clusters,
  };
}

/**
 * Mixed scenario with elderly population and limited mobility range
 * Combines multiple constraints to create a challenging test case
 */
function mixedElderlyLimitedRangeScenario() {
  updateStatusMessage(
    "Setting up mixed scenario: elderly population with limited mobility..."
  );

  // Set high population
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "180";
  }

  // Set limited shelters
  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "8";
  }

  // Enable priority
  const prioritySelect = document.getElementById("priority");
  if (prioritySelect) {
    prioritySelect.value = "true";
  }

  // Set larger radius for more challenges
  const radiusInput = document.getElementById("radius");
  if (radiusInput) {
    radiusInput.value = "1.5";
  }

  // Run custom mixed case simulation
  runMixedElderlyLimitedRangeSimulation();
}

/**
 * Custom simulation for mixed elderly and limited range scenario
 */
async function runMixedElderlyLimitedRangeSimulation() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent =
      "Running mixed elderly/limited mobility scenario...";
    statusElement.className = "status-message running";
  }

  try {
    // Get parameters from UI
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 180;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 8;
    const radius = parseFloat(document.getElementById("radius").value) || 1.5;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

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
      elderlyFocus: true,
      elderlyPercentage: 40, // 40% elderly
      limitedMobility: {
        elderlyRange: 0.2, // 200 meters for elderly
        childrenRange: 0.4, // 400 meters for children
        adultRange: 0.6, // 600 meters for adults
      },
    };

    // Generate synthetic data
    const data = generateMixedElderlyLimitedRangeData(requestData);

    // Get visualizer
    const visualizer = window.visualizer || initializeVisualizer();

    // Clear existing data
    visualizer.clearMap();

    // Display results
    visualizeWithAnimation(data, visualizer);

    // Add circles showing each person's mobility range
    if (window.highlightCritical) {
      showMobilityRanges(data.people, visualizer);
    }

    // Update status
    if (statusElement) {
      statusElement.textContent =
        "Mixed elderly/limited mobility scenario running";
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

/**
 * Show mobility ranges as circles around each person
 */
function showMobilityRanges(people, visualizer) {
  // Only show for a subset to avoid cluttering the map
  const criticalPeople = people.filter((p) => p.age >= 80 || p.age <= 6);
  const sampleSize = Math.min(criticalPeople.length, 10);
  const sample = criticalPeople.slice(0, sampleSize);

  for (const person of sample) {
    // Draw a circle representing their mobility range
    const circle = L.circle([person.latitude, person.longitude], {
      radius: person.mobilityRange * 1000, // Convert km to meters
      color:
        person.age >= 70 ? "#ff69b4" : person.age <= 12 ? "#32cd32" : "#4169e1",
      fillColor:
        person.age >= 70 ? "#ff69b4" : person.age <= 12 ? "#32cd32" : "#4169e1",
      fillOpacity: 0.1,
      weight: 1,
      dashArray: "3, 5",
    });

    visualizer.pathLines.addLayer(circle);
  }
}

/**
 * Generate data for mixed elderly and limited mobility scenario
 */
function generateMixedElderlyLimitedRangeData(requestData) {
  const random = new Random(Date.now());

  // Generate people with high elderly percentage
  const people = [];
  for (let i = 0; i < requestData.peopleCount; i++) {
    // Generate age with higher percentage of elderly
    let age;
    const ageRandom = random.nextDouble();

    if (ageRandom < 0.4) {
      // 40% elderly
      age = random.nextInt(70, 95);
    } else if (ageRandom < 0.6) {
      // 20% children
      age = random.nextInt(1, 19);
    } else {
      // 40% adults
      age = random.nextInt(19, 70);
    }

    // Generate random location
    const angle = random.nextDouble() * 2 * Math.PI;
    const distance = (random.nextDouble() * requestData.radiusKm) / 111.0;

    const lat = requestData.centerLatitude + distance * Math.cos(angle);
    const lon = requestData.centerLongitude + distance * Math.sin(angle);

    // Determine mobility range based on age
    let mobilityRange;
    if (age >= 70) {
      // Elderly have shortest range
      mobilityRange = requestData.limitedMobility.elderlyRange;
    } else if (age <= 12) {
      // Children have medium range
      mobilityRange = requestData.limitedMobility.childrenRange;
    } else {
      // Adults have standard range
      mobilityRange = requestData.limitedMobility.adultRange;
    }

    people.push({
      id: i + 1,
      age: age,
      latitude: lat,
      longitude: lon,
      mobilityRange: mobilityRange,
    });
  }

  // Generate shelters - fewer and more scattered
  const shelters = [];

  // Known Beer Sheva locations
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
      capacity: random.nextInt(4, 9), // Larger capacity (4-8)
    });
  }

  // Add remaining random shelters
  for (let i = knownLocations.length; i < requestData.shelterCount; i++) {
    // Generate random location - more spread out
    const angle = random.nextDouble() * 2 * Math.PI;
    const distance =
      ((random.nextDouble() * 0.4 + 0.6) * requestData.radiusKm) / 111.0; // Minimum 60% of radius

    const lat = requestData.centerLatitude + distance * Math.cos(angle);
    const lon = requestData.centerLongitude + distance * Math.sin(angle);

    shelters.push({
      id: i + 1,
      name: `Shelter ${i + 1}`,
      latitude: lat,
      longitude: lon,
      capacity: random.nextInt(4, 9), // Larger capacity (4-8)
    });
  }

  // Custom assignment with mobility constraints
  const assignments = {};
  const shelterCapacity = {};

  // Initialize shelter capacities
  shelters.forEach((shelter) => {
    shelterCapacity[shelter.id] = shelter.capacity;
  });

  // First, create a list of all possible assignments within mobility range
  const possibleAssignments = [];

  for (const person of people) {
    // Use person's mobility range
    const maxRangeKm = person.mobilityRange;

    for (const shelter of shelters) {
      // Skip if shelter capacity is zero
      if (shelterCapacity[shelter.id] <= 0) continue;

      // Calculate distance
      const distance = calculateDistance(
        person.latitude,
        person.longitude,
        shelter.latitude,
        shelter.longitude
      );

      // Check if within this person's range
      if (distance <= maxRangeKm) {
        // Calculate vulnerability score
        const vulnerabilityScore = requestData.prioritySettings
          ?.enableAgePriority
          ? calculateVulnerabilityScore(person.age)
          : 0;

        possibleAssignments.push({
          personId: person.id,
          shelterId: shelter.id,
          distance: distance,
          vulnerabilityScore: vulnerabilityScore,
        });
      }
    }
  }

  // Sort by priority and distance
  possibleAssignments.sort((a, b) => {
    if (requestData.prioritySettings?.enableAgePriority) {
      if (a.vulnerabilityScore !== b.vulnerabilityScore) {
        return b.vulnerabilityScore - a.vulnerabilityScore;
      }
    }
    return a.distance - b.distance;
  });

  // Identify "critical" people with only one shelter option
  const personOptions = {};
  for (const assignment of possibleAssignments) {
    if (!personOptions[assignment.personId]) {
      personOptions[assignment.personId] = [];
    }
    personOptions[assignment.personId].push(assignment.shelterId);
  }

  const criticalPeople = [];
  for (const personId in personOptions) {
    if (personOptions[personId].length === 1) {
      criticalPeople.push(parseInt(personId));
    }
  }

  // First assign critical people
  const criticalAssignments = possibleAssignments.filter(
    (a) =>
      criticalPeople.includes(a.personId) && shelterCapacity[a.shelterId] > 0
  );

  for (const assignment of criticalAssignments) {
    // Make the assignment
    assignments[assignment.personId] = {
      personId: assignment.personId,
      shelterId: assignment.shelterId,
      distance: assignment.distance,
    };

    // Update shelter capacity
    shelterCapacity[assignment.shelterId]--;

    // Mark the person as critical
    const person = people.find((p) => p.id === assignment.personId);
    if (person) {
      person.critical = true;
    }
  }

  // Now assign the rest
  for (const assignment of possibleAssignments) {
    // Skip if person already assigned or shelter full
    if (
      assignments[assignment.personId] ||
      shelterCapacity[assignment.shelterId] <= 0
    ) {
      continue;
    }

    // Make the assignment
    assignments[assignment.personId] = {
      personId: assignment.personId,
      shelterId: assignment.shelterId,
      distance: assignment.distance,
    };

    // Update shelter capacity
    shelterCapacity[assignment.shelterId]--;
  }

  // Calculate statistics
  const stats = calculateStatistics(people, shelters, assignments);

  return {
    people: people,
    shelters: shelters,
    assignments: assignments,
    statistics: stats,
    criticalPeople: criticalPeople,
    mobilityRanges: true,
  };
}

// ===================================================
// UTILITY FUNCTIONS
// ===================================================

/**
 * Generate random people
 */
function generateRandomPeople(requestData, random) {
  const people = [];

  for (let i = 0; i < requestData.peopleCount; i++) {
    // Generate random location within the specified radius
    const angle = random.nextDouble() * 2 * Math.PI;
    const distance = (random.nextDouble() * requestData.radiusKm) / 111.0; // Convert to degrees

    const latOffset = distance * Math.cos(angle);
    const lonOffset = distance * Math.sin(angle);

    // Generate age with proper distribution - include children and elderly
    let age;
    const ageRandom = random.nextDouble();

    if (requestData.elderlyFocus) {
      // Modified distribution for elderly focus
      if (ageRandom < requestData.elderlyPercentage / 100) {
        age = random.nextInt(70, 95); // Elderly
      } else if (ageRandom < (requestData.elderlyPercentage + 15) / 100) {
        age = random.nextInt(1, 19); // Children (15%)
      } else {
        age = random.nextInt(19, 70); // Adults (remainder)
      }
    } else {
      // Standard distribution
      if (ageRandom < 0.15) {
        age = random.nextInt(1, 19); // Children (15%)
      } else if (ageRandom < 0.85) {
        age = random.nextInt(19, 70); // Adults (70%)
      } else {
        age = random.nextInt(70, 95); // Elderly (15%)
      }
    }

    people.push({
      id: i + 1,
      age: age,
      latitude: requestData.centerLatitude + latOffset,
      longitude: requestData.centerLongitude + lonOffset,
    });
  }

  return people;
}

/**
 * Generate random shelters
 */
function generateShelters(requestData, random) {
  const shelters = [];

  // Known Beer Sheva locations
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

  // Add remaining random shelters
  for (let i = knownLocations.length; i < requestData.shelterCount; i++) {
    // Generate random location
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
 * Assign people to shelters using standard algorithm
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
    // List of families ordered by vulnerability
    const prioritizedFamilies = [...families].sort((a, b) => {
      const aScore = calculateFamilyVulnerabilityScore(a);
      const bScore = calculateFamilyVulnerabilityScore(b);
      return bScore - aScore;
    });

    // For each family, find a shelter with enough capacity
    for (const family of prioritizedFamilies) {
      const familySize = family.members.length;

      // Find shelters with enough capacity
      const suitableShelters = shelters
        .filter((shelter) => shelterCapacity[shelter.id] >= familySize)
        .map((shelter) => {
          // Calculate average distance
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

      // If we found a suitable shelter, assign the family
      if (suitableShelters.length > 0) {
        const { shelter, avgDistance } = suitableShelters[0];

        // Assign all family members
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
  // Create list of possible assignments
  const possibleAssignments = [];

  for (const person of people) {
    // Skip if already assigned
    if (assignments[person.id]) continue;

    // Calculate vulnerability score
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

  // Sort by priority and distance
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
    // Skip if already assigned or shelter full
    if (
      assignments[assignment.personId] ||
      shelterCapacity[assignment.shelterId] <= 0
    ) {
      continue;
    }

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
 * Calculate statistics for the simulation
 */
function calculateStatistics(people, shelters, assignments) {
  const assignedCount = Object.keys(assignments).length;
  let totalDistance = 0;
  let maxDistance = 0;
  let minDistance = Infinity;

  // Calculate distance statistics
  for (const personId in assignments) {
    const distance = assignments[personId].distance;
    totalDistance += distance;

    if (distance > maxDistance) {
      maxDistance = distance;
    }

    if (distance < minDistance) {
      minDistance = distance;
    }
  }

  // Calculate average distance
  const averageDistance = assignedCount > 0 ? totalDistance / assignedCount : 0;

  // Return statistics object
  return {
    executionTimeMs: 0,
    assignedCount: assignedCount,
    unassignedCount: people.length - assignedCount,
    assignmentPercentage: people.length > 0 ? assignedCount / people.length : 0,
    totalShelterCapacity: shelters.reduce((sum, s) => sum + s.capacity, 0),
    averageDistance: averageDistance,
    maxDistance: maxDistance,
    minDistance: minDistance > 0 ? minDistance : 0,
  };
}

/**
 * Calculate a family's vulnerability score
 */
function calculateFamilyVulnerabilityScore(family) {
  let score = 0;
  for (const member of family.members) {
    score += calculateVulnerabilityScore(member.age);
  }
  return score / family.members.length;
}

/**
 * Calculate individual vulnerability score based on age
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
 * Calculate distance between two points using Haversine formula
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
 * Update the status message
 */
function updateStatusMessage(message) {
  const statusElement = document.getElementById("simulation-status");
  if (statusElement) {
    statusElement.textContent = message;
    statusElement.className = "status-message running";
  }
}

/**
 * Visualize data with animation effect
 */
function visualizeWithAnimation(data, visualizer) {
  if (!window.animateAssignments) {
    // Standard visualization if animation is disabled
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Highlight critical people if needed
    if (window.highlightCritical && data.criticalPeople) {
      highlightCriticalPeople(data.criticalPeople, visualizer);
    }

    return;
  }

  // With animation: first show just people and shelters
  visualizer.visualizeSimulation(
    data.people,
    data.shelters,
    {} // Empty assignments
  );

  // Then gradually add assignments with a delay
  const totalDelay = 1500; // Total animation time in ms
  const assignments = data.assignments;
  const assignmentIds = Object.keys(assignments);

  // Check if we have a lot of assignments
  if (assignmentIds.length > 100) {
    // Too many assignments for smooth animation, do it in batches
    const batchSize = 20;
    const batches = Math.ceil(assignmentIds.length / batchSize);
    const batchDelay = totalDelay / batches;

    for (let i = 0; i < batches; i++) {
      setTimeout(() => {
        const start = i * batchSize;
        const end = Math.min(start + batchSize, assignmentIds.length);
        const batchAssignments = {};

        for (let j = start; j < end; j++) {
          const id = assignmentIds[j];
          batchAssignments[id] = assignments[id];
        }

        // Add this batch of assignments
        visualizer.visualizeSimulation(data.people, data.shelters, {
          ...data.assignments.slice(0, end),
        });
      }, i * batchDelay);
    }
  } else {
    // Animate each assignment individually
    const delay = totalDelay / assignmentIds.length;

    const currentAssignments = {};
    assignmentIds.forEach((id, index) => {
      setTimeout(() => {
        currentAssignments[id] = assignments[id];

        visualizer.visualizeSimulation(
          data.people,
          data.shelters,
          currentAssignments
        );

        // Highlight critical people after all assignments are done
        if (
          index === assignmentIds.length - 1 &&
          window.highlightCritical &&
          data.criticalPeople
        ) {
          setTimeout(() => {
            highlightCriticalPeople(data.criticalPeople, visualizer);
          }, 500);
        }
      }, index * delay);
    });
  }
}

/**
 * Highlight critical people on the map
 */
function highlightCriticalPeople(criticalPeople, visualizer) {
  if (!criticalPeople || !visualizer) return;

  // Find and highlight critical people on the map
  visualizer.peopleMarkers.eachLayer((layer) => {
    const personId = layer.options.personId;
    if (!personId) return;

    if (Array.isArray(criticalPeople) && criticalPeople.includes(personId)) {
      // Add a special class to the marker
      const icon = layer.getIcon();
      const iconHtml = icon.options.html;

      // Create a new icon with the critical-person class
      const newIcon = L.divIcon({
        className: icon.options.className + " critical-person",
        html: iconHtml,
        iconSize: icon.options.iconSize,
        iconAnchor: icon.options.iconAnchor,
      });

      // Update the marker with the new icon
      layer.setIcon(newIcon);

      // Bring to front
      if (layer.bringToFront) {
        layer.bringToFront();
      }

      // Add a permanent tooltip
      layer.bindTooltip("Critical - Limited Options", {
        permanent: true,
        direction: "top",
        className: "critical-tooltip",
      });
    }
  });
}

/**
 * Reset highlighted critical people
 */
function resetHighlights() {
  const visualizer = window.visualizer || initializeVisualizer();
  if (!visualizer) return;

  visualizer.peopleMarkers.eachLayer((layer) => {
    // Remove any tooltips
    if (layer.getTooltip()) {
      layer.unbindTooltip();
    }

    // Reset icon if it was a critical person
    if (
      layer.options.className &&
      layer.options.className.includes("critical-person")
    ) {
      // Find out what kind of person this was
      const person = people.find((p) => p.id === layer.options.personId);
      if (person) {
        let icon;
        if (person.age >= 70) {
          icon = visualizer.icons.elderly;
        } else if (person.age <= 12) {
          icon = visualizer.icons.child;
        } else {
          icon = visualizer.icons.person;
        }

        layer.setIcon(icon);
      }
    }
  });
}

// Initialize the advanced extreme scenarios when the page loads
document.addEventListener("DOMContentLoaded", function () {
  // Add a small delay to ensure the regular extreme scenarios are loaded first
  setTimeout(() => {
    addAdvancedExtremeScenarios();
  }, 2000);
});
