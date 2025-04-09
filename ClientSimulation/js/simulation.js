/**
 * ShelterSimulationVisualizer - Main class that handles the visualization of shelter assignments
 * This class creates and manages an interactive map for visualizing emergency shelter assignments
 * It displays the results of server-side simulations
 */
class ShelterSimulationVisualizer {
  /**
   * Constructor - initializes the map, layers, statistics, and UI controls
   * @param {string} mapElementId - The HTML element ID where the map will be rendered
   * @param {boolean} addControls - Whether to add controls (now false by default)
   */
  constructor(mapElementId) {
    // Better approach: check if the map container element exists first
    const mapContainer = document.getElementById(mapElementId);
    if (!mapContainer) {
      console.error(`Map container with ID ${mapElementId} not found`);
      return;
    }

    // Make sure the map container has dimensions before initializing Leaflet
    if (mapContainer.offsetWidth === 0 || mapContainer.offsetHeight === 0) {
      console.warn("Map container has zero dimensions. Setting default size.");
      mapContainer.style.width = "100%";
      mapContainer.style.height = "500px";
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

    //old view
    // Set up UI control panels
    //this.addControlPanel();

    this.originalSimulationData = null; // store the base simulation without any manual people
    this.manualPeople = []; // store manual people
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
    // Save the current simulation data
    this.currentSimulationData = {
      people: people,
      shelters: shelters,
      assignments: assignments,
    };

    // If this is a regular simulation (not one with manual people added),
    // save it as the original data
    if (!people.some((p) => p.isManual)) {
      this.originalSimulationData = {
        people: [...people], // Create a copy
        shelters: [...shelters],
        assignments: { ...assignments },
      };
      console.log("Saved original simulation with", people.length, "people");
    }

    // Call the original implementation
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
    const controlContainer = controlDiv.querySelector(".control-container");

    // Add event listeners after a short delay to ensure DOM is ready
    setTimeout(() => {
      const runButton = document.getElementById("run-simulation");

      // Set up the run button to trigger the server simulation
      if (runButton) {
        runButton.addEventListener("click", () => {
          // Reset manual people when running a new simulation
          this.clearManualPeople();
          // Run server simulation
          runServerSimulation();
        });
      }
    }, 500);

    // Add custom scenario controls
    const customDiv = document.createElement("div");
    customDiv.className = "custom-controls";
    customDiv.innerHTML = `
  <h4>Manual People Placement</h4>
  <button id="enable-placement" class="control-button">Place People Manually</button>
  <button id="run-with-manual" class="control-button" disabled>Run With Manual People (0)</button>
  <button id="clear-manual" class="control-button">Clear Manual People</button>
  <div class="manual-placement-info">
    <p><small>Click on map to add people. Right-click to cycle through age groups.</small></p>
  </div>
`;

    if (controlContainer) {
      controlContainer.appendChild(customDiv);
    } else {
      console.error("Control container not found");
    }

    // Add event listeners with proper binding
    setTimeout(() => {
      const enableButton = document.getElementById("enable-placement");
      const runManualButton = document.getElementById("run-with-manual");
      const clearButton = document.getElementById("clear-manual");

      if (enableButton) {
        enableButton.addEventListener("click", () => {
          const isEnabled = enableButton.classList.toggle("active");
          if (isEnabled) {
            enableButton.textContent = "Placing People (Click Map)";
            this.enableManualPlacement(true);
          } else {
            enableButton.textContent = "Place People Manually";
            this.enableManualPlacement(false);
          }
        });
      }

      if (runManualButton) {
        runManualButton.addEventListener("click", () => {
          this.runWithManualPeople();
        });
      }

      if (clearButton) {
        clearButton.addEventListener("click", () => {
          this.clearManualPeople();
        });
      }
    }, 500);
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

  /////////////////////////////////////////////////////////////////
  /**
   * Enables manual placement of people on the map
   * @param {boolean} enable - Whether to enable manual placement
   */
  ////////////////////////////////////////////////////////////////
  enableManualPlacement(enable) {
    if (enable) {
      // Add a message to the status
      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent =
          "Click on the map to add people. Right-click to change age.";
        statusElement.className = "status-message running";
      }

      // Initialize manual people array if it doesn't exist
      if (!this.manualPeople) {
        this.manualPeople = [];
      }

      // Add click handler to the map
      this.map.on("click", this.handleMapClick, this);
      this.map.on("contextmenu", this.handleContextClick, this);
    } else {
      // Remove handlers - fixed to use proper context binding
      this.map.off("click", this.handleMapClick, this);
      this.map.off("contextmenu", this.handleContextClick, this);
    }
  }

  /**
   * Clears all manually placed people
   * Ensures complete clearing of manual people data and markers
   * and restores the original simulation
   */
  clearManualPeople() {
    console.log("Clearing manual people. Before:", this.manualPeople?.length);

    // Reset the manual people array
    this.manualPeople = [];

    // Remove manual markers from the map
    this.peopleMarkers.eachLayer((marker) => {
      if (marker.options && marker.options.isManual) {
        this.peopleMarkers.removeLayer(marker);
      }
    });

    // Restore the original simulation if we have it
    if (this.originalSimulationData) {
      this.visualizeSimulation(
        this.originalSimulationData.people,
        this.originalSimulationData.shelters,
        this.originalSimulationData.assignments
      );
    }

    // Update the button text and FORCE it to be zero
    const runManualButton = document.getElementById("run-with-manual");
    if (runManualButton) {
      runManualButton.textContent = "Run With Manual People (0)";
      runManualButton.value = 0;
      runManualButton.disabled = true;
    }

    console.log("After clearing:", this.manualPeople.length);
  }

  /**
   * Handle map click to add a person
   */
  handleMapClick(e) {
    const lat = e.latlng.lat;
    const lng = e.latlng.lng;

    // Generate a unique ID for this manual person
    // Use a timestamp prefix to ensure uniqueness even after clearing
    const id = `manual_${Date.now()}_${this.manualPeople.length}`;

    // Default to adult (35)
    const person = {
      id: id,
      age: 35,
      latitude: lat,
      longitude: lng,
      isManual: true,
    };

    this.manualPeople.push(person);

    // Add marker for the person
    const icon = this.icons.person;
    const marker = L.marker([lat, lng], {
      icon,
      isManual: true,
      personId: id,
    });

    marker.bindPopup(`
    <p>Person #${this.manualPeople.length} (Manual)</p>
    <p>Age: ${person.age}</p>
    <p>Status: Unassigned</p>
  `);

    this.peopleMarkers.addLayer(marker);

    // Update the "Run with Manual" button status
    this.updateManualControlStatus();
  }

  /**
   * Handle right-click to change age
   */
  handleContextClick(e) {
    // Find if we already have a person at this location
    const lat = e.latlng.lat;
    const lng = e.latlng.lng;

    // Check all markers within a small radius
    let found = false;
    this.peopleMarkers.eachLayer((layer) => {
      if (!layer.options || !layer.options.isManual) return;

      const markerLat = layer.getLatLng().lat;
      const markerLng = layer.getLatLng().lng;

      // If marker is close enough (within ~10 meters)
      if (
        Math.abs(markerLat - lat) < 0.0001 &&
        Math.abs(markerLng - lng) < 0.0001
      ) {
        found = true;

        // Find the person in our manual list by ID
        const personId = layer.options.personId;
        const personIndex = this.manualPeople.findIndex(
          (p) => p.id === personId
        );

        if (personIndex >= 0) {
          // Cycle through age groups: adult -> elderly -> child -> adult
          const person = this.manualPeople[personIndex];
          if (person.age < 70) {
            if (person.age < 12) {
              // Child -> Adult
              person.age = 35;
            } else {
              // Adult -> Elderly
              person.age = 75;
            }
          } else {
            // Elderly -> Child
            person.age = 8;
          }

          // Update marker
          if (person.age >= 70) {
            layer.setIcon(this.icons.elderly);
          } else if (person.age <= 12) {
            layer.setIcon(this.icons.child);
          } else {
            layer.setIcon(this.icons.person);
          }

          // Update popup
          layer.setPopupContent(`
          <p>Person #${personIndex + 1} (Manual)</p>
          <p>Age: ${person.age}</p>
          <p>Status: Unassigned</p>
        `);
        }
      }
    });
  }

  /**
   * Update the manual control button status
   */
  updateManualControlStatus() {
    const manualButton = document.getElementById("run-with-manual");
    if (manualButton && this.manualPeople && this.manualPeople.length > 0) {
      manualButton.textContent = `Run With Manual People (${this.manualPeople.length})`;
      manualButton.disabled = false;
    }
  }

  /**
   * Run simulation with manually placed people added to existing simulation
   * Preserves current shelters and only adds the manual people
   */
  /**
   * Run simulation with manually placed people added to existing simulation
   * Preserves current shelters and only adds the manual people
   */
  runWithManualPeople() {
    if (!this.manualPeople || this.manualPeople.length === 0) {
      alert("Please add some people to the map first");
      return;
    }

    const statusElement = document.getElementById("simulation-status");
    if (statusElement) {
      statusElement.textContent = "Adding manual people to simulation...";
      statusElement.className = "status-message running";
    }

    // Use the original simulation data as our base
    const basePeople = this.originalSimulationData
      ? [...this.originalSimulationData.people]
      : [];

    const baseShelters = this.originalSimulationData
      ? [...this.originalSimulationData.shelters]
      : [];

    // If we don't have original data, run a server simulation first
    if (baseShelters.length === 0) {
      this.runCustomServerSimulation(this.manualPeople);
      return;
    }

    console.log(
      `Adding ${this.manualPeople.length} manual people to base simulation with ${basePeople.length} people`
    );

    // Create a new combined people array with base + all manual people
    const allPeople = [...basePeople];

    // Find the highest ID currently in use
    let maxId = 0;
    basePeople.forEach((person) => {
      if (typeof person.id === "number" && person.id > maxId) {
        maxId = person.id;
      }
    });

    // Add ALL manual people with new sequential IDs
    this.manualPeople.forEach((person, index) => {
      const newPerson = {
        ...person,
        id: maxId + index + 1,
        isManual: true, // Mark as manual for future reference
      };
      allPeople.push(newPerson);
    });

    // Run server simulation with combined people
    this.runServerSimulationWithCustomData(allPeople, baseShelters);
  }

  /**
   * Run server simulation with custom people and shelters
   * @param {Array} customPeople - Array of people objects
   * @param {Array} customShelters - Array of shelter objects
   */
  async runServerSimulationWithCustomData(customPeople, customShelters) {
    const statusElement = document.getElementById("simulation-status");

    try {
      // Get parameters from UI controls
      const priorityEnabled =
        document.getElementById("priority").value === "true";
      const radius = parseFloat(document.getElementById("radius").value) || 0.5;

      // Create a modified request that includes our custom data
      const requestData = {
        peopleCount: 0, // We're providing our own people
        shelterCount: 0, // We're providing our own shelters
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
        useCustomShelters: true,
        customShelters: customShelters,
      };

      // Call the server API with our custom request
      const response = await fetch(
        "https://localhost:{PORT}/api/Simulation/run",
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

      // Save the current simulation data for future reference
      this.currentSimulationData = data;

      // Display the results on the map
      this.visualizeSimulation(data.people, data.shelters, data.assignments);

      // Update status message
      if (statusElement) {
        statusElement.textContent = "Combined simulation complete";
        statusElement.className = "status-message success";

        // Clear the status after a few seconds
        setTimeout(() => {
          statusElement.textContent = "";
          statusElement.className = "status-message";
        }, 3000);
      }
    } catch (error) {
      console.error("Server simulation failed:", error);
      if (statusElement) {
        statusElement.textContent = `Error: ${error.message}`;
        statusElement.className = "status-message error";
      }
    }
  }

  /**
   * Run server simulation with custom people data
   * Uses your existing server API but with manually placed people
   */
  async runCustomServerSimulation(customPeople) {
    const statusElement = document.getElementById("simulation-status");

    try {
      // Get parameters from UI controls
      const shelterCount =
        parseInt(document.getElementById("shelter-count").value) || 8;
      const radius = parseFloat(document.getElementById("radius").value) || 0.5;
      const priorityEnabled =
        document.getElementById("priority").value === "true";

      // Check if we have current shelters to reuse
      const currentShelters = this.currentSimulationData?.shelters || [];

      // Format the custom people data to match server expectations
      const formattedPeople = customPeople.map((person, index) => {
        // Extract numeric ID or create a new one
        let numericId;
        if (typeof person.id === "number") {
          numericId = person.id;
        } else if (
          typeof person.id === "string" &&
          !isNaN(parseInt(person.id))
        ) {
          numericId = parseInt(person.id);
        } else {
          numericId = index + 1000; // Fallback ID
        }

        return {
          id: numericId,
          age: person.age,
          latitude: person.latitude,
          longitude: person.longitude,
        };
      });

      // Create request data with better type handling
      const requestData = {
        peopleCount: 0,
        shelterCount: currentShelters.length > 0 ? 0 : shelterCount,
        centerLatitude: 31.2518,
        centerLongitude: 34.7913,
        radiusKm: radius,
        prioritySettings: {
          enableAgePriority: priorityEnabled,
          childMaxAge: 12,
          elderlyMinAge: 70,
        },
        useCustomPeople: true,
        customPeople: formattedPeople,
      };

      // Add custom shelters if we have them
      if (currentShelters.length > 0) {
        requestData.useCustomShelters = true;
        requestData.customShelters = currentShelters;
      }

      console.log("Sending request to server:", JSON.stringify(requestData));

      // Call the server API with our custom request
      const response = await fetch(
        "https://localhost:{PORT}/api/Simulation/run",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(requestData),
        }
      );

      // Try to get the response text for better error diagnostics
      if (!response.ok) {
        let errorDetail = "";
        try {
          errorDetail = await response.text();
        } catch (e) {
          errorDetail = "Could not read error details";
        }

        console.error("Server error details:", errorDetail);
        throw new Error(
          `Server responded with ${response.status}: ${
            errorDetail || response.statusText
          }`
        );
      }

      // Parse the successful response
      const data = await response.json();

      // Save the combined simulation data
      this.currentSimulationData = data;

      // Display the results on the map
      this.visualizeSimulation(data.people, data.shelters, data.assignments);

      // Update status message
      if (statusElement) {
        statusElement.textContent = "Manual simulation complete";
        statusElement.className = "status-message success";

        // Clear the status after a few seconds
        setTimeout(() => {
          statusElement.textContent = "";
          statusElement.className = "status-message";
        }, 3000);
      }
    } catch (error) {
      // Handle errors
      console.error("Server simulation with custom people failed:", error);
      if (statusElement) {
        statusElement.textContent = `Error: ${error.message}`;
        statusElement.className = "status-message error";
      }

      // Call the fallback method
      this.fallbackCustomSimulation(customPeople);
    }
  }

  /**
   * Fallback method if the server doesn't support custom people
   * This will simply display manually placed people without assignments
   */
  fallbackCustomSimulation(customPeople) {
    const statusElement = document.getElementById("simulation-status");

    if (statusElement) {
      statusElement.textContent =
        "Using fallback for manual people (no assignments)";
      statusElement.className = "status-message warning";
    }

    // If we have existing simulation data, use it as the base
    let people = [];
    let shelters = [];
    let assignments = {};

    if (this.currentSimulationData) {
      // Start with current data
      people = [...this.currentSimulationData.people];
      shelters = [...this.currentSimulationData.shelters];
      assignments = { ...this.currentSimulationData.assignments };

      // Add manual people
      customPeople.forEach((person) => {
        // Only add if not already present (by checking coordinates)
        const exists = people.some(
          (p) =>
            Math.abs(p.latitude - person.latitude) < 0.0001 &&
            Math.abs(p.longitude - person.longitude) < 0.0001
        );

        if (!exists) {
          // Find the next available ID
          const nextId = Math.max(...people.map((p) => p.id), 0) + 1;
          people.push({
            ...person,
            id: nextId,
          });
        }
      });
    } else {
      // No existing data, just use the manual people and generate shelters
      people = customPeople.map((person, idx) => ({
        ...person,
        id: idx + 1,
      }));

      // Generate shelters
      const shelterCount =
        parseInt(document.getElementById("shelter-count").value) || 8;
      shelters = this.generateShelters({
        shelterCount: shelterCount,
        centerLatitude: 31.2518,
        centerLongitude: 34.7913,
        radiusKm: 0.5,
      });
    }

    // Visualize just the people and shelters, without assignments
    this.visualizeSimulation(people, shelters, assignments);

    // Inform the user
    alert(
      "Manual people placement is working, but assignment is not available in this mode. Please use the server simulation for assignments."
    );
  }

  /**
   * Run server simulation with custom shelter data
   */
  async runServerSimulationWithCustomShelters(customShelters) {
    const statusElement = document.getElementById("simulation-status");

    try {
      // Get parameters from UI controls
      const peopleCount =
        parseInt(document.getElementById("people-count").value) || 100;
      const radius = parseFloat(document.getElementById("radius").value) || 0.5;
      const priorityEnabled =
        document.getElementById("priority").value === "true";

      // Create a modified request that includes our custom shelters
      const requestData = {
        peopleCount: peopleCount,
        shelterCount: 0, // We're providing our own shelters, so don't generate any
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

      // Call the server API with our custom request
      const response = await fetch(
        "https://localhost:{PORT}/api/Simulation/run",
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

      // Display the results on the map
      this.visualizeSimulation(data.people, data.shelters, data.assignments);

      // Update status message
      if (statusElement) {
        statusElement.textContent = "Manual shelter simulation complete";
        statusElement.className = "status-message success";

        // Clear the status after a few seconds
        setTimeout(() => {
          statusElement.textContent = "";
          statusElement.className = "status-message";
        }, 3000);
      }
    } catch (error) {
      console.error("Server simulation with custom shelters failed:", error);
      // Fallback to a simpler approach if needed
      this.fallbackCustomShelterSimulation(customShelters);
    }
  }

  /**
   * Generate shelters for manual simulations
   */
  generateShelters(requestData) {
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
        capacity: Math.floor(Math.random() * 5) + 1, // Capacity between 1 and 5
      });
    }

    // Add remaining random shelters if needed
    for (let i = knownLocations.length; i < requestData.shelterCount; i++) {
      const angle = Math.random() * 2 * Math.PI;
      const distance = (Math.random() * requestData.radiusKm * 0.7) / 111.0;

      const latOffset = distance * Math.cos(angle);
      const lonOffset = distance * Math.sin(angle);

      shelters.push({
        id: i + 1,
        name: `Shelter ${i + 1}`,
        latitude: requestData.centerLatitude + latOffset,
        longitude: requestData.centerLongitude + lonOffset,
        capacity: Math.floor(Math.random() * 5) + 1, // Capacity between 1 and 5
      });
    }

    return shelters;
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
  // Ensure we have a valid visualizer
  const visualizer = window.visualizer || initializeVisualizer();

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
    const response = await fetch(
      "https://localhost:{PORT}/api/Simulation/run",
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

    // Save the current simulation data in the visualizer for future reference
    visualizer.currentSimulationData = data;

    // Display the results on the map using the visualizer
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

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
// document.addEventListener("DOMContentLoaded", function () {
//   // Create a new ShelterSimulationVisualizer instance targeting the 'map' element
//   window.visualizer = new ShelterSimulationVisualizer("map");

//   console.log("Shelter simulation visualizer initialized");
// });

// Export functions for use in other modules
if (typeof module !== "undefined" && module.exports) {
  module.exports = {
    runServerSimulation,
    updateServerStatistics,
  };
}
