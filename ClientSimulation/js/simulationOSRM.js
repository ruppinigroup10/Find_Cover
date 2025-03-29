class ShelterSimulationVisualizer {
  constructor(mapElementId) {
    // Initialize the map
    this.map = L.map(mapElementId).setView([31.2518, 34.7913], 13); // Beer Sheva coordinates

    // Base map layer - OpenStreetMap
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    // Initialize marker groups
    this.shelterMarkers = L.layerGroup().addTo(this.map);
    this.peopleMarkers = L.layerGroup().addTo(this.map);
    this.pathLines = L.layerGroup().addTo(this.map);

    // Initialize counters and statistics
    this.stats = {
      totalPeople: 0,
      assignedPeople: 0,
      unassignedPeople: 0,
      averageDistance: 0,
      maxDistance: 0,
      shelterUsage: [],
    };

    // Initialize custom icons
    this.icons = this.createIcons();

    // Add UI controls
    this.addControlPanel();
  }

  // Function to calculate street distance using Leaflet Routing Machine
  calculateStreetDistance(lat1, lon1, lat2, lon2) {
    return new Promise((resolve, reject) => {
      // Debug log
      console.log(
        `Calculating street distance from [${lat1},${lon1}] to [${lat2},${lon2}]`
      );

      try {
        // Create a routing control with default router
        const control = L.Routing.control({
          waypoints: [L.latLng(lat1, lon1), L.latLng(lat2, lon2)],
          routeWhileDragging: false,
          lineOptions: {
            styles: [{ color: "#0000FF", opacity: 0, weight: 0 }], // Hidden path
          },
          fitSelectedRoutes: false,
          show: false,
          showAlternatives: false,
          addWaypoints: false,
          useZoomParameter: false,
          draggableWaypoints: false,
        });

        // Add control to map temporarily to calculate route
        control.addTo(this.map);

        // Listen for route calculation
        control.on("routesfound", (e) => {
          const routes = e.routes;
          if (routes && routes.length > 0) {
            const route = routes[0];
            const distanceInMeters = route.summary.totalDistance;
            const distanceInKm = distanceInMeters / 1000;

            console.log(
              `Street distance calculated: ${distanceInKm.toFixed(2)} km`
            );

            // Update our route data with the actual path coordinates for visualization
            const routeCoords = routes[0].coordinates.map((coord) => [
              coord.lat,
              coord.lng,
            ]);

            // Safe removal - check if the control is still on the map
            try {
              if (this.map) {
                this.map.removeControl(control);
              }
            } catch (err) {
              console.warn("Could not remove routing control:", err);
            }

            // Return both distance and route coordinates
            resolve({
              distance: distanceInKm,
              coordinates: routeCoords,
            });
          } else {
            console.warn("No routes found");
            const airDistance = this.calculateAirDistance(
              lat1,
              lon1,
              lat2,
              lon2
            );

            // Safe removal
            try {
              if (this.map) {
                this.map.removeControl(control);
              }
            } catch (err) {
              console.warn("Could not remove routing control:", err);
            }

            resolve({
              distance: airDistance,
              coordinates: [
                [lat1, lon1],
                [lat2, lon2],
              ],
            });
          }
        });

        // Handle errors
        control.on("routingerror", (e) => {
          console.warn("Routing error:", e.error);
          // Fall back to air distance
          const airDistance = this.calculateAirDistance(lat1, lon1, lat2, lon2);

          // Safe removal
          try {
            if (this.map) {
              this.map.removeControl(control);
            }
          } catch (err) {
            console.warn("Could not remove routing control:", err);
          }

          resolve({
            distance: airDistance,
            coordinates: [
              [lat1, lon1],
              [lat2, lon2],
            ],
          });
        });

        // Timeout if routing takes too long
        setTimeout(() => {
          console.warn("Routing timeout");
          const airDistance = this.calculateAirDistance(lat1, lon1, lat2, lon2);

          // Safe removal
          try {
            if (this.map) {
              this.map.removeControl(control);
            }
          } catch (err) {
            console.warn("Could not remove routing control:", err);
          }

          resolve({
            distance: airDistance,
            coordinates: [
              [lat1, lon1],
              [lat2, lon2],
            ],
          });
        }, 5000);
      } catch (error) {
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
  // Implementation of a method to calculate street distances for all assignments
  async calculateAllStreetDistances(people, shelters, assignments) {
    console.log("Calculating street distances for all assignments...");
    const assignmentKeys = Object.keys(assignments);

    for (let i = 0; i < assignmentKeys.length; i++) {
      const personId = assignmentKeys[i];
      const assignment = assignments[personId];
      const person = people.find((p) => p.id == personId);
      const shelter = shelters.find((s) => s.id == assignment.shelterId);

      if (person && shelter) {
        try {
          // Log progress
          console.log(
            `Calculating route for person ${personId} (${i + 1}/${
              assignmentKeys.length
            })`
          );

          // Calculate street distance
          const result = await this.calculateStreetDistance(
            person.latitude,
            person.longitude,
            shelter.latitude,
            shelter.longitude
          );

          // Update assignment with street distance and route
          assignment.distance = result.distance;
          assignment.route = {
            coordinates: result.coordinates,
          };

          // Add a small delay to avoid overwhelming the routing service
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

  calculateAirDistance(lat1, lon1, lat2, lon2) {
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

  createIcons() {
    return {
      shelter: L.divIcon({
        className: "marker-shelter",
        html: '<div style="background-color: #ff4500; border-radius: 50%; width: 14px; height: 14px;"></div>',
        iconSize: [14, 14],
        iconAnchor: [7, 7],
      }),
      person: L.divIcon({
        className: "marker-person",
        html: '<div style="background-color: #4169e1; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      child: L.divIcon({
        className: "marker-child",
        html: '<div style="background-color: #32cd32; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      elderly: L.divIcon({
        className: "marker-elderly",
        html: '<div style="background-color: #ff69b4; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      unassigned: L.divIcon({
        className: "marker-unassigned",
        html: '<div style="background-color: #808080; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
    };
  }

  clearMap() {
    this.shelterMarkers.clearLayers();
    this.peopleMarkers.clearLayers();
    this.pathLines.clearLayers();

    // Reset statistics
    this.stats = {
      totalPeople: 0,
      assignedPeople: 0,
      unassignedPeople: 0,
      averageDistance: 0,
      maxDistance: 0,
      shelterUsage: [],
    };
  }

  visualizeSimulation(people, shelters, assignments) {
    // Clear previous visualization
    this.clearMap();

    this.stats.totalPeople = people.length;
    this.stats.assignedPeople = Object.keys(assignments).length;
    this.stats.unassignedPeople =
      this.stats.totalPeople - this.stats.assignedPeople;

    // Calculate appropriate bounds for the map
    const bounds = this.calculateBounds(people, shelters);
    this.map.fitBounds(bounds);

    // Display shelters
    this.displayShelters(shelters);

    // Display people and their assignments
    this.displayPeopleAndAssignments(people, shelters, assignments);

    // Update statistics display
    this.updateStatisticsDisplay();
  }

  calculateBounds(people, shelters) {
    const allPoints = [
      ...people.map((p) => [p.latitude, p.longitude]),
      ...shelters.map((s) => [s.latitude, s.longitude]),
    ];

    // If we have data, calculate bounds
    if (allPoints.length > 0) {
      return L.latLngBounds(allPoints);
    }

    // Default bounds if no data
    return L.latLngBounds([
      [32.0853 - 0.1, 34.7818 - 0.1],
      [32.0853 + 0.1, 34.7818 + 0.1],
    ]);
  }

  displayShelters(shelters) {
    // Initialize shelter usage statistics
    this.stats.shelterUsage = shelters.map((s) => ({
      id: s.id,
      name: s.name,
      capacity: s.capacity,
      assigned: 0,
      percentUsed: 0,
    }));

    shelters.forEach((shelter) => {
      const marker = L.marker([shelter.latitude, shelter.longitude], {
        icon: this.icons.shelter,
      });

      // Add popup with shelter info
      marker.bindPopup(`
                <h3>${shelter.name}</h3>
                <p>Capacity: <span id="shelter-${shelter.id}-count">0</span>/${shelter.capacity}</p>
                <p>Status: <span id="shelter-${shelter.id}-status">Empty</span></p>
            `);

      this.shelterMarkers.addLayer(marker);
    });
  }

  displayPeopleAndAssignments(people, shelters, assignments) {
    // Track total distance for average calculation
    let totalDistance = 0;
    this.stats.maxDistance = 0;

    people.forEach((person) => {
      // Determine which icon to use based on age and assignment status
      let icon = this.icons.person;
      if (!assignments[person.id]) {
        icon = this.icons.unassigned;
      } else if (person.age >= 70) {
        icon = this.icons.elderly;
      } else if (person.age <= 12) {
        icon = this.icons.child;
      }

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

          // Update document elements for this shelter
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

          // Show the actual route if you have it
          if (assignment.route && assignment.route.coordinates) {
            const routeLine = L.polyline(assignment.route.coordinates, {
              color: this.getLineColor(person.age),
              opacity: 0.7,
              weight: 3,
            });
            this.pathLines.addLayer(routeLine);
          } else {
            // Fallback to straight line
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

          // Add popup with assignment info
          marker.bindPopup(`
          <p>Person #${person.id}</p>
          <p>Age: ${person.age}</p>
          <p>Assigned to: ${shelter.name}</p>
          <p>Distance: ${assignment.distance.toFixed(2)} km</p>
        `);
        }
      } else {
        // Unassigned person popup
        marker.bindPopup(`
        <p>Person #${person.id}</p>
        <p>Age: ${person.age}</p>
        <p>Status: <span class="status-unassigned">Unassigned</span></p>
      `);
      }

      this.peopleMarkers.addLayer(marker);
    });

    // Calculate average distance
    if (this.stats.assignedPeople > 0) {
      this.stats.averageDistance = totalDistance / this.stats.assignedPeople;
    }
  }

  addControlPanel() {
    // Create control panels
    this.addStatisticsPanel();
    this.addSimulationControlPanel();
  }

  addStatisticsPanel() {
    // Create statistics panel in the top right
    const statsDiv = L.DomUtil.create(
      "div",
      "simulation-statistics " //leaflet-bar
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
      </div>
    `;

    // Create a custom control
    const StatsControl = L.Control.extend({
      options: {
        position: "topright",
      },
      onAdd: () => {
        return statsDiv;
      },
    });

    // Add the control to the map
    new StatsControl().addTo(this.map);
  }

  addSimulationControlPanel() {
    // Create simulation control panel in the top left
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
          <input type="number" id="people-count" min="10" max="500" value="100">
        </div>
        <div class="control-group">
          <label for="shelter-count">Number of Shelters:</label>
          <input type="number" id="shelter-count" min="1" max="50" value="8">
        </div>
        <div class="control-group">
          <label for="radius">Simulation Radius (km):</label>
          <input type="number" id="radius" min="0.5" max="20" step="0.5" value="5">
        </div>
        <div class="control-group">
          <label for="priority">Priority Assignment:</label>
          <select id="priority">
            <option value="true" selected>Enabled (elderly first)</option>
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

    // Create a custom control
    const ControlPanelControl = L.Control.extend({
      options: {
        position: "topleft",
      },
      onAdd: () => {
        return controlDiv;
      },
    });

    // Control to the map
    new ControlPanelControl().addTo(this.map);

    // Event listeners
    setTimeout(() => {
      const runButton = document.getElementById("run-simulation");
      const simTypeSelect = document.getElementById("simulation-type");
      const clientOnlyElements = document.querySelectorAll(".client-only");

      // Toggle visibility of client-only options
      if (simTypeSelect) {
        simTypeSelect.addEventListener("change", () => {
          const isClientSide = simTypeSelect.value === "client";
          clientOnlyElements.forEach((el) => {
            el.style.display = isClientSide ? "block" : "none";
          });
        });
      }

      if (runButton) {
        runButton.addEventListener("click", () => {
          const simulationType =
            document.getElementById("simulation-type").value;
          if (simulationType === "server") {
            runServerSimulation();
          } else {
            this.runSimulationFromUI(); // Local simulation
          }
        });
      }
    }, 500);
  }

  // Method to run simulation with parameters from the UI
  async runSimulationFromUI() {
    const statusElement = document.getElementById("simulation-status");
    if (statusElement) {
      statusElement.textContent = "Running simulation...";
      statusElement.className = "status-message running";
    }

    try {
      // Get parameters from UI
      const peopleCount =
        parseInt(document.getElementById("people-count").value) || 100;
      const shelterCount =
        parseInt(document.getElementById("shelter-count").value) || 8;
      const radius = parseFloat(document.getElementById("radius").value) || 5;
      const priorityEnabled =
        document.getElementById("priority").value === "true";
      const useStreetDistance =
        document.getElementById("route-type").value === "street";

      // Convert radius from km to degrees (approximate)
      const radiusDegrees = radius / 111; // 1 degree ≈ 111 km

      // Generate data
      const people = this.generatePeople(peopleCount, radiusDegrees);
      const shelters = this.generateShelters(shelterCount, radiusDegrees);

      // Run assignment algorithm
      let assignments = this.assignPeopleToShelters(
        people,
        shelters,
        priorityEnabled
      );

      // If street distance is selected, calculate actual street distances
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

      // Visualize the results
      this.visualizeSimulation(people, shelters, assignments);

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

  // Generate people with random locations around the center
  generatePeople(count, radiusDegrees) {
    const people = [];
    const centerLat = 31.2518; // Beer Sheva latitude
    const centerLon = 34.7913; // Beer Sheva longitude

    for (let i = 0; i < count; i++) {
      const age = Math.floor(Math.random() * 90) + 1; // Age 1-90

      // Generate random point within radius
      const angle = Math.random() * 2 * Math.PI;
      const distance = Math.random() * radiusDegrees;
      const lat = centerLat + distance * Math.cos(angle);
      const lon = centerLon + distance * Math.sin(angle);

      people.push({
        id: i + 1,
        age: age,
        latitude: lat,
        longitude: lon,
      });
    }

    return people;
  }

  // Generate shelters with random locations around the center
  generateShelters(count, radiusDegrees) {
    const shelters = [];
    const centerLat = 31.2518; // Beer Sheva latitude
    const centerLon = 34.7913; // Beer Sheva longitude

    for (let i = 0; i < count; i++) {
      // Generate random point within radius, but more central
      const angle = Math.random() * 2 * Math.PI;
      const distance = Math.random() * radiusDegrees * 0.7; // Shelters are more central
      const lat = centerLat + distance * Math.cos(angle);
      const lon = centerLon + distance * Math.sin(angle);

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

  // Assignment algorithm
  assignPeopleToShelters(people, shelters, priorityEnabled) {
    // Create working copies of the data
    const remainingShelters = shelters.map((s) => ({
      ...s,
      remainingCapacity: s.capacity,
      assignedPeople: [],
    }));

    // Sort people by priority if needed
    let peopleToAssign = [...people];
    if (priorityEnabled) {
      peopleToAssign.sort((a, b) => {
        // Children and elderly first
        const aPriority = a.age <= 12 || a.age >= 70 ? 0 : 1;
        const bPriority = b.age <= 12 || b.age >= 70 ? 0 : 1;
        return aPriority - bPriority;
      });
    }

    const assignments = {};

    // Assign each person to the nearest shelter with available capacity
    peopleToAssign.forEach((person) => {
      const availableShelters = remainingShelters.filter(
        (s) => s.remainingCapacity > 0
      );
      if (availableShelters.length === 0) {
        // All shelters at capacity - this person remains unassigned
        return;
      }

      // Find nearest shelter
      availableShelters.sort((a, b) => {
        const distanceA = this.calculateAirDistance(
          person.latitude,
          person.longitude,
          a.latitude,
          a.longitude
        );
        const distanceB = this.calculateAirDistance(
          person.latitude,
          person.longitude,
          b.latitude,
          b.longitude
        );
        return distanceA - distanceB;
      });

      const nearestShelter = availableShelters[0];

      // Calculate distance
      const distance = this.calculateAirDistance(
        person.latitude,
        person.longitude,
        nearestShelter.latitude,
        nearestShelter.longitude
      );

      // Create assignment
      assignments[person.id] = {
        personId: person.id,
        shelterId: nearestShelter.id,
        distance: distance,
      };

      // Update shelter capacity
      nearestShelter.remainingCapacity--;
      nearestShelter.assignedPeople.push(person);
    });

    return assignments;
  }

  updateStatisticsDisplay() {
    // Update main statistics in the control panel
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
      // Clear previous content
      shelterUsageContainer.innerHTML = "";

      // Sort shelters by usage percentage
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

      // Add table body
      const tbody = document.createElement("tbody");

      sortedShelters.forEach((shelter) => {
        const row = document.createElement("tr");

        // Determine status class based on usage
        let statusClass = "status-available";
        if (shelter.percentUsed >= 100) {
          statusClass = "status-full";
        } else if (shelter.percentUsed >= 80) {
          statusClass = "status-almost-full";
        }

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

      // Add summary statistics
      const summaryDiv = document.createElement("div");
      summaryDiv.className = "shelter-summary";

      // Calculate additional stats
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

async function runServerSimulation() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent = "Calling server simulation...";
    statusElement.className = "status-message running";
  }

  try {
    // Get parameters from UI
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 100;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 8;
    const radius = parseFloat(document.getElementById("radius").value) || 5;
    const priorityEnabled =
      document.getElementById("priority").value === "true";

    // Prepare request payload
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

    // Call your API
    const response = await fetch("https://localhost:7094/api/Simulation/run", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(requestData),
    });

    if (!response.ok) {
      throw new Error(
        `Server responded with ${response.status}: ${response.statusText}`
      );
    }

    // Parse the response
    const data = await response.json();

    // Display the results on the map
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Show statistics
    updateServerStatistics(data.statistics);

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
    console.error("Server simulation error:", error);
    if (statusElement) {
      statusElement.textContent = `Error: ${error.message}`;
      statusElement.className = "status-message error";
    }
  }
}

// Helper function to update statistics panel with server data
function updateServerStatistics(stats) {
  if (!stats) return;

  const statsTotal = document.getElementById("stats-total");
  const statsAssigned = document.getElementById("stats-assigned");
  const statsUnassigned = document.getElementById("stats-unassigned");
  const statsAvgDistance = document.getElementById("stats-avg-distance");
  const statsMaxDistance = document.getElementById("stats-max-distance");

  if (statsTotal)
    statsTotal.textContent = stats.assignedCount + stats.unassignedCount;
  if (statsAssigned) statsAssigned.textContent = stats.assignedCount;
  if (statsUnassigned) statsUnassigned.textContent = stats.unassignedCount;
  if (statsAvgDistance)
    statsAvgDistance.textContent = stats.averageDistance.toFixed(2);
  if (statsMaxDistance)
    statsMaxDistance.textContent = stats.maxDistance.toFixed(2);
}
