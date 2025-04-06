/**
 * ShelterSimulationVisualizer - Main class that handles the simulation and visualization of shelter assignments
 * This class creates and manages an interactive map for visualizing emergency shelter assignments
 * It allows generating random populations and shelter locations, then assigns people to shelters
 * based on distance and priority rules, and visualizes the results on the map
 */
class ShelterSimulationVisualizer {
  /**
   * Constructor - initializes the map, layers, statistics, and UI controls
   * @param {string} mapElementId - The HTML element ID where the map will be rendered
   */
  constructor(mapElementId) {
    // Initialize the map centered on Beer Sheva
    this.map = L.map(mapElementId).setView([31.2518, 34.7913], 13); // Beer Sheva coordinates

    // Add OpenStreetMap as the base tile layer
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    // Create layer groups to organize map elements
    this.shelterMarkers = L.layerGroup().addTo(this.map); // Group for shelter markers
    this.peopleMarkers = L.layerGroup().addTo(this.map); // Group for people markers
    this.pathLines = L.layerGroup().addTo(this.map); // Group for path lines connecting people to shelters

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
   * Calculates the real-world street distance between two points using Leaflet Routing Machine
   * This is more accurate than air distance as it accounts for actual road networks
   *
   * @param {number} lat1 - Latitude of the first point
   * @param {number} lon1 - Longitude of the first point
   * @param {number} lat2 - Latitude of the second point
   * @param {number} lon2 - Longitude of the second point
   * @returns {Promise} - Resolves to an object with distance in km and route coordinates
   */
  calculateStreetDistance(lat1, lon1, lat2, lon2) {
    return new Promise((resolve, reject) => {
      // Log debugging information
      console.log(
        `Calculating street distance from [${lat1},${lon1}] to [${lat2},${lon2}]`
      );

      try {
        // Create a Leaflet routing control configured to calculate the route
        const control = L.Routing.control({
          waypoints: [L.latLng(lat1, lon1), L.latLng(lat2, lon2)], // Start and end points
          routeWhileDragging: false,
          lineOptions: {
            styles: [{ color: "#0000FF", opacity: 0, weight: 0 }], // Hidden path - we only need the data
          },
          fitSelectedRoutes: false, // Don't auto-focus map on the route
          show: false, // Don't show the routing control UI
          showAlternatives: false, // We only need one route
          addWaypoints: false, // Prevent adding additional waypoints
          useZoomParameter: false, // Don't modify the zoom level
          draggableWaypoints: false, // Waypoints shouldn't be draggable
        });

        // Add control to map temporarily to calculate route
        control.addTo(this.map);

        // Event handler for successful route calculation
        control.on("routesfound", (e) => {
          const routes = e.routes;
          if (routes && routes.length > 0) {
            // Extract distance from the first route
            const route = routes[0];
            const distanceInMeters = route.summary.totalDistance;
            const distanceInKm = distanceInMeters / 1000;

            console.log(
              `Street distance calculated: ${distanceInKm.toFixed(2)} km`
            );

            // Extract the route path coordinates for visualization
            const routeCoords = routes[0].coordinates.map((coord) => [
              coord.lat,
              coord.lng,
            ]);

            // Remove the routing control from the map to free resources
            try {
              if (this.map) {
                this.map.removeControl(control);
              }
            } catch (err) {
              console.warn("Could not remove routing control:", err);
            }

            // Return both the distance and the route coordinates
            resolve({
              distance: distanceInKm,
              coordinates: routeCoords,
            });
          } else {
            // Fall back to air distance if no routes were found
            console.warn("No routes found");
            const airDistance = this.calculateAirDistance(
              lat1,
              lon1,
              lat2,
              lon2
            );

            // Clean up by removing the control
            try {
              if (this.map) {
                this.map.removeControl(control);
              }
            } catch (err) {
              console.warn("Could not remove routing control:", err);
            }

            // Return air distance and a straight line path
            resolve({
              distance: airDistance,
              coordinates: [
                [lat1, lon1],
                [lat2, lon2],
              ],
            });
          }
        });

        // Handle routing errors by falling back to air distance
        control.on("routingerror", (e) => {
          console.warn("Routing error:", e.error);
          const airDistance = this.calculateAirDistance(lat1, lon1, lat2, lon2);

          // Clean up
          try {
            if (this.map) {
              this.map.removeControl(control);
            }
          } catch (err) {
            console.warn("Could not remove routing control:", err);
          }

          // Return fallback distance and path
          resolve({
            distance: airDistance,
            coordinates: [
              [lat1, lon1],
              [lat2, lon2],
            ],
          });
        });

        // Set a timeout to prevent hanging if routing takes too long
        setTimeout(() => {
          console.warn("Routing timeout");
          const airDistance = this.calculateAirDistance(lat1, lon1, lat2, lon2);

          // Clean up
          try {
            if (this.map) {
              this.map.removeControl(control);
            }
          } catch (err) {
            console.warn("Could not remove routing control:", err);
          }

          // Return fallback after timeout
          resolve({
            distance: airDistance,
            coordinates: [
              [lat1, lon1],
              [lat2, lon2],
            ],
          });
        }, 5000); // 5 second timeout
      } catch (error) {
        // Handle any exceptions in routing setup
        console.error("Error in street distance calculation:", error);
        const airDistance = this.calculateAirDistance(lat1, lon1, lat2, lon2);
        resolve({
          distance: airDistance,
          coordinates: [
            [lat1, lon1],
            [lat2, lon2],
          ],
        });
      }
    });
  }

  /**
   * Calculates street distances for all person-shelter assignments
   * Processes each assignment sequentially to avoid overwhelming routing services
   *
   * @param {Array} people - Array of person objects
   * @param {Array} shelters - Array of shelter objects
   * @param {Object} assignments - Object mapping person IDs to their shelter assignments
   * @returns {Object} - Updated assignments with street distances and routes
   */
  async calculateAllStreetDistances(people, shelters, assignments) {
    console.log("Calculating street distances for all assignments...");
    const assignmentKeys = Object.keys(assignments);

    // Process each assignment sequentially
    for (let i = 0; i < assignmentKeys.length; i++) {
      const personId = assignmentKeys[i];
      const assignment = assignments[personId];
      const person = people.find((p) => p.id == personId);
      const shelter = shelters.find((s) => s.id == assignment.shelterId);

      if (person && shelter) {
        try {
          // Log progress to console
          console.log(
            `Calculating route for person ${personId} (${i + 1}/${
              assignmentKeys.length
            })`
          );

          // Calculate the street distance for this person-shelter pair
          const result = await this.calculateStreetDistance(
            person.latitude,
            person.longitude,
            shelter.latitude,
            shelter.longitude
          );

          // Update the assignment with the calculated distance and route
          assignment.distance = result.distance;
          assignment.route = {
            coordinates: result.coordinates,
          };

          // Small delay to prevent overwhelming the routing service
          await new Promise((resolve) => setTimeout(resolve, 200));
        } catch (error) {
          console.error(
            `Error calculating route for person ${personId}:`,
            error
          );
        }
      }
    }

    console.log("All street distances calculated");
    return assignments;
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
          <label for="simulation-type">Simulation Type:</label>
          <select id="simulation-type">
            <option value="client">Client-side (Browser)</option>
            <option value="server">Server-side (API)</option>
          </select>
        </div>
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
        <div class="control-group client-only">
          <label for="route-type">Route Calculation:</label>
          <select id="route-type">
            <option value="air">Air Distance (faster)</option>
            <option value="street" selected>Street Distance (realistic)</option>
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
      const simTypeSelect = document.getElementById("simulation-type");
      const clientOnlyElements = document.querySelectorAll(".client-only");

      // Toggle visibility of client-only options based on simulation type
      if (simTypeSelect) {
        simTypeSelect.addEventListener("change", () => {
          const isClientSide = simTypeSelect.value === "client";
          clientOnlyElements.forEach((el) => {
            el.style.display = isClientSide ? "block" : "none";
          });
        });
      }

      // Set up the run button to trigger the appropriate simulation type
      if (runButton) {
        runButton.addEventListener("click", () => {
          const simulationType =
            document.getElementById("simulation-type").value;
          if (simulationType === "server") {
            runServerSimulation(); // Call external server simulation function
          } else {
            this.runSimulationFromUI(); // Run local simulation
          }
        });
      }
    }, 500); // Short delay to ensure DOM is ready
  }

  /**
   * Runs a client-side simulation with parameters from the UI controls
   * Generates random people and shelters, assigns people to shelters,
   * and visualizes the results
   */
  async runSimulationFromUI() {
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
      const useStreetDistance =
        document.getElementById("route-type").value === "street";

      // Convert radius from km to approximate degrees (1 degree ≈ 111 km)
      const radiusDegrees = radius / 111;

      // Generate random data for the simulation
      const people = this.generatePeople(peopleCount, radiusDegrees);
      const shelters = this.generateShelters(shelterCount, radiusDegrees);

      // Run the assignment algorithm
      let assignments = this.assignPeopleToShelters(
        people,
        shelters,
        priorityEnabled
      );

      // Calculate street distances if selected (more accurate but slower)
      if (useStreetDistance) {
        if (statusElement) {
          statusElement.textContent = "Calculating street distances...";
        }
        assignments = await this.calculateAllStreetDistances(
          people,
          shelters,
          assignments
        );
      }

      // Visualize the simulation results
      this.visualizeSimulation(people, shelters, assignments);

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
      console.error("Simulation error:", error);
      if (statusElement) {
        statusElement.textContent = "Error running simulation";
        statusElement.className = "status-message error";
      }
    }
  }

  /**
   * Generates random people with realistic age distribution around a center point
   *
   * @param {number} count - Number of people to generate
   * @param {number} radiusDegrees - Radius in degrees to distribute people
   * @returns {Array} - Array of generated person objects
   */
  generatePeople(count, radiusDegrees) {
    const people = [];
    const centerLat = 31.2518; // Beer Sheva latitude
    const centerLon = 34.7913; // Beer Sheva longitude

    for (let i = 0; i < count; i++) {
      let age;
      // Generate age with a realistic distribution:
      // 15% children, 70% adults, 15% elderly
      const ageRand = Math.random();
      if (ageRand < 0.15) {
        // 15% children
        age = Math.floor(Math.random() * 12) + 1; // Ages 1-12
      } else if (ageRand < 0.85) {
        // 70% adults
        age = Math.floor(Math.random() * 57) + 13; // Ages 13-69
      } else {
        // 15% elderly
        age = Math.floor(Math.random() * 25) + 70; // Ages 70-94
      }

      // Generate random point within radius using polar coordinates
      const angle = Math.random() * 2 * Math.PI; // Random angle in radians
      const distance = Math.random() * radiusDegrees; // Random distance within max radius
      const lat = centerLat + distance * Math.cos(angle); // Convert to latitude
      const lon = centerLon + distance * Math.sin(angle); // Convert to longitude

      // Add person to the array
      people.push({
        id: i + 1,
        age: age,
        latitude: lat,
        longitude: lon,
      });
    }

    return people;
  }

  /**
   * Generates random shelters, including some known landmarks in Beer Sheva
   *
   * @param {number} count - Number of shelters to generate
   * @param {number} radiusDegrees - Radius in degrees for random shelters
   * @returns {Array} - Array of generated shelter objects
   */
  generateShelters(count, radiusDegrees) {
    const shelters = [];
    const centerLat = 31.2518; // Beer Sheva latitude
    const centerLon = 34.7913; // Beer Sheva longitude

    // Include some real-world locations as shelters for realism
    const knownLocations = [
      { name: "Ben Gurion University", lat: 31.2634, lon: 34.8044 },
      { name: "Beer Sheva Central Station", lat: 31.2434, lon: 34.798 },
      { name: "Grand Canyon Mall", lat: 31.2508, lon: 34.7738 },
      { name: "Soroka Medical Center", lat: 31.2534, lon: 34.8018 },
    ];

    // Add known locations first if they fit within the requested count
    for (let i = 0; i < Math.min(count, knownLocations.length); i++) {
      const location = knownLocations[i];
      shelters.push({
        id: i + 1,
        name: location.name,
        latitude: location.lat,
        longitude: location.lon,
        capacity: Math.floor(Math.random() * 5) + 1, // Capacity 1-5
      });
    }

    // Add remaining random shelters if needed
    for (let i = knownLocations.length; i < count; i++) {
      // Generate random point within radius, but more central (0.7 factor)
      // Shelters are more centrally located for realism
      const angle = Math.random() * 2 * Math.PI;
      const distance = Math.random() * radiusDegrees * 0.7; // Shelters are more central
      const lat = centerLat + distance * Math.cos(angle);
      const lon = centerLon + distance * Math.sin(angle);

      // Add shelter to the array
      shelters.push({
        id: i + 1,
        name: `Shelter ${i + 1}`,
        latitude: lat,
        longitude: lon,
        capacity: Math.floor(Math.random() * 5) + 1, // Capacity 1-5
      });
    }

    return shelters;
  }

  /**
   * Core algorithm to assign people to shelters based on distance and priority
   * Implements time constraints and optional prioritization for vulnerable groups
   *
   * @param {Array} people - Array of person objects
   * @param {Array} shelters - Array of shelter objects
   * @param {boolean} priorityEnabled - Whether to prioritize children and elderly
   * @returns {Object} - Mapping of person IDs to their shelter assignments
   */
  assignPeopleToShelters(people, shelters, priorityEnabled) {
    // Constants defining time and distance constraints
    const MAX_TRAVEL_TIME_MINUTES = 1.0; // Maximum travel time in minutes
    const WALKING_SPEED_KM_PER_MINUTE = 0.6; // 36 km/h = 0.6 km/min (fast movement)
    const MAX_DISTANCE_KM =
      MAX_TRAVEL_TIME_MINUTES * WALKING_SPEED_KM_PER_MINUTE; // ~600m in 1 minute

    console.log(
      `Time constraint: Maximum distance = ${MAX_DISTANCE_KM.toFixed(4)} km`
    );

    // Create working copies of the shelter data with tracking for capacity
    const remainingShelters = shelters.map((s) => ({
      ...s,
      remainingCapacity: s.capacity, // Track remaining capacity for each shelter
      assignedPeople: [], // Track which people are assigned to this shelter
    }));

    // Calculate total capacity and check if there are enough shelters for everyone
    const totalPeople = people.length;
    const totalShelterCapacity = remainingShelters.reduce(
      (sum, shelter) => sum + shelter.capacity,
      0
    );
    const enoughSheltersForAll = totalShelterCapacity >= totalPeople;

    console.log(
      `Total People: ${totalPeople}, Total Shelter Capacity: ${totalShelterCapacity}`
    );
    console.log(`Enough shelters for all: ${enoughSheltersForAll}`);

    // Find all valid person-shelter pairs within time constraint
    const allValidPairs = [];
    people.forEach((person) => {
      // Find all shelters accessible to this person within the time constraint
      const accessibleShelters = remainingShelters
        .map((shelter) => {
          const distance = this.calculateAirDistance(
            person.latitude,
            person.longitude,
            shelter.latitude,
            shelter.longitude
          );
          return { shelter, distance };
        })
        .filter((pair) => pair.distance <= MAX_DISTANCE_KM) // Filter by max distance
        .sort((a, b) => a.distance - b.distance); // Sort by distance (nearest first)

      // Add all valid person-shelter pairs to the collection
      accessibleShelters.forEach(({ shelter, distance }) => {
        allValidPairs.push({ person, shelter, distance });
      });
    });

    // Get unique people who can reach at least one shelter
    const eligiblePeople = people.filter((p) =>
      allValidPairs.some((pair) => pair.person.id === p.id)
    );

    console.log(
      `People who can reach a shelter: ${eligiblePeople.length} of ${totalPeople}`
    );

    // Initialize assignments object
    const assignments = {};

    // If there are enough shelters for everyone and prioritization is enabled
    if (enoughSheltersForAll && priorityEnabled) {
      console.log("Using priority assignment - enough shelters for everyone");

      // First, assign elderly to nearest shelters (highest priority)
      const elderlyPeople = people.filter((p) => p.age >= 70);
      this.assignPeopleToNearestShelters(
        elderlyPeople,
        remainingShelters,
        assignments,
        MAX_DISTANCE_KM
      );

      // Then, assign children to nearest shelters (second priority)
      const childrenPeople = people
        .filter((p) => p.age <= 12)
        .filter((p) => !assignments[p.id]); // Skip if already assigned
      this.assignPeopleToNearestShelters(
        childrenPeople,
        remainingShelters,
        assignments,
        MAX_DISTANCE_KM
      );

      // Finally, assign remaining adults to nearest shelters (lowest priority)
      const adultPeople = people
        .filter((p) => p.age > 12 && p.age < 70)
        .filter((p) => !assignments[p.id]); // Skip if already assigned
      this.assignPeopleToNearestShelters(
        adultPeople,
        remainingShelters,
        assignments,
        MAX_DISTANCE_KM
      );
    } else {
      console.log(
        "Using random selection - limited shelter capacity or no priority"
      );

      // Randomly shuffle the eligible people for fair selection
      const shuffledEligiblePeople = [...eligiblePeople].sort(
        () => Math.random() - 0.5
      );

      // Calculate available total capacity across all shelters
      const availableCapacity = remainingShelters.reduce(
        (sum, s) => sum + s.remainingCapacity,
        0
      );

      // Limit selection to available capacity
      const selectedPeople = shuffledEligiblePeople.slice(0, availableCapacity);

      console.log(
        `Randomly selected ${selectedPeople.length} people for assignment`
      );

      // Even with random selection, still prioritize elderly within the selected group
      const selectedElderly = selectedPeople.filter((p) => p.age >= 70);
      const selectedNonElderly = selectedPeople.filter((p) => p.age < 70);

      console.log(
        `Selected elderly: ${selectedElderly.length}, Selected non-elderly: ${selectedNonElderly.length}`
      );

      // Assign elderly to their nearest shelters first
      this.assignPeopleToNearestShelters(
        selectedElderly,
        remainingShelters,
        assignments,
        MAX_DISTANCE_KM
      );

      // Then assign remaining people to available shelters
      this.assignPeopleToNearestShelters(
        selectedNonElderly,
        remainingShelters,
        assignments,
        MAX_DISTANCE_KM
      );
    }

    console.log(
      `Final assignments: ${Object.keys(assignments).length} people assigned`
    );

    return assignments;
  }

  /**
   * Helper method to assign people to their nearest shelters with capacity
   * Called by the main assignment algorithm with different priority groups
   *
   * @param {Array} peopleToAssign - Array of person objects to assign
   * @param {Array} availableShelters - Array of shelter objects with remaining capacity
   * @param {Object} assignments - Mapping of person IDs to assignments (modified in place)
   * @param {number} maxDistanceKm - Maximum allowed distance
   */
  assignPeopleToNearestShelters(
    peopleToAssign,
    availableShelters,
    assignments,
    maxDistanceKm
  ) {
    peopleToAssign.forEach((person) => {
      // Skip if already assigned
      if (assignments[person.id]) return;

      // Find shelters with capacity within max distance for this person
      const accessibleShelters = availableShelters
        .filter((s) => s.remainingCapacity > 0) // Only consider shelters with capacity
        .map((shelter) => {
          // Calculate distance from person to shelter
          const distance = this.calculateAirDistance(
            person.latitude,
            person.longitude,
            shelter.latitude,
            shelter.longitude
          );
          return { shelter, distance };
        })
        .filter((pair) => pair.distance <= maxDistanceKm) // Filter by max distance
        .sort((a, b) => a.distance - b.distance); // Sort by distance (nearest first)

      // Assign to the nearest shelter if any are accessible
      if (accessibleShelters.length > 0) {
        const { shelter, distance } = accessibleShelters[0];

        // Create assignment record
        assignments[person.id] = {
          personId: person.id,
          shelterId: shelter.id,
          distance: distance,
        };

        // Update shelter capacity and tracking
        shelter.remainingCapacity--;
        shelter.assignedPeople.push(person);
      }
    });
  }

  /**
   * Updates the statistics display in the UI with current simulation results
   * Populates tables and charts with data about assignments, shelter usage, etc.
   */
  updateStatisticsDisplay() {
    // Update main statistics in the control panel
    const statsTotal = document.getElementById("stats-total");
    const statsAssigned = document.getElementById("stats-assigned");
    const statsUnassigned = document.getElementById("stats-unassigned");
    const statsAvgDistance = document.getElementById("stats-avg-distance");
    const statsMaxDistance = document.getElementById("stats-max-distance");

    // Update summary count values
    if (statsTotal) statsTotal.textContent = this.stats.totalPeople;
    if (statsAssigned) statsAssigned.textContent = this.stats.assignedPeople;
    if (statsUnassigned)
      statsUnassigned.textContent = this.stats.unassignedPeople;
    if (statsAvgDistance)
      statsAvgDistance.textContent = this.stats.averageDistance.toFixed(2);
    if (statsMaxDistance)
      statsMaxDistance.textContent = this.stats.maxDistance.toFixed(2);

    // Update age group statistics table
    const ageStatsContainer = document.getElementById("age-stats-container");
    if (ageStatsContainer) {
      ageStatsContainer.innerHTML = ""; // Clear previous content

      // Create age group table
      const ageTable = document.createElement("table");
      ageTable.className = "age-stats-table";

      // Populate table with age group statistics
      ageTable.innerHTML = `
      <thead>
        <tr>
          <th>Age Group</th>
          <th>Assigned</th>
          <th>Unassigned</th>
          <th>Total</th>
          <th>Assignment %</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>Elderly (70+)</td>
          <td class="status-available">${
            this.stats.ageGroups.assigned.elderly
          }</td>
          <td class="status-unassigned">${
            this.stats.ageGroups.unassigned.elderly
          }</td>
          <td>${
            this.stats.ageGroups.assigned.elderly +
            this.stats.ageGroups.unassigned.elderly
          }</td>
          <td>${this.calculatePercentage(
            this.stats.ageGroups.assigned.elderly,
            this.stats.ageGroups.assigned.elderly +
              this.stats.ageGroups.unassigned.elderly
          )}%</td>
        </tr>
        <tr>
          <td>Children (≤12)</td>
          <td class="status-available">${
            this.stats.ageGroups.assigned.children
          }</td>
          <td class="status-unassigned">${
            this.stats.ageGroups.unassigned.children
          }</td>
          <td>${
            this.stats.ageGroups.assigned.children +
            this.stats.ageGroups.unassigned.children
          }</td>
          <td>${this.calculatePercentage(
            this.stats.ageGroups.assigned.children,
            this.stats.ageGroups.assigned.children +
              this.stats.ageGroups.unassigned.children
          )}%</td>
        </tr>
        <tr>
          <td>Adults (13-69)</td>
          <td class="status-available">${
            this.stats.ageGroups.assigned.adults
          }</td>
          <td class="status-unassigned">${
            this.stats.ageGroups.unassigned.adults
          }</td>
          <td>${
            this.stats.ageGroups.assigned.adults +
            this.stats.ageGroups.unassigned.adults
          }</td>
          <td>${this.calculatePercentage(
            this.stats.ageGroups.assigned.adults,
            this.stats.ageGroups.assigned.adults +
              this.stats.ageGroups.unassigned.adults
          )}%</td>
        </tr>
      </tbody>
    `;

      ageStatsContainer.appendChild(ageTable);

      // Add time constraint information box
      const timeConstraintInfo = document.createElement("div");
      timeConstraintInfo.className = "time-constraint-info";
      timeConstraintInfo.innerHTML = `
      <h4>Time Constraint Info</h4>
      <p>Max travel time: 1 minute</p>
      <p>Walking speed: 5 km/h (~0.08 km/min)</p>
      <p>Max possible distance: 0.08 km (~80 meters)</p>
    `;
      ageStatsContainer.appendChild(timeConstraintInfo);
    }

    // Update shelter usage statistics table
    const shelterUsageContainer = document.getElementById(
      "shelter-usage-container"
    );
    if (shelterUsageContainer) {
      // Clear previous content
      shelterUsageContainer.innerHTML = "";

      // Sort shelters by usage percentage (most full first)
      const sortedShelters = [...this.stats.shelterUsage].sort(
        (a, b) => b.percentUsed - a.percentUsed
      );

      // Create a table for shelter statistics
      const table = document.createElement("table");
      table.className = "shelter-stats-table";

      // Add table header
      const thead = document.createElement("thead");
      thead.innerHTML = `
      <tr>
        <th>Shelter</th>
        <th>Usage</th>
        <th>Capacity</th>
      </tr>
    `;
      table.appendChild(thead);

      // Add table body with rows for each shelter
      const tbody = document.createElement("tbody");

      sortedShelters.forEach((shelter) => {
        const row = document.createElement("tr");

        // Determine status class based on usage percentage
        let statusClass = "status-available";
        if (shelter.percentUsed >= 100) {
          statusClass = "status-full"; // Red for full
        } else if (shelter.percentUsed >= 80) {
          statusClass = "status-almost-full"; // Orange for almost full
        }

        // Create row content with shelter info and capacity visualization
        row.innerHTML = `
        <td>${shelter.name}</td>
        <td class="${statusClass}">${shelter.assigned}/${
          shelter.capacity
        } (${shelter.percentUsed.toFixed(0)}%)</td>
        <td>
          <div class="capacity-bar">
            <div class="capacity-fill ${statusClass}" style="width: ${Math.min(
          100,
          shelter.percentUsed
        )}%"></div>
          </div>
        </td>
      `;

        tbody.appendChild(row);
      });

      table.appendChild(tbody);
      shelterUsageContainer.appendChild(table);

      // Add summary statistics about shelter usage
      const summaryDiv = document.createElement("div");
      summaryDiv.className = "shelter-summary";

      // Calculate additional stats about shelter status
      const fullShelters = sortedShelters.filter(
        (s) => s.percentUsed >= 100
      ).length;
      const almostFullShelters = sortedShelters.filter(
        (s) => s.percentUsed >= 80 && s.percentUsed < 100
      ).length;
      const emptyShelters = sortedShelters.filter(
        (s) => s.assigned === 0
      ).length;

      summaryDiv.innerHTML = `
      <p>Full shelters: <span class="status-full">${fullShelters}</span></p>
      <p>Almost full: <span class="status-almost-full">${almostFullShelters}</span></p>
      <p>Empty shelters: <span>${emptyShelters}</span></p>
    `;

      shelterUsageContainer.appendChild(summaryDiv);
    }
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

  // Optional: Run an initial simulation with default values
  // visualizer.runSimulationFromUI();
});

// Export functions for use in other modules
if (typeof module !== "undefined" && module.exports) {
  module.exports = {
    runServerSimulation,
    updateServerStatistics,
  };
}
