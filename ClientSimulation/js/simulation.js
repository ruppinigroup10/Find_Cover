/**
 * ShelterSimulationVisualizer - Main class that handles the visualization of shelter assignments
 * This class creates and manages an interactive map for visualizing emergency shelter assignments
 * It displays the results of server-side simulations
 */
class ShelterSimulationVisualizer {
  /**
   * Constructor - initializes the map, layers, statistics, and UI controls
   * @param {string} mapElementId - The HTML element ID where the map will be rendered
   */
  constructor(mapElementId) {
    // Better approach: check if the map container element exists first
    const mapContainer = document.getElementById(mapElementId);
    if (!mapContainer) {
      console.error(`Map container with ID ${mapElementId} not found`);
      return;
    }

    // Clean approach: always create a new map instance, but first destroy any existing one
    if (mapContainer._leaflet_id) {
      console.log("Cleaning up existing map instance");
      // If there's an existing map in this container, remove it first
      mapContainer._leaflet = null;
      mapContainer._leaflet_id = null;
    }

    // Initialize the map centered on Beer Sheva
    this.map = L.map(mapElementId).setView([31.2518, 34.7913], 13); // Beer Sheva coordinates

    // Add OpenStreetMap as the base tile layer
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    // Create layer groups to organize map elements
    this.shelterMarkers = L.layerGroup().addTo(this.map);
    this.peopleMarkers = L.layerGroup().addTo(this.map);
    this.pathLines = L.layerGroup().addTo(this.map);

    // Initialize statistics object to track simulation results
    this.stats = {
      totalPeople: 0,
      assignedPeople: 0,
      unassignedPeople: 0,
      averageDistance: 0,
      maxDistance: 0,
      shelterUsage: [], // Will store usage statistics for each shelter
      ageGroups: {
        // Tracks age demographics of assigned/unassigned people
        assigned: { elderly: 0, children: 0, adults: 0 },
        unassigned: { elderly: 0, children: 0, adults: 0 },
      },
    };

    // Create custom map icons for different entities
    this.icons = this.createIcons();

    // Set up UI control panels
    this.addControlPanel();
  }

  /**
   * Calculates the "as the crow flies" distance between two points using the Haversine formula
   * This is much faster than street distance but less accurate for actual travel
   *
   * @param {number} lat1 - Latitude of the first point
   * @param {number} lon1 - Longitude of the first point
   * @param {number} lat2 - Latitude of the second point
   * @param {number} lon2 - Longitude of the second point
   * @returns {number} - Distance in kilometers
   */
  calculateAirDistance(lat1, lon1, lat2, lon2) {
    const R = 6371; // Earth radius in km
    const dLat = ((lat2 - lat1) * Math.PI) / 180; // Convert degree difference to radians
    const dLon = ((lon2 - lon1) * Math.PI) / 180;

    // Haversine formula for distance on a sphere
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos((lat1 * Math.PI) / 180) *
        Math.cos((lat2 * Math.PI) / 180) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c; // Distance in kilometers
  }

  /**
   * Creates custom map icons for different entity types
   * Uses div icons with color-coded dots to distinguish between different types
   * For shelters, the size is dynamically calculated based on capacity (1-5)
   *
   * @returns {Object} - Object containing Leaflet icon objects for each entity type
   */
  createIcons() {
    return {
      // Shelter icon - orange dot
      shelter: L.divIcon({
        className: "marker-shelter",
        html: '<div style="background-color: #ff4500; border-radius: 50%; width: 14px; height: 14px;"></div>',
        iconSize: [14, 14],
        iconAnchor: [7, 7],
      }),
      // Regular person icon - blue dot
      person: L.divIcon({
        className: "marker-person",
        html: '<div style="background-color: #4169e1; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      // Child icon - green dot
      child: L.divIcon({
        className: "marker-child",
        html: '<div style="background-color: #32cd32; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      // Elderly icon - pink dot
      elderly: L.divIcon({
        className: "marker-elderly",
        html: '<div style="background-color: #ff69b4; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      // Unassigned person icon - gray dot
      unassigned: L.divIcon({
        className: "marker-unassigned",
        html: '<div style="background-color: #808080; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),

      // For shelters, we'll create a function that returns a sized icon
      // This will be called dynamically for each shelter based on its capacity
      getShelterIcon: function (capacity) {
        // Ensure capacity is within range 1-5
        const cappedCapacity = Math.max(1, Math.min(5, capacity));

        // Define specific sizes for each capacity value (1-5)
        const sizeMap = {
          1: 14, // Smallest size
          2: 18,
          3: 22,
          4: 26,
          5: 30, // Largest size
        };

        // Define color shades for each capacity (light to dark red)
        const colorMap = {
          1: "#CC3333", // Light red
          2: "#CC0000",
          3: "#BB0000",
          4: "#990000",
          5: "#660000", // Darkest red
        };

        // Get the size and color based on the capacity
        const size = sizeMap[cappedCapacity];
        const color = colorMap[cappedCapacity];

        // Calculate the anchor (center point) based on size
        const anchor = size / 2;

        return L.divIcon({
          className: "marker-shelter",
          html: `<div style="background-color: ${color}; border-radius: 50%; width: ${size}px; height: ${size}px;"></div>`,
          iconSize: [size, size],
          iconAnchor: [anchor, anchor],
        });
      },
    };
  }

  /**
   * Clears all layers and resets statistics
   * Called before visualizing a new simulation
   */
  clearMap() {
    // Clear all markers and lines from the map
    this.shelterMarkers.clearLayers();
    this.peopleMarkers.clearLayers();
    this.pathLines.clearLayers();

    // Reset statistics to initial state
    this.stats = {
      totalPeople: 0,
      assignedPeople: 0,
      unassignedPeople: 0,
      averageDistance: 0,
      maxDistance: 0,
      shelterUsage: [],
      ageGroups: {
        assigned: { elderly: 0, children: 0, adults: 0 },
        unassigned: { elderly: 0, children: 0, adults: 0 },
      },
    };
  }

  /**
   * Main visualization method - displays simulation results on the map
   *
   * @param {Array} people - Array of person objects
   * @param {Array} shelters - Array of shelter objects
   * @param {Object} assignments - Object mapping person IDs to their shelter assignments
   */
  visualizeSimulation(people, shelters, assignments) {
    // Clear previous visualization
    this.clearMap();

    // Update basic statistics
    this.stats.totalPeople = people.length;
    this.stats.assignedPeople = Object.keys(assignments).length;
    this.stats.unassignedPeople =
      this.stats.totalPeople - this.stats.assignedPeople;

    // Reset age group statistics
    this.stats.ageGroups = {
      assigned: { elderly: 0, children: 0, adults: 0 },
      unassigned: { elderly: 0, children: 0, adults: 0 },
    };

    // Calculate map bounds to ensure all entities are visible
    const bounds = this.calculateBounds(people, shelters);
    this.map.fitBounds(bounds);

    // Display shelters on the map
    this.displayShelters(shelters);

    // Display people and their shelter assignments
    this.displayPeopleAndAssignments(people, shelters, assignments);

    // Update the statistics display in the UI
    this.updateStatisticsDisplay();
  }

  /**
   * Calculates the geographical bounds that contain all people and shelters
   * Used to set the map view to show all entities
   *
   * @param {Array} people - Array of person objects with coordinates
   * @param {Array} shelters - Array of shelter objects with coordinates
   * @returns {L.LatLngBounds} - Leaflet bounds object
   */
  calculateBounds(people, shelters) {
    // Combine all coordinates into a single array
    const allPoints = [
      ...people.map((p) => [p.latitude, p.longitude]),
      ...shelters.map((s) => [s.latitude, s.longitude]),
    ];

    // If we have data, calculate bounds from all points
    if (allPoints.length > 0) {
      return L.latLngBounds(allPoints);
    }

    // Default bounds centered on Tel Aviv if no data
    return L.latLngBounds([
      [32.0853 - 0.1, 34.7818 - 0.1],
      [32.0853 + 0.1, 34.7818 + 0.1],
    ]);
  }

  /**
   * Displays shelter markers on the map and initializes shelter usage statistics
   * Uses dynamically sized icons based on shelter capacity
   *
   * @param {Array} shelters - Array of shelter objects
   */
  displayShelters(shelters) {
    // Initialize shelter usage statistics for each shelter
    this.stats.shelterUsage = shelters.map((s) => ({
      id: s.id,
      name: s.name,
      capacity: s.capacity,
      assigned: 0, // Number of people assigned
      percentUsed: 0, // Percentage of capacity used
    }));

    // Add each shelter as a marker on the map
    shelters.forEach((shelter) => {
      // Get a dynamically sized icon based on this shelter's capacity
      const shelterIcon = this.icons.getShelterIcon(shelter.capacity);

      const marker = L.marker([shelter.latitude, shelter.longitude], {
        icon: shelterIcon,
      });

      // Add popup with shelter information
      marker.bindPopup(`
      <h3>${shelter.name}</h3>
      <p>Capacity: <span id="shelter-${shelter.id}-count">0</span>/${shelter.capacity}</p>
      <p>Status: <span id="shelter-${shelter.id}-status">Empty</span></p>
    `);

      // Add the marker to the shelter layer group
      this.shelterMarkers.addLayer(marker);
    });
  }

  /**
   * Displays people and their shelter assignments on the map
   * Draws paths between people and their assigned shelters
   * Updates statistics based on assignments
   *
   * @param {Array} people - Array of person objects
   * @param {Array} shelters - Array of shelter objects
   * @param {Object} assignments - Mapping of person IDs to shelter assignments
   */
  displayPeopleAndAssignments(people, shelters, assignments) {
    // Track total distance for average calculation
    let totalDistance = 0;
    this.stats.maxDistance = 0;

    // Process each person
    people.forEach((person) => {
      // Determine which icon to use based on age and assignment status
      let icon = this.icons.person; // Default icon

      // Handle unassigned people
      if (!assignments[person.id]) {
        icon = this.icons.unassigned; // Gray icon for unassigned

        // Update age group statistics for unassigned
        if (person.age >= 70) {
          this.stats.ageGroups.unassigned.elderly++;
        } else if (person.age <= 12) {
          this.stats.ageGroups.unassigned.children++;
        } else {
          this.stats.ageGroups.unassigned.adults++;
        }
      } else {
        // Handle assigned people - use age-specific icons
        if (person.age >= 70) {
          icon = this.icons.elderly;
          this.stats.ageGroups.assigned.elderly++;
        } else if (person.age <= 12) {
          icon = this.icons.child;
          this.stats.ageGroups.assigned.children++;
        } else {
          this.stats.ageGroups.assigned.adults++;
        }
      }

      // Create a marker for this person
      const marker = L.marker([person.latitude, person.longitude], { icon });

      // Check if the person has been assigned to a shelter
      if (assignments[person.id]) {
        const assignment = assignments[person.id];
        const shelter = shelters.find((s) => s.id === assignment.shelterId);

        if (shelter) {
          // Update shelter usage statistics
          const shelterStat = this.stats.shelterUsage.find(
            (s) => s.id === shelter.id
          );
          if (shelterStat) {
            shelterStat.assigned++;
            shelterStat.percentUsed =
              (shelterStat.assigned / shelter.capacity) * 100;
          }

          // Update DOM elements for this shelter's popup
          const countElement = document.getElementById(
            `shelter-${shelter.id}-count`
          );
          const statusElement = document.getElementById(
            `shelter-${shelter.id}-status`
          );

          if (countElement) {
            countElement.textContent = shelterStat.assigned.toString();
          }

          if (statusElement) {
            // Update status text and class based on usage
            if (shelterStat.assigned >= shelter.capacity) {
              statusElement.textContent = "Full";
              statusElement.className = "status-full";
            } else if (shelterStat.assigned >= shelter.capacity * 0.8) {
              statusElement.textContent = "Almost Full";
              statusElement.className = "status-almost-full";
            } else {
              statusElement.textContent = "Available";
              statusElement.className = "status-available";
            }
          }

          // Draw the path from person to shelter
          // Use the calculated route if available
          if (assignment.route && assignment.route.coordinates) {
            const routeLine = L.polyline(assignment.route.coordinates, {
              color: this.getLineColor(person.age),
              opacity: 0.7,
              weight: 3,
            });
            this.pathLines.addLayer(routeLine);
          } else {
            // Fallback to a straight line if no route is available
            const line = L.polyline(
              [
                [person.latitude, person.longitude],
                [shelter.latitude, shelter.longitude],
              ],
              { color: this.getLineColor(person.age), opacity: 0.7, weight: 2 }
            );
            this.pathLines.addLayer(line);
          }

          // Update distance statistics
          totalDistance += assignment.distance;
          if (assignment.distance > this.stats.maxDistance) {
            this.stats.maxDistance = assignment.distance;
          }

          // Add popup with assignment information
          marker.bindPopup(`
          <p>Person #${person.id}</p>
          <p>Age: ${person.age}</p>
          <p>Assigned to: ${shelter.name}</p>
          <p>Distance: ${assignment.distance.toFixed(2)} km</p>
        `);
        }
      } else {
        // Popup for unassigned person
        marker.bindPopup(`
        <p>Person #${person.id}</p>
        <p>Age: ${person.age}</p>
        <p>Status: <span class="status-unassigned">Unassigned</span></p>
      `);
      }

      // Add the marker to the people layer group
      this.peopleMarkers.addLayer(marker);
    });

    // Calculate average distance for assigned people
    if (this.stats.assignedPeople > 0) {
      this.stats.averageDistance = totalDistance / this.stats.assignedPeople;
    }
  }

  /**
   * Updates the statistics panel with current simulation data
   * Called after visualization to show results in the UI
   */
  updateStatisticsDisplay() {
    console.log("Updating statistics display with:", this.stats);

    // Update basic statistics
    const statsTotal = document.getElementById("stats-total");
    const statsAssigned = document.getElementById("stats-assigned");
    const statsUnassigned = document.getElementById("stats-unassigned");
    const statsAvgDistance = document.getElementById("stats-avg-distance");
    const statsMaxDistance = document.getElementById("stats-max-distance");

    if (statsTotal) statsTotal.textContent = this.stats.totalPeople;
    if (statsAssigned) statsAssigned.textContent = this.stats.assignedPeople;
    if (statsUnassigned)
      statsUnassigned.textContent = this.stats.unassignedPeople;
    if (statsAvgDistance)
      statsAvgDistance.textContent = this.stats.averageDistance.toFixed(2);
    if (statsMaxDistance)
      statsMaxDistance.textContent = this.stats.maxDistance.toFixed(2);

    // Update shelter usage statistics
    const shelterUsageContainer = document.getElementById(
      "shelter-usage-container"
    );
    if (shelterUsageContainer) {
      shelterUsageContainer.innerHTML = ""; // Clear existing content

      if (this.stats.shelterUsage.length === 0) {
        shelterUsageContainer.innerHTML = "<p>No shelter data available</p>";
      } else {
        // Sort by usage percentage (highest first)
        const sortedShelters = [...this.stats.shelterUsage]
          .filter((shelter) => shelter.capacity > 0) // Only show shelters with capacity
          .sort((a, b) => b.percentUsed - a.percentUsed);

        // Display statistics for each shelter (limit to top 5 for UI clarity)
        sortedShelters.slice(0, 5).forEach((shelter) => {
          const shelterDiv = document.createElement("div");
          shelterDiv.className = "shelter-usage-item";

          // Determine status class and color
          let statusClass = "status-available";
          let color = "green";

          if (shelter.percentUsed >= 100) {
            statusClass = "status-full";
            color = "red";
          } else if (shelter.percentUsed >= 80) {
            statusClass = "status-almost-full";
            color = "orange";
          }

          shelterDiv.innerHTML = `
          <div class="shelter-name">Shelter ${shelter.id}</div>
          <div class="shelter-stats" style="color: ${color}">
            ${shelter.assigned}/${shelter.capacity} (${Math.round(
            shelter.percentUsed
          )}%)
          </div>
        `;
          shelterUsageContainer.appendChild(shelterDiv);
        });
      }
    }

    // Update age group statistics
    const ageStatsContainer = document.getElementById("age-stats-container");
    if (ageStatsContainer) {
      const ageGroups = this.stats.ageGroups;
      const totalAssigned =
        ageGroups.assigned.elderly +
        ageGroups.assigned.children +
        ageGroups.assigned.adults;
      const totalUnassigned =
        ageGroups.unassigned.elderly +
        ageGroups.unassigned.children +
        ageGroups.unassigned.adults;

      ageStatsContainer.innerHTML = "";

      // Create a table for age group statistics
      const table = document.createElement("table");
      table.className = "age-stats-table";

      // Add table headers
      const headerRow = document.createElement("tr");
      ["", "Assigned", "Unassigned"].forEach((header) => {
        const th = document.createElement("th");
        th.textContent = header;
        headerRow.appendChild(th);
      });
      table.appendChild(headerRow);

      // Add data rows for each age group
      const ageCategories = [
        {
          name: "Children",
          assigned: ageGroups.assigned.children,
          unassigned: ageGroups.unassigned.children,
        },
        {
          name: "Adults",
          assigned: ageGroups.assigned.adults,
          unassigned: ageGroups.unassigned.adults,
        },
        {
          name: "Elderly",
          assigned: ageGroups.assigned.elderly,
          unassigned: ageGroups.unassigned.elderly,
        },
      ];

      ageCategories.forEach((category) => {
        const row = document.createElement("tr");

        // Add name cell
        const nameCell = document.createElement("td");
        nameCell.textContent = category.name;
        row.appendChild(nameCell);

        // Add assigned cell with percentage
        const assignedCell = document.createElement("td");
        const assignedPct =
          totalAssigned > 0
            ? Math.round((category.assigned / totalAssigned) * 100)
            : 0;
        assignedCell.textContent = `${category.assigned} (${assignedPct}%)`;
        row.appendChild(assignedCell);

        // Add unassigned cell with percentage
        const unassignedCell = document.createElement("td");
        const unassignedPct =
          totalUnassigned > 0
            ? Math.round((category.unassigned / totalUnassigned) * 100)
            : 0;
        unassignedCell.textContent = `${category.unassigned} (${unassignedPct}%)`;
        row.appendChild(unassignedCell);

        table.appendChild(row);
      });

      ageStatsContainer.appendChild(table);
    }
  }

  /**
   * Adds control panels to the map
   * Sets up UI elements for statistics and simulation controls
   */
  addControlPanel() {
    // Create both control panels
    this.addStatisticsPanel(); // Panel showing simulation results
    this.addSimulationControlPanel(); // Panel with input controls
  }

  /**
   * Adds a statistics panel to the top-right corner of the map
   * Displays summary statistics about the simulation
   */
  addStatisticsPanel() {
    // Create statistics panel HTML element
    const statsDiv = L.DomUtil.create(
      "div",
      "simulation-statistics leaflet-bar"
    );
    statsDiv.innerHTML = `
      <div class="stats-panel">
        <h3>Simulation Statistics</h3>
        <div id="stats-container">
          <p>Total people: <span id="stats-total">0</span></p>
          <p>Assigned: <span id="stats-assigned">0</span></p>
          <p>Unassigned: <span id="stats-unassigned">0</span></p>
          <p>Avg. distance: <span id="stats-avg-distance">0</span> km</p>
          <p>Max distance: <span id="stats-max-distance">0</span> km</p>
        </div>
        <div id="shelter-stats">
          <h4>Shelter Usage</h4>
          <div id="shelter-usage-container"></div>
        </div>
        <div id="age-stats">
          <h4>Assignment by Age Group</h4>
          <div id="age-stats-container"></div>
        </div>
      </div>
    `;

    // Create a custom Leaflet control for the statistics panel
    const StatsControl = L.Control.extend({
      options: {
        position: "topright", // Position in the top-right corner
      },
      onAdd: () => {
        return statsDiv;
      },
    });

    // Add the control to the map
    new StatsControl().addTo(this.map);
  }

  /**
   * Adds a simulation control panel to the top-left corner of the map
   * Provides UI controls for configuring and running simulations
   */
  addSimulationControlPanel() {
    // Create control panel HTML element
    const controlDiv = L.DomUtil.create(
      "div",
      "simulation-controls leaflet-bar"
    );
    controlDiv.innerHTML = `
    <div class="control-panel">
      <h3>Simulation Controls</h3>
      <div class="control-container">
        <div class="control-group">
          <label for="people-count">Number of People:</label>
          <input type="number" id="people-count" min="10" max="500" value="20">
        </div>
        <div class="control-group">
          <label for="shelter-count">Number of Shelters:</label>
          <input type="number" id="shelter-count" min="1" max="50" value="10">
        </div>
        <div class="control-group">
          <label for="radius">Simulation Radius (km):</label>
          <input type="number" id="radius" min="0.5" max="20" step="0.5" value="0.5">
        </div>
        <div class="control-group">
          <label for="priority">Priority Assignment:</label>
          <select id="priority">
            <option value="true" selected>Enabled (children & elderly first)</option>
            <option value="false">Disabled (distance only)</option>
          </select>
        </div>
        <button id="run-simulation" class="control-button">Run Simulation</button>
        <div id="simulation-status" class="status-message"></div>
      </div>
    </div>
  `;

    // Create a custom Leaflet control for the control panel
    const ControlPanelControl = L.Control.extend({
      options: {
        position: "topleft", // Position in the top-left corner
      },
      onAdd: () => {
        return controlDiv;
      },
    });

    // Add the control to the map
    new ControlPanelControl().addTo(this.map);

    // Add event listeners after a short delay to ensure DOM is ready
    setTimeout(() => {
      const runButton = document.getElementById("run-simulation");

      // Set up the run button to trigger the server simulation
      if (runButton) {
        runButton.addEventListener("click", () => {
          runServerSimulation(); // Call server simulation function
        });
      }
    }, 500); // Short delay to ensure DOM is ready
  }

  /**
   * Helper method to calculate percentage with safe division (avoids divide by zero)
   *
   * @param {number} part - The numerator (part of the total)
   * @param {number} total - The denominator (the total amount)
   * @returns {string} - Formatted percentage with one decimal place
   */
  calculatePercentage(part, total) {
    if (total === 0) return "0.0";
    return ((part / total) * 100).toFixed(1);
  }

  /**
   * Returns a color for the line based on the person's age
   * Used to color-code paths on the map
   *
   * @param {number} age - Age of the person
   * @returns {string} - CSS color string
   */
  getLineColor(age) {
    if (age < 12) {
      return "#32cd32"; // Green for children
    } else if (age >= 70) {
      return "#ff69b4"; // Pink for elderly
    } else {
      return "#4169e1"; // Blue for adults
    }
  }
}

// Export the class for use in other modules
if (typeof module !== "undefined" && module.exports) {
  module.exports = ShelterSimulationVisualizer;
}

/**
 * Function to handle server-side simulation
 * Calls a remote API to run the simulation and visualizes the results
 */
async function runServerSimulation() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Calling server simulation...";
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
    const response = await fetch("https://localhost:7094/api/Simulation/run", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(requestData),
    });

    // Handle error responses
    if (!response.ok) {
      throw new Error(
        `Server responded with ${response.status}: ${response.statusText}`
      );
    }

    // Parse the successful response
    const data = await response.json();

    // Display the results on the map using the visualizer
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Update statistics with server-provided data
    updateServerStatistics(data.statistics);

    // Update status message
    if (statusElement) {
      statusElement.textContent = "Server simulation complete";
      statusElement.className = "status-message success";

      // Clear the status after a few seconds
      setTimeout(() => {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }, 3000);
    }
  } catch (error) {
    // Handle errors
    console.error("Server simulation error:", error);
    if (statusElement) {
      statusElement.textContent = `Error: ${error.message}`;
      statusElement.className = "status-message error";
    }
  }
}

/**
 * Helper function to update statistics panel with server-provided data
 * Used when running server-side simulations
 *
 * @param {Object} stats - Statistics object from server response
 */
function updateServerStatistics(stats) {
  if (!stats) return; // Safety check

  // Update UI elements with statistics
  const statsTotal = document.getElementById("stats-total");
  const statsAssigned = document.getElementById("stats-assigned");
  const statsUnassigned = document.getElementById("stats-unassigned");
  const statsAvgDistance = document.getElementById("stats-avg-distance");
  const statsMaxDistance = document.getElementById("stats-max-distance");

  // Update the values in the DOM
  if (statsTotal)
    statsTotal.textContent = stats.assignedCount + stats.unassignedCount;
  if (statsAssigned) statsAssigned.textContent = stats.assignedCount;
  if (statsUnassigned) statsUnassigned.textContent = stats.unassignedCount;
  if (statsAvgDistance)
    statsAvgDistance.textContent = stats.averageDistance.toFixed(2);
  if (statsMaxDistance)
    statsMaxDistance.textContent = stats.maxDistance.toFixed(2);
}

/**
 * Initialization code for the application
 * Creates the visualizer instance and sets up the application when the DOM is ready
 */
document.addEventListener("DOMContentLoaded", function () {
  // Create a new ShelterSimulationVisualizer instance targeting the 'map' element
  window.visualizer = new ShelterSimulationVisualizer("map");

  console.log("Shelter simulation visualizer initialized");
});

// Export functions for use in other modules
if (typeof module !== "undefined" && module.exports) {
  module.exports = {
    runServerSimulation,
    updateServerStatistics,
  };
}
