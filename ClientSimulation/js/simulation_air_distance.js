class ShelterSimulationVisualizer {
  // Function to calculate street distance using Leaflet Routing Machine
  // This function uses the OSRM router to calculate the distance between two points
  calculateStreetDistance(lat1, lon1, lat2, lon2) {
    return new Promise((resolve, reject) => {
      // Create a hidden routing control
      const control = L.Routing.control({
        waypoints: [L.latLng(lat1, lon1), L.latLng(lat2, lon2)],
        router: L.Routing.osrm({
          serviceUrl: "https://router.project-osrm.org/route/v1",
        }),
        fitSelectedRoutes: false,
        show: false,
        showAlternatives: false,
        addWaypoints: false,
      }).addTo(this.map);

      // Listen for route calculation
      control.on("routesfound", function (e) {
        const route = e.routes[0];
        const distanceInMeters = route.summary.totalDistance;
        const distanceInKm = distanceInMeters / 1000;

        // Remove the routing control after we get the result
        control.remove();

        resolve(distanceInKm);
      });

      // Handle errors
      control.on("routingerror", function (e) {
        console.warn("Routing error:", e.error);
        // Fall back to air distance
        const airDistance = calculateAirDistance(lat1, lon1, lat2, lon2);
        control.remove();
        resolve(airDistance);
      });

      // Timeout if routing takes too long
      setTimeout(() => {
        console.warn("Routing timeout");
        const airDistance = calculateAirDistance(lat1, lon1, lat2, lon2);
        control.remove();
        resolve(airDistance);
      }, 5000);
    });

    // Helper for air distance calculation
    function calculateAirDistance(lat1, lon1, lat2, lon2) {
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
  }
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

    // Add UI controls if needed
    this.addControlPanel();
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
      } else if (person.age <= 12) {
        icon = this.icons.child;
      } else if (person.age >= 70) {
        icon = this.icons.elderly;
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

          // Draw a line between person and assigned shelter
          const line = L.polyline(
            [
              [person.latitude, person.longitude],
              [shelter.latitude, shelter.longitude],
            ],
            { color: this.getLineColor(person.age), opacity: 0.7, weight: 2 }
          );
          this.pathLines.addLayer(line);

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

  getLineColor(age) {
    // Different colors based on age categories
    if (age <= 12) return "#3388ff"; // Blue for children
    if (age >= 70) return "#ff3388"; // Pink for elderly
    return "#33ff88"; // Green for adults
  }

  updateStatisticsDisplay() {
    // Update statistics panel if it exists
    const statsPanel = document.getElementById("statistics-panel");
    if (statsPanel) {
      statsPanel.innerHTML = `
                <h3>Simulation Statistics</h3>
                <p>Total People: ${this.stats.totalPeople}</p>
                <p>Assigned: ${this.stats.assignedPeople} (${(
        (this.stats.assignedPeople / this.stats.totalPeople) *
        100
      ).toFixed(2)}%)</p>
                <p>Unassigned: ${this.stats.unassignedPeople}</p>
                <p>Average Distance: ${this.stats.averageDistance.toFixed(
                  2
                )} km</p>
                <p>Maximum Distance: ${this.stats.maxDistance.toFixed(2)} km</p>

                <h4>Shelter Usage</h4>
                <div class="shelter-usage-container">
                    ${this.stats.shelterUsage
                      .map(
                        (shelter) => `
                        <div class="shelter-usage-bar">
                            <div class="shelter-name">${shelter.name}</div>
                            <div class="shelter-bar-container">
                                <div class="shelter-bar" style="width: ${Math.min(
                                  100,
                                  shelter.percentUsed
                                )}%"></div>
                            </div>
                            <div class="shelter-count">${shelter.assigned}/${
                          shelter.capacity
                        }</div>
                        </div>
                    `
                      )
                      .join("")}
                </div>
            `;
    }
  }

  addControlPanel() {
    // Create a control panel for simulation parameters if needed
    const controlPanel = L.control({ position: "topleft" });

    controlPanel.onAdd = (map) => {
      const div = L.DomUtil.create("div", "control-panel");
      div.innerHTML = `
                <div class="control-header">Simulation Controls</div>
                <div class="control-content">
                    <div class="form-group">
                        <label for="people-count">Number of People:</label>
                        <input type="number" id="people-count" min="10" max="10000" value="1000">
                    </div>
                    <div class="form-group">
                        <label for="shelter-count">Number of Shelters:</label>
                        <input type="number" id="shelter-count" min="1" max="100" value="20">
                    </div>
                    <div class="form-group">
                        <label for="radius">Radius (km):</label>
                        <input type="number" id="radius" min="1" max="50" value="10" step="0.5">
                    </div>
                    <div class="form-group">
                        <label for="priority">Enable Age Priority:</label>
                        <input type="checkbox" id="priority" checked>
                    </div>
                    <button id="run-simulation" class="control-button">Run Simulation</button>
                </div>
            `;

      // Prevent map interactions when interacting with the control
      L.DomEvent.disableClickPropagation(div);
      L.DomEvent.disableScrollPropagation(div);

      return div;
    };

    controlPanel.addTo(this.map);

    // Add statistics panel
    const statsPanel = L.control({ position: "topright" });

    statsPanel.onAdd = (map) => {
      const div = L.DomUtil.create("div", "statistics-panel");
      div.id = "statistics-panel";
      div.innerHTML =
        "<h3>Simulation Statistics</h3><p>Run a simulation to see statistics</p>";

      // Prevent map interactions when interacting with the stats panel
      L.DomEvent.disableClickPropagation(div);
      L.DomEvent.disableScrollPropagation(div);

      return div;
    };

    statsPanel.addTo(this.map);

    // Set up event listeners
    this.setupEventListeners();
  }

  setupEventListeners() {
    const runButton = document.getElementById("run-simulation");
    if (runButton) {
      runButton.addEventListener("click", () => {
        this.runSimulation();
      });
    }
  }

  runSimulation() {
    // Get parameters from UI
    const peopleCount = parseInt(document.getElementById("people-count").value);
    const shelterCount = parseInt(
      document.getElementById("shelter-count").value
    );
    const radius = parseFloat(document.getElementById("radius").value);
    const enablePriority = document.getElementById("priority").checked;

    // Show loading indicator
    this.showLoading(true);

    // Create the request data object
    const requestData = {
      peopleCount,
      shelterCount,
      centerLatitude: this.map.getCenter().lat,
      centerLongitude: this.map.getCenter().lng,
      radiusKm: radius,
      prioritySettings: {
        enableAgePriority: enablePriority,
        childMaxAge: 12,
        elderlyMinAge: 70,
      },
    };

    console.log("Sending request data:", requestData);

    // Make API call to backend
    fetch("https://localhost:7094/api/Simulation/run", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(requestData),
    })
      .then((response) => {
        console.log("Response status:", response.status, response.statusText);

        if (!response.ok) {
          return response.text().then((errorText) => {
            console.error("Response error text:", errorText);
            throw new Error(
              `Server returned ${response.status}: ${response.statusText}`
            );
          });
        }

        return response.text();
      })
      .then((responseText) => {
        console.log("Response text:", responseText);

        let data;
        if (responseText && responseText.trim()) {
          data = JSON.parse(responseText);
        } else {
          throw new Error("Empty response from server");
        }

        // Visualize the results
        this.visualizeSimulation(data.people, data.shelters, data.assignments);

        // Hide loading indicator
        this.showLoading(false);
      })
      .catch((error) => {
        this.showLoading(false);
        console.error("Error running simulation:", error);
        this.showError(`Failed to run simulation: ${error.message}`);
      });
  }

  showLoading(isLoading) {
    let loadingElement = document.getElementById("loading-indicator");

    if (isLoading) {
      if (!loadingElement) {
        loadingElement = document.createElement("div");
        loadingElement.id = "loading-indicator";
        loadingElement.innerHTML = "Running simulation...";
        document.body.appendChild(loadingElement);
      }
      loadingElement.style.display = "flex";
    } else if (loadingElement) {
      loadingElement.style.display = "none";
    }
  }

  showError(message) {
    alert(message);
  }
}
