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

    // Clear existing data including manual people
    visualizer.clearMap();
    visualizer.clearManualPeople();

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
 * Update the manual button state
 */
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

/**
 * Disable all active modes (placement and removal)
 */
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

  // Run simulation with walking distances button
  const walkingButton = document.getElementById("run-simulation-walking");
  if (walkingButton) {
    walkingButton.addEventListener("click", function () {
      if (typeof runServerSimulationWithWalkingDistances === "function") {
        runServerSimulationWithWalkingDistances();
      } else {
        console.error("runServerSimulationWithWalkingDistances is not defined");
      }
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

  // Run with manual people
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

  // Run after removal
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

  // Set up distance-based placement checkbox handler
  const distanceCheckbox = document.getElementById("enable-distance-mode");
  const distanceOptions = document.getElementById("distance-options");

  if (distanceCheckbox) {
    distanceCheckbox.addEventListener("change", function () {
      if (distanceOptions) {
        distanceOptions.style.display = this.checked ? "block" : "none";
      }
    });
  }

  // NEW: Override the updateManualControlStatus to handle both buttons
  if (window.visualizer) {
    const originalUpdateStatus = window.visualizer.updateManualControlStatus;
    window.visualizer.updateManualControlStatus = function () {
      // Call original function first
      if (originalUpdateStatus) {
        originalUpdateStatus.call(this);
      }

      // Update walking distance button
      const walkingBtn = document.getElementById("run-manual-walking");
      if (!walkingBtn) return;

      const manualCount = this.manualPeople?.length || 0;

      if (manualCount > 0) {
        walkingBtn.textContent = `Run With Manual (Walking Distance) (${manualCount})`;
        walkingBtn.disabled = false;
      } else {
        walkingBtn.textContent = "Run With Manual (Walking Distance) (0)";
        walkingBtn.disabled = true;
      }
    };
  }
}

/**
 * Run simulation with manual people using Google Maps walking distances
 */
async function runManualSimulationWithWalkingDistances() {
  const statusElement = document.getElementById("simulation-status");
  const button = document.getElementById("run-manual-walking");

  if (button) {
    button.classList.add("loading");
    button.disabled = true;
  }

  if (statusElement) {
    statusElement.textContent =
      "Running manual simulation with walking distances...";
    statusElement.className = "status-message running";
  }

  try {
    const priorityEnabled =
      document.getElementById("priority").value === "true";
    const radius = parseFloat(document.getElementById("radius").value) || 0.5;
    const useDatabaseShelters =
      document.getElementById("use-database-shelters")?.checked || false;

    const allCurrentPeople = [];
    const seenIds = new Set();

    window.visualizer.peopleMarkers.eachLayer((marker) => {
      if (marker.getLatLng && marker.options && marker.options.personId) {
        const personId = marker.options.personId;

        if (seenIds.has(personId)) return;
        seenIds.add(personId);

        let personData = null;

        if (window.visualizer.currentSimulationData?.people) {
          personData = window.visualizer.currentSimulationData.people.find(
            (p) => p.id == personId
          );
        }

        if (!personData && marker.options.isManual) {
          personData = window.visualizer.manualPeople.find(
            (p) => p.id == personId
          );
        }

        if (personData) {
          allCurrentPeople.push({
            id: personData.id,
            age: personData.age,
            latitude: marker.getLatLng().lat,
            longitude: marker.getLatLng().lng,
            isManual: marker.options.isManual === true,
          });
        }
      }
    });

    const currentShelters =
      window.visualizer.currentSimulationData?.shelters || [];

    const requestData = {
      peopleCount: 0,
      shelterCount: currentShelters.length === 0 ? 10 : 0,
      centerLatitude: 31.2518,
      centerLongitude: 34.7913,
      radiusKm: radius,
      prioritySettings: {
        enableAgePriority: priorityEnabled,
        childMaxAge: 12,
        elderlyMinAge: 70,
      },
      useCustomPeople: true,
      customPeople: allCurrentPeople,
      useCustomShelters: currentShelters.length > 0,
      customShelters: currentShelters.length > 0 ? currentShelters : undefined,
      useDatabaseShelters: useDatabaseShelters,
    };

    const response = await fetch(
      `https://localhost:${PORT}/api/Simulation/run-with-walking-distances`,
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

    window.visualizer.clearMap();

    const newManualPeople = [];
    data.people.forEach((person) => {
      if (
        person.isManual ||
        window.visualizer.manualPeople.some((mp) => mp.id == person.id)
      ) {
        newManualPeople.push(person);
      }
    });
    window.visualizer.manualPeople = newManualPeople;

    window.visualizer.currentSimulationData = data;
    window.visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    window.visualizer.updateManualPeopleList();
    window.visualizer.updateManualControlStatus();

    updateStatistics(data.statistics, data.people, data.assignments);

    if (statusElement) {
      statusElement.textContent =
        "Manual simulation with walking distances complete";
      statusElement.className = "status-message success";

      setTimeout(() => {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }, 3000);
    }
  } catch (error) {
    console.error(
      "Error running manual simulation with walking distances:",
      error
    );
    if (statusElement) {
      statusElement.textContent = `Error: ${error.message}`;
      statusElement.className = "status-message error";
    }
  } finally {
    if (button) {
      button.classList.remove("loading");
      button.disabled = window.visualizer?.manualPeople?.length === 0;
    }
  }
}

/**
 * Enhance the setup event handlers with Google Maps walking distance features
 */
function enhanceSetupEventHandlers() {
  // Check if button already exists to prevent duplicates
  if (document.getElementById("run-manual-walking")) {
    console.log("Walking distance button already exists, skipping creation");
    return;
  }

  // Create the new button
  const manualWalkingButton = document.createElement("button");
  manualWalkingButton.id = "run-manual-walking";
  manualWalkingButton.className = "control-button";
  manualWalkingButton.textContent = "Run With Manual (Walking Distance)";
  manualWalkingButton.disabled = true;
  manualWalkingButton.style.backgroundColor = "#2d4d3a";
  manualWalkingButton.style.marginTop = "5px";

  // Insert it after the regular manual button
  const runManualButton = document.getElementById("run-with-manual");
  if (runManualButton && runManualButton.parentNode) {
    runManualButton.parentNode.insertBefore(
      manualWalkingButton,
      runManualButton.nextSibling
    );
  }

  // Add event handler
  manualWalkingButton.addEventListener("click", function () {
    if (window.visualizer) {
      disableActiveModes();
      runManualSimulationWithWalkingDistances();
    }
  });

  // Update the manual control status function to handle both buttons
  if (window.visualizer) {
    const originalUpdateStatus = window.visualizer.updateManualControlStatus;
    window.visualizer.updateManualControlStatus = function () {
      originalUpdateStatus.call(this);

      const walkingButton = document.getElementById("run-manual-walking");
      if (!walkingButton) return;

      const manualCount = this.manualPeople?.length || 0;

      if (manualCount > 0) {
        walkingButton.textContent = `Run With Manual (Walking Distance) (${manualCount})`;
        walkingButton.disabled = false;
      } else {
        walkingButton.textContent = "Run With Manual (Walking Distance) (0)";
        walkingButton.disabled = true;
      }
    };

    window.visualizer.updateManualControlStatus();
  }
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

  // Add enhanced features after a short delay
  setTimeout(() => {
    enhanceSetupEventHandlers();
  }, 500);

  console.log("FindCover application initialized");
});

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
  console.log("Manual people:", window.visualizer?.manualPeople);
};
