console.log("Using port:", PORT);

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
    const useDatabaseShelters =
      document.getElementById("use-database-shelters")?.checked || false;

    const requestData = {
      peopleCount: peopleCount,
      shelterCount: shelterCount,
      centerLatitude: 31.2518,
      centerLongitude: 34.7913,
      radiusKm: radius,
      prioritySettings: {
        enableAgePriority: priorityEnabled,
        childMaxAge: 12,
        elderlyMinAge: 70,
      },
      useDatabaseShelters: useDatabaseShelters,
    };

    const response = await fetch(
      `https://localhost:${PORT}/api/Simulation/run`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(requestData),
      }
    );

    if (!response.ok) {
      throw new Error(
        `Server responded with ${response.status}: ${response.statusText}`
      );
    }

    const data = await response.json();

    // Clear existing data
    visualizer.clearMap();

    // Save original data for future reference
    visualizer.originalSimulationData = {
      people: [...data.people],
      shelters: [...data.shelters],
      assignments: { ...data.assignments },
    };

    // Set as current simulation data
    visualizer.currentSimulationData = data;

    // Display results
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Update age group statistics
    if (visualizer.updateAgeGroupStatistics) {
      visualizer.updateAgeGroupStatistics(data.people, data.assignments);
    }

    // Reset button states
    disableActiveModes();
    updateManualButtonState();

    if (statusElement) {
      statusElement.textContent = "Simulation complete";
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
 * Updates the statistics panel with simulation results
 */
function updateStatistics(stats, people, assignments) {
  if (!stats) return;

  // Update basic statistics
  const elements = {
    "stats-total": people.length,
    "stats-assigned": stats.assignedCount,
    "stats-unassigned": stats.unassignedCount,
    "stats-avg-distance": stats.averageDistance.toFixed(2),
    "stats-max-distance": stats.maxDistance.toFixed(2),
    "stats-total-capacity": stats.totalShelterCapacity,
    "stats-shelter-usage": stats.shelterUsagePercentage.toFixed(1),
  };

  Object.entries(elements).forEach(([id, value]) => {
    const element = document.getElementById(id);
    if (element) element.textContent = value;
  });

  // Update age group statistics
  updateAgeGroupStats(people, stats, assignments);
}

/**
 * Updates age group statistics in the UI
 */
function updateAgeGroupStats(people, stats, assignments) {
  // Use the visualizer's method if available
  if (window.visualizer?.updateAgeGroupStatistics) {
    window.visualizer.updateAgeGroupStatistics(people, assignments);
    return;
  }

  // Fallback implementation
  const ageStatsContainer = document.getElementById("age-stats-container");
  if (!ageStatsContainer) return;

  const ageGroups = {
    assigned: { children: 0, adults: 0, elderly: 0 },
    unassigned: { children: 0, adults: 0, elderly: 0 },
    total: { children: 0, adults: 0, elderly: 0 },
  };

  people.forEach((person) => {
    const isAssigned = assignments && assignments[person.id];
    const ageCategory =
      person.age >= 70 ? "elderly" : person.age <= 12 ? "children" : "adults";

    ageGroups.total[ageCategory]++;
    if (isAssigned) {
      ageGroups.assigned[ageCategory]++;
    } else {
      ageGroups.unassigned[ageCategory]++;
    }
  });

  // Create table
  ageStatsContainer.innerHTML = "";
  const table = document.createElement("table");
  table.className = "age-stats-table";

  // Headers
  const headerRow = document.createElement("tr");
  ["", "Assigned", "Unassigned", "% Assigned"].forEach((header) => {
    const th = document.createElement("th");
    th.textContent = header;
    headerRow.appendChild(th);
  });
  table.appendChild(headerRow);

  // Data rows
  ["children", "adults", "elderly"].forEach((category) => {
    const row = document.createElement("tr");

    const nameCell = document.createElement("td");
    nameCell.textContent = category.charAt(0).toUpperCase() + category.slice(1);
    row.appendChild(nameCell);

    const assignedCell = document.createElement("td");
    assignedCell.textContent = ageGroups.assigned[category];
    row.appendChild(assignedCell);

    const unassignedCell = document.createElement("td");
    unassignedCell.textContent = ageGroups.unassigned[category];
    row.appendChild(unassignedCell);

    const percentCell = document.createElement("td");
    const total = ageGroups.total[category];
    const assigned = ageGroups.assigned[category];
    const percent = total > 0 ? Math.round((assigned / total) * 100) : 0;
    percentCell.textContent = `${percent}%`;

    if (percent === 100) {
      percentCell.style.color = "green";
      percentCell.style.fontWeight = "bold";
    } else if (percent < 50) {
      percentCell.style.color = "red";
    } else {
      percentCell.style.color = "orange";
    }

    row.appendChild(percentCell);
    table.appendChild(row);
  });

  ageStatsContainer.appendChild(table);
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
  // Run initial simulation button
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

  // Manual people placement toggle
  const enableButton = document.getElementById("enable-placement");
  if (enableButton) {
    enableButton.addEventListener("click", function () {
      const isActive = this.classList.toggle("active");

      // Disable removal mode if it's active
      if (isActive) {
        const removalButton = document.getElementById("enable-removal");
        if (removalButton?.classList.contains("active")) {
          removalButton.classList.remove("active");
          removalButton.textContent = "Remove People (Manual & Auto)";
          if (window.visualizer) {
            window.visualizer.enableUniversalRemoval(false);
          }
        }

        this.textContent = "Stop Placing People";
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

  // Run with manual people - KEEP THIS WORKING
  const runManualButton = document.getElementById("run-with-manual");
  if (runManualButton) {
    runManualButton.addEventListener("click", function () {
      if (window.visualizer) {
        // Disable active modes first
        disableActiveModes();
        // Run the unified simulation update
        window.visualizer.runUnifiedSimulationUpdate();
      }
    });
  }

  // Clear manual people
  const clearButton = document.getElementById("clear-manual");
  if (clearButton) {
    clearButton.addEventListener("click", function () {
      if (window.visualizer) {
        window.visualizer.clearManualPeople();
        updateManualButtonState();
      }
    });
  }

  function updateManualButtonState() {
    const manualButton = document.getElementById("run-with-manual");
    if (!manualButton) return;

    const manualCount = window.visualizer?.manualPeople?.length || 0;

    if (manualCount > 0) {
      manualButton.textContent = `Run With Manual People (${manualCount})`;
      manualButton.disabled = false;
    } else {
      manualButton.textContent = "Run With Manual People (0)";
      manualButton.disabled = true;
    }
  }

  // People removal toggle
  const removalButton = document.getElementById("enable-removal");
  if (removalButton) {
    removalButton.addEventListener("click", function () {
      const isActive = this.classList.toggle("active");

      // Disable placement mode if it's active
      if (isActive) {
        const placementButton = document.getElementById("enable-placement");
        if (placementButton?.classList.contains("active")) {
          placementButton.classList.remove("active");
          placementButton.textContent = "Place People Manually";
          if (window.visualizer) {
            window.visualizer.enableManualPlacement(false);
          }
        }
      }

      this.textContent = isActive
        ? "Stop Removing People"
        : "Remove People (Manual & Auto)";

      if (window.visualizer) {
        window.visualizer.enableUniversalRemoval(isActive);
      }
    });
  }

  // Run after removal - KEEP THIS WORKING
  const runAfterRemovalButton = document.getElementById("run-after-removal");
  if (runAfterRemovalButton) {
    runAfterRemovalButton.addEventListener("click", function () {
      if (window.visualizer) {
        // Disable active modes first
        disableActiveModes();
        // Run the unified simulation update
        window.visualizer.runUnifiedSimulationUpdate();
      }
    });
  }
}

function disableActiveModes() {
  const placementButton = document.getElementById("enable-placement");
  if (placementButton?.classList.contains("active")) {
    placementButton.classList.remove("active");
    placementButton.textContent = "Place People Manually";
    if (window.visualizer) {
      window.visualizer.enableManualPlacement(false);
    }
  }

  const removalButton = document.getElementById("enable-removal");
  if (removalButton?.classList.contains("active")) {
    removalButton.classList.remove("active");
    removalButton.textContent = "Remove People (Manual & Auto)";
    if (window.visualizer) {
      window.visualizer.enableUniversalRemoval(false);
    }
  }
}

/**
 * Enables or disables the removal of manually placed people from the map
 * When enabled, clicking on a manually added person will remove them
 *
 * @param {boolean} enable - Whether to enable removal mode
 */
function enableManualPeopleRemoval(enable) {
  const statusElement = document.getElementById("simulation-status");
  const removalButton = document.getElementById("enable-removal");

  if (window.visualizer) {
    window.visualizer.enableManualRemoval(enable);

    if (enable) {
      if (statusElement) {
        statusElement.textContent =
          "Click on manually added people to remove them";
        statusElement.className = "status-message running";
      }

      if (removalButton) {
        removalButton.textContent = "Stop Removing People";
        removalButton.classList.add("active");
      }
    } else {
      if (statusElement) {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }

      if (removalButton) {
        removalButton.textContent = "Remove People Manually";
        removalButton.classList.remove("active");
      }
    }
  }
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
