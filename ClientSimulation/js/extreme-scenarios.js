/**
 * Extreme scenario handler functions for shelter simulation
 * These functions add testing capabilities for edge cases
 */

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
      id: "scenario-elderly",
      text: "Elderly Crisis (50% Elderly)",
      handler: elderlyScenario,
    },
    {
      id: "scenario-reset",
      text: "Reset to Default and Run Simulation",
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

  // Turn off database shelters
  const databaseSheltersCheckbox = document.getElementById(
    "use-database-shelters"
  );
  if (databaseSheltersCheckbox) {
    databaseSheltersCheckbox.checked = false;
  }

  // Run the simulation
  runServerSimulation();
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

  // Run a custom server simulation with a different age distribution
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

    // Generate custom people with high percentage of elderly
    const customPeople = [];

    // Generate 50% elderly, 20% children, 30% adults
    for (let i = 0; i < peopleCount; i++) {
      let age;
      // Simple deterministic distribution
      if (i < peopleCount * 0.5) {
        // 50% elderly (70-95)
        age = 70 + Math.floor(Math.random() * 25);
      } else if (i < peopleCount * 0.7) {
        // 20% children (1-12)
        age = 1 + Math.floor(Math.random() * 12);
      } else {
        // 30% adults (13-69)
        age = 13 + Math.floor(Math.random() * 57);
      }

      // Random location around center
      const angle = Math.random() * 2 * Math.PI;
      const distance = (Math.random() * radius) / 111.0; // Convert km to approximate degrees

      customPeople.push({
        id: i + 1,
        age: age,
        latitude: 31.2518 + distance * Math.cos(angle),
        longitude: 34.7913 + distance * Math.sin(angle),
      });
    }

    // Prepare request payload with custom people
    const requestData = {
      peopleCount: 0, // Set to 0 to use custom people instead
      shelterCount: shelterCount,
      centerLatitude: 31.2518, // Beer Sheva
      centerLongitude: 34.7913, // Beer Sheva
      radiusKm: radius,
      prioritySettings: {
        enableAgePriority: priorityEnabled,
        childMaxAge: 12,
        elderlyMinAge: 70,
      },
      useCustomPeople: true,
      customPeople: customPeople,
      useDatabaseShelters: false,
    };

    // Call the server API with our custom people
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

    // Handle error responses
    if (!response.ok) {
      throw new Error(
        `Server responded with ${response.status}: ${response.statusText}`
      );
    }

    // Parse the successful response
    const data = await response.json();

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

    // update statistics with the enhanced age group information:
    updateStatistics(data.statistics, data.people, data.assignments);

    // Count elderly percentage for status message
    const elderlyCount = data.people.filter((p) => p.age >= 70).length;
    const elderlyPercentage = Math.round(
      (elderlyCount / data.people.length) * 100
    );

    // Update status message
    if (statusElement) {
      statusElement.textContent = `Elderly crisis scenario running (${elderlyPercentage}% elderly)`;
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
 * Add the zero capacity shelters scenario function
 * Creates a simulation with a mix of regular and zero-capacity shelters
 */
function zeroCapacitySheltersScenario() {
  updateStatusMessage("Setting up zero capacity shelters scenario...");

  // Set higher population to make the challenge more significant
  const peopleCountInput = document.getElementById("people-count");
  if (peopleCountInput) {
    peopleCountInput.value = "200";
  }

  // Set many shelters (several will have zero capacity)
  const shelterCountInput = document.getElementById("shelter-count");
  if (shelterCountInput) {
    shelterCountInput.value = "15";
  }

  // Enable priority to see how it handles the zero capacity constraint
  const prioritySelect = document.getElementById("priority");
  if (prioritySelect) {
    prioritySelect.value = "true";
  }

  // Run custom simulation with zero capacity shelters
  runZeroCapacitySheltersSimulation();
}

/**
 * Add the zero capacity shelters simulation function
 * Uses a server-side simulation that includes shelters with zero capacity
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
      parseInt(document.getElementById("people-count").value) || 200;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 15;
    const radius = parseFloat(document.getElementById("radius").value) || 0.8;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

    // Create custom shelters - specifically include several with zero capacity
    const customShelters = [];

    // Generate shelters with some having zero capacity
    for (let i = 0; i < shelterCount; i++) {
      // Every third shelter should have zero capacity
      const isZeroCapacity = i % 3 === 0;
      const capacity = isZeroCapacity ? 0 : Math.floor(Math.random() * 5) + 3;

      // Create a shelter at a random location
      const angle = Math.random() * 2 * Math.PI;
      const distance = (Math.random() * radius * 0.7) / 111.0;
      const latOffset = distance * Math.cos(angle);
      const lonOffset = distance * Math.sin(angle);

      customShelters.push({
        id: i + 1,
        name: isZeroCapacity ? `Closed Shelter ${i + 1}` : `Shelter ${i + 1}`,
        latitude: 31.2518 + latOffset,
        longitude: 34.7913 + lonOffset,
        capacity: capacity,
      });
    }

    // Create a modified request with our custom shelters
    const requestData = {
      peopleCount: peopleCount,
      shelterCount: 0, // Don't generate shelters, we'll use our custom ones
      centerLatitude: 31.2518, // Beer Sheva
      centerLongitude: 34.7913, // Beer Sheva
      radiusKm: radius,
      prioritySettings: {
        enableAgePriority: priorityEnabled,
        childMaxAge: 12,
        elderlyMinAge: 70,
      },
      useCustomShelters: true,
      customShelters: customShelters,
    };

    // Call server simulation with our custom shelters that include true zero capacity
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

    //update statistics with the enhanced age group information:
    updateStatistics(data.statistics, data.people, data.assignments);

    // Update status with count of zero capacity shelters
    const zeroCapacityShelters = customShelters.filter(
      (s) => s.capacity === 0
    ).length;
    if (statusElement) {
      statusElement.textContent = `Zero capacity shelters scenario running (${zeroCapacityShelters} shelters with zero capacity)`;
      statusElement.className = "status-message success";

      setTimeout(() => {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }, 5000);
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
