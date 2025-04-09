// This version supports the new layout with controls outside the map

// Ensure we only have one global instance of the visualizer
let globalVisualizer = null;

/**
 * Initialize the simulation visualizer
 * This function ensures only one instance exists
 */
function initializeVisualizer() {
  // Check if we already have an instance
  if (globalVisualizer) {
    console.log("Visualizer already exists, reusing instance");
    return globalVisualizer;
  }

  // Clean up any existing map elements
  const mapContainer = document.getElementById("map");
  if (mapContainer) {
    // If the map container already has a Leaflet instance, attempt to clean it up
    if (mapContainer._leaflet_id) {
      console.log("Cleaning up existing map");
      // Try to remove any existing map instances
      try {
        const existingMap = mapContainer._leaflet;
        if (existingMap && typeof existingMap.remove === "function") {
          existingMap.remove();
        }
      } catch (e) {
        console.warn("Error cleaning up existing map:", e);
      }

      // Reset Leaflet properties
      mapContainer._leaflet = null;
      mapContainer._leaflet_id = null;
    }

    // Also remove any child elements to ensure a clean start
    while (mapContainer.firstChild) {
      mapContainer.removeChild(mapContainer.firstChild);
    }
  }

  // Created new visualizer instance for the map
  // No need to add UI controls since we now have them outside the map
  globalVisualizer = new ShelterSimulationVisualizer("map", false);
  return globalVisualizer;
}

/**
 * Function to handle server-side simulation
 * Calls a remote API to run the simulation and visualizes the results
 */
async function runServerSimulation() {
  // Ensure we have a valid visualizer
  const visualizer = window.visualizer || initializeVisualizer();

  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Running simulation...";
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
    };

    // Call the server API
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

    // Clear any existing data
    visualizer.clearMap();

    // Save as original data for future reference
    visualizer.originalSimulationData = {
      people: [...data.people],
      shelters: [...data.shelters],
      assignments: { ...data.assignments },
    };

    // Display the results on the map
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Update statistics manually (we no longer rely on Leaflet controls)
    updateStatistics(data.statistics, data.people);

    // Update status message
    if (statusElement) {
      statusElement.textContent = "Simulation complete";
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
 * Updates the statistics panel with simulation results
 */
function updateStatistics(stats, people) {
  if (!stats) return;

  // Update basic statistics
  const statsTotal = document.getElementById("stats-total");
  const statsAssigned = document.getElementById("stats-assigned");
  const statsUnassigned = document.getElementById("stats-unassigned");
  const statsAvgDistance = document.getElementById("stats-avg-distance");
  const statsMaxDistance = document.getElementById("stats-max-distance");

  if (statsTotal) statsTotal.textContent = people.length;
  if (statsAssigned) statsAssigned.textContent = stats.assignedCount;
  if (statsUnassigned) statsUnassigned.textContent = stats.unassignedCount;
  if (statsAvgDistance)
    statsAvgDistance.textContent = stats.averageDistance.toFixed(2);
  if (statsMaxDistance)
    statsMaxDistance.textContent = stats.maxDistance.toFixed(2);

  // Update age group statistics (if needed)
  updateAgeGroupStats(people, stats);
}

/**
 * Updates age group statistics in the UI
 */
function updateAgeGroupStats(people, stats) {
  const ageStatsContainer = document.getElementById("age-stats-container");
  if (!ageStatsContainer) return;

  // Calculate age groups
  const assigned = { children: 0, adults: 0, elderly: 0 };
  const unassigned = { children: 0, adults: 0, elderly: 0 };

  // We would need assignments data for this
  // Simple placeholder for now
  ageStatsContainer.innerHTML = `
    <table class="age-stats-table">
      <tr>
        <th>Age Group</th>
        <th>Assigned</th>
        <th>Unassigned</th>
      </tr>
      <tr>
        <td>Children</td>
        <td>${stats.assignedChildren || "N/A"}</td>
        <td>${stats.unassignedChildren || "N/A"}</td>
      </tr>
      <tr>
        <td>Adults</td>
        <td>${stats.assignedAdults || "N/A"}</td>
        <td>${stats.unassignedAdults || "N/A"}</td>
      </tr>
      <tr>
        <td>Elderly</td>
        <td>${stats.assignedElderly || "N/A"}</td>
        <td>${stats.unassignedElderly || "N/A"}</td>
      </tr>
    </table>
  `;
}

/**
 * Initialization code for the application
 * Creates the visualizer instance and sets up the application when the DOM is ready
 */
document.addEventListener("DOMContentLoaded", function () {
  // Initialize the map
  window.visualizer = initializeVisualizer();

  // Set up event handlers for UI controls
  setupEventHandlers();

  console.log("FindCover application initialized");
});

/**
 * Set up event handlers for all UI controls
 */
function setupEventHandlers() {
  // Run simulation button
  const runButton = document.getElementById("run-simulation");
  if (runButton) {
    runButton.addEventListener("click", function () {
      // Clear manual people first
      if (window.visualizer) {
        window.visualizer.clearManualPeople();
      }
      runServerSimulation();
    });
  }

  // Manual people placement
  const enableButton = document.getElementById("enable-placement");
  if (enableButton) {
    enableButton.addEventListener("click", function () {
      const isActive = this.classList.toggle("active");

      if (isActive) {
        this.textContent = "Placing People (Click Map)";
        if (window.visualizer) {
          window.visualizer.enableManualPlacement(true);
        }
      } else {
        this.textContent = "Place People Manually";
        if (window.visualizer) {
          window.visualizer.enableManualPlacement(false);
        }
      }
    });
  }

  // Run with manual people
  const runManualButton = document.getElementById("run-with-manual");
  if (runManualButton) {
    runManualButton.addEventListener("click", function () {
      if (window.visualizer) {
        window.visualizer.runWithManualPeople();
      }
    });
  }

  // Clear manual people
  const clearButton = document.getElementById("clear-manual");
  if (clearButton) {
    clearButton.addEventListener("click", function () {
      if (window.visualizer) {
        window.visualizer.clearManualPeople();

        // Reset the run with manual button
        const manualButton = document.getElementById("run-with-manual");
        if (manualButton) {
          manualButton.textContent = "Run With Manual People (0)";
          manualButton.disabled = true;
        }
      }
    });
  }

  // Initialize extreme scenarios if available
  setTimeout(() => {
    if (typeof addExtremeScenarioControls === "function") {
      addExtremeScenarioControls();
    }
  }, 500);
}

// Expose to global scope for debugging
window.debugFindCover = function () {
  console.log("=== DEBUG INFO ===");
  console.log(
    "Visualizer defined:",
    typeof ShelterSimulationVisualizer !== "undefined"
  );
  console.log("Visualizer instance:", window.visualizer);
  console.log(
    "Current simulation data:",
    window.visualizer?.currentSimulationData
  );
};
