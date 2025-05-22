// Log which port the application is using for server communication
console.log("Using port:", PORT);

//==============================================================
// Shelter Simulation Visualizer
//==============================================================

/**
 * ShelterSimulationVisualizer - This is the main class that shows shelter assignments on a map
 * It's responsible for creating an interactive map that shows people, shelters,
 * and the optimal routes between them in emergency situations
 */
class ShelterSimulationVisualizer {
  /**
   * Constructor - Sets up the map, layers, and statistics objects when the class is created
   * @param {string} mapElementId - The HTML element where the map will be displayed
   * @param {boolean} addControls - Whether to add control buttons to the map
   */
  constructor(mapElementId) {
    // First, check if the map container element exists on the page
    const mapContainer = document.getElementById(mapElementId);
    console.log(
      "Visualizer instance created with ID:",
      Math.random().toString(36).substr(2, 9)
    );
    console.log("this reference in constructor:", this);
    if (!mapContainer) {
      // If the container doesn't exist, log an error and stop
      console.error(`Map container with ID ${mapElementId} not found`);
      return;
    }

    // Make sure the map container has proper dimensions before creating the map
    if (mapContainer.offsetWidth === 0 || mapContainer.offsetHeight === 0) {
      // If container has no size, set default dimensions
      console.warn("Map container has zero dimensions. Setting default size.");
      mapContainer.style.width = "100%";
      mapContainer.style.height = "500px";
    }

    // If there's already a map in this container, clean it up first to avoid conflicts
    if (mapContainer._leaflet_id) {
      console.log("Cleaning up existing map instance");
      mapContainer._leaflet = null;
      mapContainer._leaflet_id = null;
    }

    // Create a new Leaflet map centered on Beer Sheva, Israel
    this.map = L.map(mapElementId).setView([31.2518, 34.7913], 13); // Beer Sheva coordinates

    // Add the basic map tiles from OpenStreetMap
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    // Create separate layers for shelters, people, and path lines for better organization
    this.shelterMarkers = L.layerGroup().addTo(this.map); // For shelter locations
    this.peopleMarkers = L.layerGroup().addTo(this.map); // For people locations
    this.pathLines = L.layerGroup().addTo(this.map); // For lines connecting people to shelters

    // Initialize statistics object to track simulation results
    this.stats = {
      totalPeople: 0, // Count of total people in simulation
      assignedPeople: 0, // Count of people assigned to shelters
      unassignedPeople: 0, // Count of people without shelter assignments
      averageDistance: 0, // Average distance to assigned shelters
      maxDistance: 0, // Maximum distance anyone has to travel
      shelterUsage: [], // Will store usage statistics for each shelter
      ageGroups: {
        // Tracks demographics of assigned/unassigned people
        assigned: { elderly: 0, children: 0, adults: 0 },
        unassigned: { elderly: 0, children: 0, adults: 0 },
      },
    };

    // Create custom map icons for different entities (shelters, people of different ages)
    this.icons = this.createIcons();

    // Store the original simulation data for reference
    this.originalSimulationData = null; // Base simulation without manual additions
    this.manualPeople = []; // Array to store manually added people
    this.removalModeActive = false; // Track if removal mode is active

    this.addDirectRemovalButton();
  }

  /**
   * Calculates the direct "as the crow flies" distance between two points on Earth
   * Uses the Haversine formula, which accounts for Earth's curvature
   *
   * @param {number} lat1 - Latitude of the first point
   * @param {number} lon1 - Longitude of the first point
   * @param {number} lat2 - Latitude of the second point
   * @param {number} lon2 - Longitude of the second point
   * @returns {number} - Distance in kilometers
   */
  calculateAirDistance(lat1, lon1, lat2, lon2) {
    const R = 6371; // Earth's radius in kilometers
    const dLat = ((lat2 - lat1) * Math.PI) / 180; // Convert latitude difference to radians
    const dLon = ((lon2 - lon1) * Math.PI) / 180; // Convert longitude difference to radians

    // Haversine formula calculates distance on a sphere (Earth)
    // the formula provides a good approximation for small distances
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos((lat1 * Math.PI) / 180) *
        Math.cos((lat2 * Math.PI) / 180) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c; // Final distance in kilometers
  }

  /**
   * Creates custom map icons for different entity types on the map
   * These icons help distinguish between shelters, adults, children, and elderly people
   *
   * @returns {Object} - Collection of icons for different entity types
   */
  createIcons() {
    return {
      // Shelter icon - orange circular dot
      shelter: L.divIcon({
        className: "marker-shelter",
        html: '<div style="background-color: #ff4500; border-radius: 50%; width: 14px; height: 14px;"></div>',
        iconSize: [14, 14],
        iconAnchor: [7, 7],
      }),
      // Regular person (adult) icon - blue circular dot
      person: L.divIcon({
        className: "marker-person",
        html: '<div style="background-color: #4169e1; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      // Child icon - green circular dot
      child: L.divIcon({
        className: "marker-child",
        html: '<div style="background-color: #32cd32; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      // Elderly icon - pink circular dot
      elderly: L.divIcon({
        className: "marker-elderly",
        html: '<div style="background-color: #ff69b4; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),
      // Unassigned person icon - gray circular dot (no shelter assignment)
      unassigned: L.divIcon({
        className: "marker-unassigned",
        html: '<div style="background-color: #808080; border-radius: 50%; width: 10px; height: 10px;"></div>',
        iconSize: [10, 10],
        iconAnchor: [5, 5],
      }),

      // Special function to create shelter icons with size based on capacity
      // Larger shelters get bigger icons, and the shelter ID is displayed inside
      getShelterIcon: function (capacity, shelterId = "") {
        // Ensure capacity is within reasonable bounds (3-7)
        const cappedCapacity = Math.max(3, Math.min(7, capacity));

        // Define specific sizes for each capacity value
        const sizeMap = {
          3: 14, // Smallest shelter size
          4: 18,
          5: 22,
          6: 26,
          7: 30, // Largest shelter size
        };

        // Define color shades for each capacity (light to dark red)
        const colorMap = {
          3: "#CC3333", // Light red for small shelters
          4: "#CC0000",
          5: "#BB0000",
          6: "#990000",
          7: "#660000", // Darkest red for large shelters
        };

        // Get the appropriate size and color based on the shelter's capacity
        const size = sizeMap[cappedCapacity];
        const color = colorMap[cappedCapacity];

        // Calculate the center point for proper positioning
        const anchor = size / 2;

        // Create HTML that includes both the circular marker and the shelter ID number
        return L.divIcon({
          className: "marker-shelter",
          html: `
            <div style="
            position: relative;
            background-color: ${color}; 
            border-radius: 50%; 
            width: ${size}px; 
            height: ${size}px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: bold;
            color: white;
            font-size: ${size > 20 ? 12 : 10}px;
            ">${shelterId}</div>
            `,
          iconSize: [size, size],
          iconAnchor: [anchor, anchor],
        });
      },
    };
  }

  /**
   * Clears all map data and resets statistics
   * Called before visualizing a new simulation to start fresh
   */
  clearMap() {
    // Remove all markers and lines from the map
    this.shelterMarkers.clearLayers(); // Clear shelter markers
    this.peopleMarkers.clearLayers(); // Clear people markers
    this.pathLines.clearLayers(); // Clear all connecting lines

    // Reset all statistics to initial values
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
   * Main function to display simulation results on the map
   * Shows all people, shelters, and the connections between them
   *
   * @param {Array} people - Array of person objects with location and age info
   * @param {Array} shelters - Array of shelter objects with location and capacity
   * @param {Object} assignments - Object mapping person IDs to their shelter assignments
   */
  visualizeSimulation(people, shelters, assignments) {
    // Save the current simulation data for reference
    this.currentSimulationData = {
      people: people,
      shelters: shelters,
      assignments: assignments,
    };

    // If this is a standard simulation (not one with manual people added),
    // save it as the original baseline data
    if (!people.some((p) => p.isManual)) {
      this.originalSimulationData = {
        people: [...people], // Create a copy to preserve the original
        shelters: [...shelters],
        assignments: { ...assignments },
      };
      console.log("Saved original simulation with", people.length, "people");
    }

    // Start fresh by clearing the map and statistics
    this.clearMap();

    // Update basic statistics about the current simulation
    this.stats.totalPeople = people.length;
    this.stats.assignedPeople = Object.keys(assignments).length;
    this.stats.unassignedPeople =
      this.stats.totalPeople - this.stats.assignedPeople;

    // Reset age group statistics for a fresh count
    this.stats.ageGroups = {
      assigned: { elderly: 0, children: 0, adults: 0 },
      unassigned: { elderly: 0, children: 0, adults: 0 },
    };

    // Calculate map bounds to ensure all people and shelters are visible
    const bounds = this.calculateBounds(people, shelters);
    this.map.fitBounds(bounds);

    // Add all shelters to the map
    this.displayShelters(shelters);

    // Add all people and their shelter assignments to the map
    this.displayPeopleAndAssignments(people, shelters, assignments);

    // Update the statistics display in the UI
    this.updateStatisticsDisplay();

    // Update shelter popups with current assignment data
    this.updateShelterPopups();

    this.updateAgeGroupStatistics(people, assignments);
  }

  /**
   * Calculates the geographical boundaries that contain all people and shelters
   * Used to set the map view to show all entities at once
   *
   * @param {Array} people - Array of person objects with coordinates
   * @param {Array} shelters - Array of shelter objects with coordinates
   * @returns {L.LatLngBounds} - Leaflet bounds object defining the area to show
   */
  calculateBounds(people, shelters) {
    // Combine all coordinates (people and shelters) into a single array
    const allPoints = [
      ...people.map((p) => [p.latitude, p.longitude]), // All people coordinates
      ...shelters.map((s) => [s.latitude, s.longitude]), // All shelter coordinates
    ];

    // If we have data points, calculate bounds that include all of them
    if (allPoints.length > 0) {
      return L.latLngBounds(allPoints);
    }

    // Default bounds centered on Tel Aviv if no data is available
    // Creates a small area around Tel Aviv coordinates
    return L.latLngBounds([
      [32.0853 - 0.1, 34.7818 - 0.1], // Southwest corner
      [32.0853 + 0.1, 34.7818 + 0.1], // Northeast corner
    ]);
  }

  /**
   * Displays all shelter markers on the map
   * Creates shelter icons sized based on capacity and initializes usage statistics
   *
   * @param {Array} shelters - Array of shelter objects to display
   */
  displayShelters(shelters) {
    // Initialize shelter usage statistics tracking for each shelter
    this.stats.shelterUsage = shelters.map((s) => ({
      id: s.id,
      name: s.name,
      capacity: s.capacity,
      assigned: 0, // Number of people assigned (starts at 0)
      percentUsed: 0, // Percentage of capacity used (starts at 0)
    }));

    // Add each shelter as a marker on the map
    shelters.forEach((shelter) => {
      // Create a dynamically sized icon based on this shelter's capacity
      // Larger capacity shelters get bigger icons, and the shelter ID is displayed
      const shelterIcon = this.icons.getShelterIcon(
        shelter.capacity,
        shelter.id
      );

      // Create the marker at the shelter's coordinates
      const marker = L.marker([shelter.latitude, shelter.longitude], {
        icon: shelterIcon,
      });

      // Add popup with shelter information that appears when clicked
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
   * Updates the popup content for all shelter markers
   * Called when assignments change to reflect current occupancy
   */
  updateShelterPopups() {
    // Find all shelter markers and update their popup contents
    this.shelterMarkers.eachLayer((marker) => {
      // Extract the shelter ID from the marker's popup content
      const shelterIdMatch =
        marker._popup._content.match(/shelter-(\d+)-count/);
      if (shelterIdMatch && shelterIdMatch[1]) {
        const shelterId = parseInt(shelterIdMatch[1]);

        // Find this shelter's stats
        const shelterStat = this.stats.shelterUsage.find(
          (s) => s.id === shelterId
        );

        if (shelterStat) {
          // Update the popup content with current data
          const shelter = this.currentSimulationData.shelters.find(
            (s) => s.id === shelterId
          );
          if (shelter) {
            marker.setPopupContent(`
                  <h3>${shelter.name}</h3>
                  <p>Capacity: <span id="shelter-${shelter.id}-count">${
              shelterStat.assigned
            }</span>/${shelter.capacity}</p>
                  <p>Status: <span id="shelter-${shelter.id}-status" class="${
              shelterStat.assigned >= shelter.capacity
                ? "status-full"
                : shelterStat.assigned >= shelter.capacity * 0.8
                ? "status-almost-full"
                : "status-available"
            }">${
              shelterStat.assigned >= shelter.capacity
                ? "Full"
                : shelterStat.assigned >= shelter.capacity * 0.8
                ? "Almost Full"
                : "Available"
            }</span></p>
                  `);
          }
        }
      }
    });
  }

  /**
   * Displays all people and their shelter assignments on the map
   * Creates people markers with different colors based on age and assignment status
   * Draws lines connecting people to their assigned shelters
   *
   * @param {Array} people - Array of person objects to display
   * @param {Array} shelters - Array of shelter objects
   * @param {Object} assignments - Mapping of person IDs to shelter assignments
   */
  displayPeopleAndAssignments(people, shelters, assignments) {
    // Track total distance for average calculation
    let totalDistance = 0;
    this.stats.maxDistance = 0;

    // Process each person one by one
    people.forEach((person) => {
      // Determine which icon to use based on age and assignment status
      let icon = this.icons.person; // Default icon for adults

      // Handle people who haven't been assigned to a shelter
      if (!assignments[person.id]) {
        icon = this.icons.unassigned; // Gray icon for unassigned people

        // Update age group statistics for unassigned people
        if (person.age >= 70) {
          this.stats.ageGroups.unassigned.elderly++;
        } else if (person.age <= 12) {
          this.stats.ageGroups.unassigned.children++;
        } else {
          this.stats.ageGroups.unassigned.adults++;
        }
      } else {
        // Handle people who have been assigned to shelters - use age-specific icons
        if (person.age >= 70) {
          icon = this.icons.elderly; // Pink icon for elderly (age 70+)
          this.stats.ageGroups.assigned.elderly++;
        } else if (person.age <= 12) {
          icon = this.icons.child; // Green icon for children (age 0-12)
          this.stats.ageGroups.assigned.children++;
        } else {
          this.stats.ageGroups.assigned.adults++; // Blue icon for adults (age 13-69)
        }
      }

      // Create a marker for this person at their location
      const marker = L.marker([person.latitude, person.longitude], {
        icon,
        personId: person.id, // Always store the server's person ID
        isManual: person.isManual === true, // Set isManual flag from person data
      });

      // After creating the marker, output debug info for manual markers
      if (person.isManual) {
        console.log(
          `Created marker for manual person ID ${person.id} at coordinates [${person.latitude}, ${person.longitude}]`
        );
      }

      // If the person has been assigned to a shelter, draw a line and update statistics
      if (assignments[person.id]) {
        const assignment = assignments[person.id];
        const shelter = shelters.find((s) => s.id === assignment.shelterId);

        if (shelter) {
          // Update shelter usage statistics
          const shelterStat = this.stats.shelterUsage.find(
            (s) => s.id === shelter.id
          );
          if (shelterStat) {
            shelterStat.assigned++; // Increment the count of people assigned to this shelter
            shelterStat.percentUsed =
              (shelterStat.assigned / shelter.capacity) * 100; // Calculate percentage used
          }

          // Update DOM elements for this shelter's popup if they exist
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
            // Update status text and class based on how full the shelter is
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
          // If a calculated route is available, use that
          if (assignment.route && assignment.route.coordinates) {
            const routeLine = L.polyline(assignment.route.coordinates, {
              color: this.getLineColor(person.age), // Color based on age
              opacity: 0.7,
              weight: 3,
            });
            this.pathLines.addLayer(routeLine);
          } else {
            // If no detailed route is available, draw a simple straight line
            const line = L.polyline(
              [
                [person.latitude, person.longitude], // From person
                [shelter.latitude, shelter.longitude], // To shelter
              ],
              { color: this.getLineColor(person.age), opacity: 0.7, weight: 2 }
            );
            this.pathLines.addLayer(line);
          }

          // Update distance statistics
          totalDistance += assignment.distance; // Add to total for average calculation
          if (assignment.distance > this.stats.maxDistance) {
            this.stats.maxDistance = assignment.distance; // Update max if this is larger
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
        // For unassigned person, find and show the nearest shelter
        // (even though they can't get there in time)
        let nearestShelter = null;
        let nearestDistance = Infinity;

        // Check all shelters to find the closest one
        shelters.forEach((shelter) => {
          const distance = this.calculateAirDistance(
            person.latitude,
            person.longitude,
            shelter.latitude,
            shelter.longitude
          );

          if (distance < nearestDistance) {
            nearestDistance = distance;
            nearestShelter = shelter;
          }
        });

        // Create popup for unassigned person with nearest shelter info
        marker.bindPopup(`
        <p>Person #${person.id}</p>
        <p>Age: ${person.age}</p>
        <p>Status: <span class="status-unassigned">Unassigned</span></p>
        ${
          nearestShelter
            ? `
        <p>Nearest shelter: ${nearestShelter.name}</p>
        <p>Distance: ${nearestDistance.toFixed(2)} km</p>
        `
            : ""
        }
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
   * Displays summary statistics in the UI
   */
  updateStatisticsDisplay() {
    console.log("Updating statistics display with:", this.stats);

    // Update basic statistics elements if they exist
    const statsTotal = document.getElementById("stats-total");
    const statsAssigned = document.getElementById("stats-assigned");
    const statsUnassigned = document.getElementById("stats-unassigned");
    const statsAvgDistance = document.getElementById("stats-avg-distance");
    const statsMaxDistance = document.getElementById("stats-max-distance");

    // Update the basic count values
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

      // Check if we have shelter data to display
      if (this.stats.shelterUsage.length === 0) {
        shelterUsageContainer.innerHTML = "<p>No shelter data available</p>";
      } else {
        // Sort shelters by usage percentage (highest first)
        // Only include shelters with capacity > 0 to avoid division by zero
        const sortedShelters = [...this.stats.shelterUsage]
          .filter((shelter) => shelter.capacity > 0)
          .sort((a, b) => b.percentUsed - a.percentUsed);

        // Display statistics for each shelter
        sortedShelters.forEach((shelter) => {
          const shelterDiv = document.createElement("div");
          shelterDiv.className = "shelter-usage-item";

          // Determine status color based on occupancy
          let statusClass = "status-available";
          let color = "green";

          if (shelter.percentUsed >= 100) {
            statusClass = "status-full";
            color = "red";
          } else if (shelter.percentUsed >= 80) {
            statusClass = "status-almost-full";
            color = "orange";
          }

          // Create the shelter usage display with appropriate color
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

      // Calculate totals for percentage calculations
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

      // Define age categories for the table
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

      // Add a row for each age category
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

      // Add the completed table to the container
      ageStatsContainer.appendChild(table);
    }
  }

  /**
   * Updates the manual people list in the UI
   * Provides visual feedback and direct removal option for manual people
   */
  updateManualPeopleList() {
    const container = document.getElementById("manual-people-list");
    if (!container) {
      console.log("Manual people list container not found");
      return;
    }

    console.log(
      "Updating manual people list with",
      this.manualPeople ? this.manualPeople.length : 0,
      "people"
    );

    // Clear the current content
    container.innerHTML = "";

    // Check if we have any manual people
    if (!this.manualPeople || this.manualPeople.length === 0) {
      container.innerHTML = "<p>No manually added people</p>";
      return;
    }

    // Create list items for each manual person
    this.manualPeople.forEach((person, index) => {
      const item = document.createElement("div");
      item.className = "manual-person-item";
      item.style.cssText =
        "display: flex; justify-content: space-between; margin: 5px 0; padding: 5px; background: #f5f5f5; border-radius: 3px;";

      // Add ID to the display for debugging
      const displayId =
        typeof person.id === "number"
          ? `#${person.id}`
          : person.id.toString().substring(0, 8) + "...";

      item.innerHTML = `
      <span>Person #${index + 1} (Age: ${person.age})</span>
      <button class="remove-manual-person" data-id="${
        person.id
      }" style="background: #e74c3c; color: white; border: none; padding: 2px 5px; border-radius: 3px; cursor: pointer;">Remove</button>
    `;

      container.appendChild(item);

      // Add click handler to the button
      const removeBtn = item.querySelector(
        `.remove-manual-person[data-id="${person.id}"]`
      );
      if (removeBtn) {
        removeBtn.addEventListener("click", () => {
          console.log("Remove button clicked for:", person.id);
          console.log("this.manualPeople reference:", this.manualPeople);
          console.log("this context:", this);

          console.log("=== MANUAL REMOVE START ===");
          console.log(
            "Manual people before remove:",
            JSON.stringify(
              this.manualPeople.map((p) => ({ id: p.id, age: p.age }))
            )
          );

          // Find marker with this ID
          let markerToRemove = null;
          this.peopleMarkers.eachLayer((layer) => {
            if (layer.options && layer.options.personId === person.id) {
              markerToRemove = layer;
            }
          });

          // Remove marker if found
          if (markerToRemove) {
            this.peopleMarkers.removeLayer(markerToRemove);
          }

          // Remove from people array
          const idx = this.manualPeople.findIndex((p) => p.id === person.id);
          if (idx >= 0) {
            this.manualPeople.splice(idx, 1);

            console.log(
              "Manual people after remove:",
              JSON.stringify(
                this.manualPeople.map((p) => ({ id: p.id, age: p.age }))
              )
            );
          }
          console.log("=== MANUAL REMOVE END ===");

          // Update the list and button
          this.updateManualPeopleList();
          this.updateManualControlStatus();
        });
      }
    });
  }

  /**
   * Adds control panels to the map
   * Sets up UI elements for statistics and simulation controls
   * Note: This is not currently used in the new layout (controls are elsewhere)
   */
  addControlPanel() {
    // Create both control panels
    this.addStatisticsPanel(); // Panel showing simulation results
    this.addSimulationControlPanel(); // Panel with input controls
  }

  /**
   * Adds a statistics panel to the top-right corner of the map
   * Displays summary statistics about the simulation
   * Note: This is not currently used in the new layout
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
          <p>Shelter usage: <span id="stats-shelter-usage">0</span>%</p>
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
   * Note: This is not currently used in the new layout
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
            enableButton.textContent = "Stop Placing People";
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
    if (total === 0) return "0.0"; // Avoid division by zero
    return ((part / total) * 100).toFixed(1); // Format with one decimal place
  }

  /**
   * Returns a color for the line based on the person's age
   * Used to color-code paths on the map for better visibility
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

  /**
   * Enables or disables manual placement of people on the map
   * When enabled, users can click on the map to add custom people
   *
   * @param {boolean} enable - Whether to enable manual placement
   */
  enableManualPlacement(enable) {
    if (enable) {
      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent =
          "Click on the map to add people. Right-click to change age.";
        statusElement.className = "status-message running";
      }

      // Initialize manual people array if needed
      if (!this.manualPeople) {
        this.manualPeople = [];
      }

      // Add event listeners
      this.map.on("click", this.handleMapClick, this);
      this.map.on("contextmenu", this.handleContextClick, this);
    } else {
      // Remove event listeners
      this.map.off("click", this.handleMapClick, this);
      this.map.off("contextmenu", this.handleContextClick, this);

      // Clear status message
      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }
    }
  }

  /**
   * Clears all manually people count from the map
   * Restores the latest simulation if available
   */
  clearManualPeople() {
    console.log(
      "Clearing manual people. Before:",
      this.manualPeople?.length || 0
    );

    // Reset the manual people array
    this.manualPeople = [];

    // Remove manual markers from the map
    const markersToRemove = [];
    this.peopleMarkers.eachLayer((marker) => {
      if (marker.options?.isManual) {
        markersToRemove.push(marker);
      }
    });

    markersToRemove.forEach((marker) => {
      this.peopleMarkers.removeLayer(marker);
    });

    // Restore original simulation if available
    if (this.originalSimulationData) {
      this.visualizeSimulation(
        this.originalSimulationData.people,
        this.originalSimulationData.shelters,
        this.originalSimulationData.assignments
      );

      // Reset current data to original data
      this.currentSimulationData = {
        people: [...this.originalSimulationData.people],
        shelters: [...this.originalSimulationData.shelters],
        assignments: { ...this.originalSimulationData.assignments },
      };

      // Update age group statistics
      if (this.updateAgeGroupStatistics) {
        this.updateAgeGroupStatistics(
          this.currentSimulationData.people,
          this.currentSimulationData.assignments
        );
      }
    }

    // Update UI
    this.updateManualControlStatus();
    console.log("After clearing:", this.manualPeople.length);
  }

  /**
   * Handle map click to add a person at that location
   * Called when the user clicks on the map in manual placement mode
   */
  handleMapClick(e) {
    console.log("=== MANUAL ADD START ===");
    console.log("Manual people before add:", this.manualPeople?.length || 0);

    const lat = e.latlng.lat;
    const lng = e.latlng.lng;

    // Initialize manualPeople array if it doesn't exist
    if (!this.manualPeople) {
      this.manualPeople = [];
    }

    // Generate a unique ID for this manual person
    const id = `manual_${Date.now()}_${this.manualPeople.length}`;

    // Create a new person object (default to adult age 35)
    const person = {
      id: id,
      age: 35,
      latitude: lat,
      longitude: lng,
      isManual: true,
    };

    // Add to the array of manual people
    this.manualPeople.push(person);

    // Add marker for the person
    const icon = this.icons.person;
    const marker = L.marker([lat, lng], {
      icon,
      isManual: true,
      personId: id,
      interactive: true,
      zIndexOffset: 1000,
    });

    // Add popup with person information
    marker.bindPopup(`
    <p>Person #${this.manualPeople.length} (Manual)</p>
    <p>Age: ${person.age}</p>
    <p>Status: Unassigned</p>
    <button class="remove-marker" data-marker-id="${id}" style="background-color: #e74c3c; color: white; border: none; padding: 5px 10px; border-radius: 3px; cursor: pointer;">Remove Person</button>
  `);

    // Handle popup remove button
    marker.on("popupopen", (event) => {
      setTimeout(() => {
        const button = document.querySelector(
          `.remove-marker[data-marker-id="${id}"]`
        );
        if (button) {
          button.addEventListener("click", (e) => {
            console.log("Remove button clicked for marker:", id);

            // Remove from manualPeople array
            const index = this.manualPeople.findIndex((p) => p.id === id);
            if (index >= 0) {
              this.manualPeople.splice(index, 1);
            }

            // Remove marker from map
            this.peopleMarkers.removeLayer(marker);

            // Close popup
            this.map.closePopup();

            // Update button status
            this.updateManualControlStatus();
            this.updateManualPeopleList();

            e.stopPropagation();
          });
        }
      }, 100);
    });

    // Add the marker to the people layer
    this.peopleMarkers.addLayer(marker);

    // Update UI controls
    this.updateManualControlStatus();
    this.updateManualPeopleList();

    console.log("Manual people after add:", this.manualPeople.length);
    console.log("=== MANUAL ADD END ===");
  }

  /**
   * Handle right-click to change a person's age
   * Cycles through age groups: adult -> elderly -> child -> adult
   */
  handleContextClick(e) {
    const lat = e.latlng.lat;
    const lng = e.latlng.lng;

    // Find manual markers near the click
    this.peopleMarkers.eachLayer((layer) => {
      if (!layer.options?.isManual) return;

      const markerLat = layer.getLatLng().lat;
      const markerLng = layer.getLatLng().lng;

      // Check if click is close to this marker
      if (
        Math.abs(markerLat - lat) < 0.001 &&
        Math.abs(markerLng - lng) < 0.001
      ) {
        const personId = layer.options.personId;
        const personIndex = this.manualPeople.findIndex(
          (p) => p.id === personId
        );

        if (personIndex >= 0) {
          const person = this.manualPeople[personIndex];

          // Cycle through ages: adult -> elderly -> child -> adult
          if (person.age >= 70) {
            person.age = 8; // Elderly -> Child
          } else if (person.age <= 12) {
            person.age = 35; // Child -> Adult
          } else {
            person.age = 75; // Adult -> Elderly
          }

          // Update marker icon
          let newIcon;
          if (person.age >= 70) {
            newIcon = this.icons.elderly;
          } else if (person.age <= 12) {
            newIcon = this.icons.child;
          } else {
            newIcon = this.icons.person;
          }
          layer.setIcon(newIcon);

          // Update popup
          layer.setPopupContent(`
          <p>Person #${personIndex + 1} (Manual)</p>
          <p>Age: ${person.age}</p>
          <p>Status: Unassigned</p>
        `);

          console.log(`Changed person ${personId} age to ${person.age}`);
        }
      }
    });
  }

  /**
   * Enables or disables removal mode for people markers
   * @param {boolean} enable - Whether to enable removal mode
   */
  enableUniversalRemoval(enable) {
    if (this.removalModeActive && !enable) {
      // Clean up removal mode
      this.map.off("click", this.handleUniversalRemovalClick, this);
      this.removalModeActive = false;

      if (this.map.getContainer()) {
        this.map.getContainer().classList.remove("removal-mode");
      }

      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }
      return;
    }

    if (!this.removalModeActive && enable) {
      // Enable removal mode
      this.map.on("click", this.handleUniversalRemovalClick, this);
      this.removalModeActive = true;

      if (this.map.getContainer()) {
        this.map.getContainer().classList.add("removal-mode");
      }

      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent =
          "Click on any person to remove them from the map";
        statusElement.className = "status-message running";
      }
    }
  }

  addDirectRemovalButton() {
    if (document.getElementById("direct-clear-button")) {
      return;
    }

    const controlContainer = document.querySelector(".custom-controls");
    if (controlContainer) {
      // Create a direct removal button
      const button = document.createElement("button");
      button.id = "direct-clear-button";
      button.className = "control-button";
      button.style.backgroundColor = "#d9534f"; // Bootstrap danger red
      button.textContent = "EMERGENCY CLEAR ALL MANUAL PEOPLE";
      button.style.marginTop = "10px";

      // Add the button to the container
      controlContainer.appendChild(button);

      // Add click handler with direct reference to this instance
      const self = this; // Capture this for the event handler
      button.addEventListener("click", function () {
        console.log("=== DIRECT CLEAR START ===");
        console.log("Manual people before:", self.manualPeople?.length);

        // Create a fresh empty array to replace the existing one
        self.manualPeople = [];

        // Remove all markers with isManual flag
        const markersToRemove = [];
        self.peopleMarkers.eachLayer((layer) => {
          if (layer.options && layer.options.isManual) {
            markersToRemove.push(layer);
          }
        });

        markersToRemove.forEach((marker) => {
          self.peopleMarkers.removeLayer(marker);
        });

        console.log("Removed markers:", markersToRemove.length);
        console.log("Manual people after:", self.manualPeople?.length);

        // Update UI
        self.updateManualControlStatus();
        if (typeof self.updateManualPeopleList === "function") {
          self.updateManualPeopleList();
        }

        console.log("=== DIRECT CLEAR END ===");
      });
    }
  }

  /**
   * Handles clicks when in removal mode to remove any person
   * @param {Object} e - Click event object from Leaflet
   */
  handleUniversalRemovalClick(e) {
    const lat = e.latlng.lat;
    const lng = e.latlng.lng;
    const SEARCH_RADIUS = 0.0001;

    console.log("Handling universal removal click at:", lat, lng);

    let markersToRemove = [];

    // Find markers near the click point
    this.peopleMarkers.eachLayer((layer) => {
      const markerLat = layer.getLatLng().lat;
      const markerLng = layer.getLatLng().lng;

      const distance = Math.sqrt(
        Math.pow(markerLat - lat, 2) + Math.pow(markerLng - lng, 2)
      );

      if (distance < SEARCH_RADIUS) {
        markersToRemove.push(layer);
      }
    });

    console.log(`Found ${markersToRemove.length} markers to remove`);

    if (markersToRemove.length > 0) {
      const removedPersonIds = new Set();

      markersToRemove.forEach((marker) => {
        const personId = marker.options?.personId;

        if (personId) {
          // If this is a manual person, remove from manual array
          if (marker.options.isManual) {
            const manualIndex = this.manualPeople.findIndex(
              (p) => p.id === personId
            );
            if (manualIndex >= 0) {
              this.manualPeople.splice(manualIndex, 1);
              console.log(`Removed manual person #${personId}`);
            }
          }

          removedPersonIds.add(personId);
        }

        // Remove marker from map
        this.peopleMarkers.removeLayer(marker);
      });

      // Clear path lines
      this.pathLines.clearLayers();

      // Update current simulation data
      if (this.currentSimulationData?.people) {
        this.currentSimulationData.people =
          this.currentSimulationData.people.filter(
            (p) => !removedPersonIds.has(p.id)
          );

        // Update assignments
        if (this.currentSimulationData.assignments) {
          const newAssignments = {};
          for (const personId in this.currentSimulationData.assignments) {
            if (
              !removedPersonIds.has(parseInt(personId)) &&
              !removedPersonIds.has(personId)
            ) {
              newAssignments[personId] =
                this.currentSimulationData.assignments[personId];
            }
          }
          this.currentSimulationData.assignments = newAssignments;
        }
      }

      // Update UI
      this.updateManualPeopleList();
      this.updateManualControlStatus();

      // Show status message
      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent = `Removed ${markersToRemove.length} ${
          markersToRemove.length > 1 ? "people" : "person"
        }. Use "Run Updated Simulation" to update assignments.`;
        statusElement.className = "status-message success";
      }
    }
  }

  /**
   * Updates the manual control button status
   * Enables the button and shows the count when people are available
   */
  updateManualControlStatus() {
    // Call the global function to update button state
    if (typeof updateManualButtonState === "function") {
      updateManualButtonState();
    }

    // Update other manual people related UI
    this.updateManualPeopleList();
  }

  /**
   * Run simulation with manually placed people added to existing simulation
   * Preserves current shelters and only adds the manual people
   */
  runWithManualPeople() {
    console.log("=== RUN WITH MANUAL START ===");

    // IMPORTANT: Clear all path lines first
    this.pathLines.clearLayers();

    console.log(
      "Manual people array:",
      JSON.stringify(this.manualPeople.map((p) => ({ id: p.id, age: p.age })))
    );
    console.log(
      "originalSimulationData people count:",
      this.originalSimulationData?.people?.length
    );
    console.log(
      "currentSimulationData people count:",
      this.currentSimulationData?.people?.length
    );

    // Check if we have any manual people to add
    if (!this.manualPeople || this.manualPeople.length === 0) {
      alert("Please add some people to the map first");
      return;
    }

    // Update status message
    const statusElement = document.getElementById("simulation-status");
    if (statusElement) {
      statusElement.textContent = "Adding manual people to simulation...";
      statusElement.className = "status-message running";
    }

    // CRITICAL FIX: Always start from the ORIGINAL simulation data
    // NOT from the current simulation data which might contain manually added people
    if (!this.originalSimulationData) {
      console.error("No original simulation data found");
      this.runCustomServerSimulation(this.manualPeople);
      return;
    }

    const basePeople = [...this.originalSimulationData.people];
    const baseShelters = [...this.originalSimulationData.shelters];

    console.log(
      `Starting with original simulation data: ${basePeople.length} people, ${this.manualPeople.length} manual people`
    );

    // Create a new combined people array
    const allPeople = [...basePeople];

    // Find the highest ID currently in use to avoid ID conflicts
    let maxId = 0;
    basePeople.forEach((person) => {
      if (typeof person.id === "number" && person.id > maxId) {
        maxId = person.id;
      }
    });

    // Add ONLY the current manual people with sequential IDs
    this.manualPeople.forEach((person, index) => {
      const newPerson = {
        ...person,
        id: maxId + index + 1, // Assign new unique ID
        isManual: true, // Mark as manual for future reference
      };
      allPeople.push(newPerson);
    });

    console.log(`Final combined people count: ${allPeople.length}`);
    console.log(`- Base people: ${basePeople.length}`);
    console.log(`- Manual people: ${this.manualPeople.length}`);

    // Run server simulation with combined people
    this.runServerSimulationWithCustomData(allPeople, baseShelters);

    console.log("Combined people being sent to server:", allPeople.length);
    console.log("From base people:", basePeople.length);
    console.log("From manual people:", this.manualPeople.length);
    console.log("=== RUN WITH MANUAL END ===");
  }

  /**
   * Run server simulation with only the current remaining people on the map
   * This captures the exact current state after any combination of operations
   */
  async runWithCurrentPeople() {
    console.log("=== RUN WITH CURRENT PEOPLE START ===");
    const statusElement = document.getElementById("simulation-status");

    if (statusElement) {
      statusElement.textContent = "Running simulation with current people...";
      statusElement.className = "status-message running";
    }

    try {
      // Get parameters from UI controls
      const priorityEnabled =
        document.getElementById("priority").value === "true";
      const radius = parseFloat(document.getElementById("radius").value) || 0.5;

      // Build the people array directly from the markers currently on the map
      const currentPeople = [];
      const currentPeopleIds = new Set(); // Track IDs to prevent duplicates

      this.peopleMarkers.eachLayer((marker) => {
        if (marker.getLatLng && marker.options && marker.options.personId) {
          // Find the original person to get the age
          let personId = marker.options.personId;

          // Skip if we already added this person (prevent duplicates)
          if (currentPeopleIds.has(personId)) return;

          // Find person data in current simulation
          const originalPerson = this.currentSimulationData.people.find(
            (p) => p.id === personId
          );

          if (originalPerson) {
            // Create a person object from the current marker position
            currentPeople.push({
              id: personId,
              age: originalPerson.age,
              latitude: marker.getLatLng().lat,
              longitude: marker.getLatLng().lng,
              isManual: marker.options.isManual === true,
            });

            // Track this ID to prevent duplicates
            currentPeopleIds.add(personId);
          }
        }
      });

      // Use the original shelters without modification
      const currentShelters = [...this.currentSimulationData.shelters];

      console.log(`Running with people from map: ${currentPeople.length}`);
      console.log(`Using shelters: ${currentShelters.length}`);

      // Create request data with the people built from markers and the original shelters
      const requestData = {
        peopleCount: 0, // Don't generate random people
        shelterCount: 0, // Don't generate random shelters
        centerLatitude: 31.2518,
        centerLongitude: 34.7913,
        radiusKm: radius,
        prioritySettings: {
          enableAgePriority: priorityEnabled,
          childMaxAge: 12,
          elderlyMinAge: 70,
        },
        useCustomPeople: true,
        customPeople: currentPeople,
        useCustomShelters: true,
        customShelters: currentShelters,
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

      // Clean up the manual people array to stay in sync with what's in the new simulation
      if (this.manualPeople && this.manualPeople.length > 0) {
        // Keep only manual people that are still in the new people data
        const newPeopleIds = new Set(data.people.map((p) => p.id));
        this.manualPeople = this.manualPeople.filter((p) =>
          newPeopleIds.has(p.id)
        );

        console.log(
          `Updated manual people array: ${this.manualPeople.length} remain`
        );
      }

      // IMPORTANT: Clear all previous visualization data
      this.clearMap();
      this.pathLines.clearLayers();

      // Save the new data as the current simulation data
      this.currentSimulationData = data;

      // Visualize the new simulation results
      this.visualizeSimulation(data.people, data.shelters, data.assignments);

      // Update age group statistics explicitly
      updateAgeGroupStats(data.people, data.statistics, data.assignments);

      // Update manual people tracking to stay in sync with server response
      if (this.manualPeople && this.manualPeople.length > 0) {
        // Keep only manual people that are still in the new people data
        const newPeopleIds = new Set(data.people.map((p) => p.id));
        this.manualPeople = this.manualPeople.filter((p) =>
          newPeopleIds.has(p.id)
        );

        // Mark the correct manual people as manual in the current data
        const manualIds = new Set(this.manualPeople.map((p) => p.id));
        data.people.forEach((p) => {
          if (manualIds.has(p.id)) {
            p.isManual = true;
          }
        });
      }

      // Update status message
      if (statusElement) {
        statusElement.textContent = "Simulation with current people complete";
        statusElement.className = "status-message success";

        // Clear the status after a few seconds
        setTimeout(() => {
          statusElement.textContent = "";
          statusElement.className = "status-message";
        }, 3000);
      }

      // Update the manual people list and controls
      this.updateManualPeopleList();
      this.updateManualControlStatus();

      console.log("=== RUN WITH CURRENT PEOPLE END ===");
    } catch (error) {
      console.error("Error running with current people:", error);
      if (statusElement) {
        statusElement.textContent = `Error: ${error.message}`;
        statusElement.className = "status-message error";
      }
    }
  }
  /**
   * Visualizes server-side simulation with both original people and manually added people
   * Calls server with custom data and processes the response
   */
  async runServerSimulationWithCustomData(customPeople, customShelters) {
    const statusElement = document.getElementById("simulation-status");

    try {
      // Get parameters from UI controls
      const priorityEnabled =
        document.getElementById("priority").value === "true";
      const radius = parseFloat(document.getElementById("radius").value) || 0.5;

      // CRITICAL FIX: Print the exact people counts we're sending
      console.log(`Sending simulation request with:`);
      console.log(`- Custom people count: ${customPeople.length}`);
      console.log(`- Custom shelters count: ${customShelters.length}`);
      console.log(`- Manual people in array: ${this.manualPeople.length}`);

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

      // CRITICAL ADDITION: Track the mapping between original manual IDs and server-assigned IDs
      if (data.people && this.manualPeople && this.manualPeople.length > 0) {
        console.log("Tracking manual people ID mapping from server response");

        // Create a map of original manual IDs to their properties for lookup
        const originalManualMap = {};
        this.manualPeople.forEach((person) => {
          originalManualMap[person.id] = {
            age: person.age,
            latitude: person.latitude,
            longitude: person.longitude,
            isManual: true,
          };
        });

        // Find all manual people in the server response by matching coordinates
        // This is more reliable than matching by ID since IDs might have changed
        const manualPeopleFromServer = [];

        data.people.forEach((serverPerson) => {
          // Check if this server person matches any of our original manual people
          // by comparing coordinates (which should be the same)
          for (const originalId in originalManualMap) {
            const originalProps = originalManualMap[originalId];

            // Use approximate matching for coordinates with small tolerance
            // This handles tiny floating point differences that might occur
            const latMatch =
              Math.abs(serverPerson.latitude - originalProps.latitude) <
              0.0000001;
            const lonMatch =
              Math.abs(serverPerson.longitude - originalProps.longitude) <
              0.0000001;

            if (latMatch && lonMatch) {
              // This is one of our manual people - save the mapping
              console.log(
                `Found manual person match: Original ID ${originalId} → Server ID ${serverPerson.id}`
              );

              // Mark it as manual in server data
              serverPerson.isManual = true;

              // Add to our collection of manual people from server
              manualPeopleFromServer.push(serverPerson);

              // No need to check this server person against other originals
              break;
            }
          }
        });

        // Replace our manual people array with the server versions
        if (manualPeopleFromServer.length > 0) {
          console.log(
            `Updating manualPeople array with ${manualPeopleFromServer.length} server-assigned IDs`
          );
          this.manualPeople = manualPeopleFromServer;
        }
      }

      // Save the current simulation data for future reference
      this.currentSimulationData = data;

      // Display the results on the map
      this.visualizeSimulation(data.people, data.shelters, data.assignments);

      // Re-mark manual people with proper attributes
      if (data.people && this.manualPeople && this.manualPeople.length > 0) {
        console.log("Re-marking manual people after server response");

        // Update this.manualPeople to reflect what came back from the server
        const manualPeopleIds = this.manualPeople.map((p) => p.id);
        const serverReturnedManualPeople = data.people.filter(
          (p) => p.isManual || manualPeopleIds.includes(p.id)
        );

        if (serverReturnedManualPeople.length > 0) {
          console.log(
            `Found ${serverReturnedManualPeople.length} manual people in server response`
          );

          // Update our manual people array with server data
          this.manualPeople = serverReturnedManualPeople;

          // Mark markers as manual in the map
          this.peopleMarkers.eachLayer((marker) => {
            if (marker.options && marker.options.personId) {
              const isManual = this.manualPeople.some(
                (p) => p.id === marker.options.personId
              );
              if (isManual) {
                marker.options.isManual = true;
                console.log(
                  `Marked marker with ID ${marker.options.personId} as manual`
                );
              }
            }
          });
        }

        // Update the UI
        this.updateManualPeopleList();
      }

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
      // Handle errors
      console.error("Server simulation failed:", error);
      if (statusElement) {
        statusElement.textContent = `Error: ${error.message}`;
        statusElement.className = "status-message error";
      }
    }
  }

  /**
   * Run server simulation with custom people data
   * Uses the existing server API but with manually placed people
   *
   * @param {Array} customPeople - Array of manually placed people
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

      // Create request data with proper type handling
      const requestData = {
        peopleCount: 0, // Don't generate random people, use our custom ones
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
        `https://localhost:${PORT}/api/Simulation/run`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(requestData),
        }
      );

      // If the request failed, try to get more detailed error information
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
    }
  }

  refreshVisualization() {
    // Only proceed if we have current simulation data
    if (!this.currentSimulationData) {
      console.log("No current simulation data to refresh");
      return;
    }

    // Clear all existing visualization
    this.clearMap();

    // Re-visualize with current data
    this.visualizeSimulation(
      this.currentSimulationData.people,
      this.currentSimulationData.shelters,
      this.currentSimulationData.assignments
    );

    console.log("Visualization refreshed with current data");
  }
}

// Export the class for use in other modules (if in a module environment)
if (typeof module !== "undefined" && module.exports) {
  module.exports = ShelterSimulationVisualizer;
}

//==============================================================
// Run Server Simulation
//==============================================================

/**
 * Function to handle server-side simulation
 * Calls a remote API to run the simulation and visualizes the results
 * This is the main function that runs when users click "Run Simulation"
 */
async function runServerSimulation() {
  // Ensure we have a valid visualizer
  const visualizer = window.visualizer || initializeVisualizer();

  // Get the status element to show progress messages
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
      peopleCount: peopleCount, // Number of people to generate
      shelterCount: shelterCount, // Number of shelters to generate
      centerLatitude: 31.2518, // Beer Sheva latitude
      centerLongitude: 34.7913, // Beer Sheva longitude
      radiusKm: radius, // Radius to generate within
      prioritySettings: {
        enableAgePriority: priorityEnabled, // Whether to prioritize by age
        childMaxAge: 12, // Maximum age for child category
        elderlyMinAge: 70, // Minimum age for elderly category
      },
    };

    // Call the server API to run the simulation
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

    // Save the current simulation data in the visualizer for future reference
    visualizer.currentSimulationData = data;

    // Display the results on the map using the visualizer
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Update status message to indicate success
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

  updateAgeGroupStats(data.people, data.statistics, data.assignments);
}

/**
 * Unified "Run Updated Simulation" button to handle both additions and removals
 */
function setupUnifiedUpdateButton() {
  // Find or create the button
  let updateButton = document.getElementById("run-updated-simulation");

  if (!updateButton) {
    // Create a new button if it doesn't exist
    updateButton = document.createElement("button");
    updateButton.id = "run-updated-simulation";
    updateButton.className = "control-button";
    updateButton.style.backgroundColor = "#4285F4"; // Google blue
    updateButton.style.fontWeight = "bold";
    updateButton.textContent = "Run Updated Simulation";

    // Find where to insert it (after the run-after-removal button)
    const afterRemovalButton = document.getElementById("run-after-removal");
    if (afterRemovalButton && afterRemovalButton.parentNode) {
      afterRemovalButton.parentNode.insertBefore(
        updateButton,
        afterRemovalButton.nextSibling
      );
    } else {
      // Fallback insertion into the control container
      const controlContainer = document.querySelector(".control-container");
      if (controlContainer) {
        controlContainer.appendChild(updateButton);
      }
    }
  }

  // Add or update the click handler
  updateButton.addEventListener("click", function () {
    // First, disable any active modes
    const placementButton = document.getElementById("enable-placement");
    const removalButton = document.getElementById("enable-removal");

    if (placementButton && placementButton.classList.contains("active")) {
      placementButton.classList.remove("active");
      placementButton.textContent = "Place People Manually";
      if (window.visualizer) {
        window.visualizer.enableManualPlacement(false);
      }
    }

    if (removalButton && removalButton.classList.contains("active")) {
      removalButton.classList.remove("active");
      if (window.visualizer) {
        window.visualizer.enableUniversalRemoval(false);
      }
    }

    // Run the unified simulation update function
    if (window.visualizer) {
      window.visualizer.runUnifiedSimulationUpdate();
    }
  });

  return updateButton;
}

/**
 * Add the unified simulation update method to the visualizer class
 */
ShelterSimulationVisualizer.prototype.runUnifiedSimulationUpdate =
  async function () {
    console.log("=== UNIFIED SIMULATION UPDATE START ===");
    const statusElement = document.getElementById("simulation-status");

    if (statusElement) {
      statusElement.textContent =
        "Updating simulation with current configuration...";
      statusElement.className = "status-message running";
    }

    try {
      // Get all current people from the map (both manual and auto)
      const allCurrentPeople = [];
      const seenIds = new Set();

      // Get people from markers on the map
      this.peopleMarkers.eachLayer((marker) => {
        if (marker.getLatLng && marker.options && marker.options.personId) {
          const personId = marker.options.personId;

          // Skip duplicates
          if (seenIds.has(personId)) return;
          seenIds.add(personId);

          // Find original person data (for age)
          const originalPerson = this.currentSimulationData?.people?.find(
            (p) => p.id === personId
          );

          if (originalPerson) {
            allCurrentPeople.push({
              id: personId,
              age: originalPerson.age,
              latitude: marker.getLatLng().lat,
              longitude: marker.getLatLng().lng,
              isManual: marker.options.isManual === true,
            });
          }
        }
      });

      // Add any manually added people not yet on the map
      this.manualPeople.forEach((person) => {
        if (!seenIds.has(person.id)) {
          allCurrentPeople.push({ ...person });
          seenIds.add(person.id);
        }
      });

      // Use the current shelters without changes
      const currentShelters = [...this.currentSimulationData.shelters];

      // Get current simulation parameters
      const priorityEnabled =
        document.getElementById("priority").value === "true";
      const radius = parseFloat(document.getElementById("radius").value) || 0.5;

      console.log(
        `Running unified update with ${allCurrentPeople.length} people and ${currentShelters.length} shelters`
      );

      // Create the request with all current data
      const requestData = {
        peopleCount: 0,
        shelterCount: 0,
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
        useCustomShelters: true,
        customShelters: currentShelters,
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

      if (!response.ok) {
        throw new Error(
          `Server responded with ${response.status}: ${response.statusText}`
        );
      }

      // Process the response
      const data = await response.json();

      // Clear all existing visualization
      this.clearMap();
      this.pathLines.clearLayers();

      // Save the new current simulation data
      this.currentSimulationData = data;

      // Visualize the new simulation
      this.visualizeSimulation(data.people, data.shelters, data.assignments);

      // Update the manual people list to stay in sync
      if (this.manualPeople && this.manualPeople.length > 0) {
        const newIds = new Set(data.people.map((p) => p.id));
        this.manualPeople = this.manualPeople.filter((p) => newIds.has(p.id));

        // Mark the correct manual people as manual in the current data
        const manualIds = new Set(this.manualPeople.map((p) => p.id));
        data.people.forEach((p) => {
          if (manualIds.has(p.id)) {
            p.isManual = true;
          }
        });
      }

      // Update UI elements
      this.updateManualPeopleList();
      this.updateManualControlStatus();

      // Show success message
      if (statusElement) {
        statusElement.textContent = "Simulation updated successfully";
        statusElement.className = "status-message success";

        setTimeout(() => {
          statusElement.textContent = "";
          statusElement.className = "status-message";
        }, 3000);
      }

      console.log("=== UNIFIED SIMULATION UPDATE END ===");
    } catch (error) {
      console.error("Error updating simulation:", error);
      if (statusElement) {
        statusElement.textContent = `Error: ${error.message}`;
        statusElement.className = "status-message error";
      }
    }
  };

//==============================================================
// Update Server Statistics
//==============================================================

/**
 * Helper function to update statistics panel with server-provided data
 * Used when running server-side simulations
 *
 * @param {Object} stats - Statistics object from server response
 */
function updateServerStatistics(stats) {
  if (!stats) return; // Safety check - don't proceed without stats data

  // Update UI elements with statistics
  const statsTotal = document.getElementById("stats-total");
  const statsAssigned = document.getElementById("stats-assigned");
  const statsUnassigned = document.getElementById("stats-unassigned");
  const statsAvgDistance = document.getElementById("stats-avg-distance");
  const statsMaxDistance = document.getElementById("stats-max-distance");

  // Update the values in the DOM elements if they exist
  if (statsTotal)
    statsTotal.textContent = stats.assignedCount + stats.unassignedCount;
  if (statsAssigned) statsAssigned.textContent = stats.assignedCount;
  if (statsUnassigned) statsUnassigned.textContent = stats.unassignedCount;
  if (statsAvgDistance)
    statsAvgDistance.textContent = stats.averageDistance.toFixed(2);
  if (statsMaxDistance)
    statsMaxDistance.textContent = stats.maxDistance.toFixed(2);
}

// Export functions for use in other modules (if in a module environment)
if (typeof module !== "undefined" && module.exports) {
  module.exports = {
    runServerSimulation,
    updateServerStatistics,
  };
}
