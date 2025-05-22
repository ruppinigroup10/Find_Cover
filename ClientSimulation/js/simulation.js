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
    this.placementModeActive = false; // Track if placement mode is active
    this.nextManualPersonId = 10000; // Start manual IDs at a high number to avoid conflicts
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
      // Manual person icon - purple circular dot with border
      manualPerson: L.divIcon({
        className: "marker-manual-person",
        html: '<div style="background-color: #9370DB; border: 2px solid #4B0082; border-radius: 50%; width: 12px; height: 12px;"></div>',
        iconSize: [16, 16],
        iconAnchor: [8, 8],
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

      // Handle manual people differently
      if (person.isManual) {
        icon = this.icons.manualPerson;
      } else if (!assignments[person.id]) {
        // Handle people who haven't been assigned to a shelter
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
              color: this.getLineColor(person.age, person.isManual), // Color based on age and manual status
              opacity: 0.7,
              weight: 3,
              dashArray: person.isManual ? "5, 5" : null, // Dashed line for manual people
            });
            this.pathLines.addLayer(routeLine);
          } else {
            // If no detailed route is available, draw a simple straight line
            const line = L.polyline(
              [
                [person.latitude, person.longitude], // From person
                [shelter.latitude, shelter.longitude], // To shelter
              ],
              {
                color: this.getLineColor(person.age, person.isManual),
                opacity: 0.7,
                weight: 2,
                dashArray: person.isManual ? "5, 5" : null, // Dashed line for manual people
              }
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
        <p>Person #${person.id} ${person.isManual ? "(Manual)" : ""}</p>
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
        <p>Person #${person.id} ${person.isManual ? "(Manual)" : ""}</p>
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
      ["", "Assigned", "Unassigned", "% Assigned"].forEach((header) => {
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

        // Add assigned cell
        const assignedCell = document.createElement("td");
        assignedCell.textContent = category.assigned;
        row.appendChild(assignedCell);

        // Add unassigned cell
        const unassignedCell = document.createElement("td");
        unassignedCell.textContent = category.unassigned;
        row.appendChild(unassignedCell);

        // Add percentage cell
        const percentCell = document.createElement("td");
        const total = category.assigned + category.unassigned;
        const percent =
          total > 0 ? Math.round((category.assigned / total) * 100) : 0;
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

      item.innerHTML = `
      <span>Person #${person.id} (Age: ${person.age})</span>
      <button class="remove-manual-person" data-id="${person.id}" style="background: #e74c3c; color: white; border: none; padding: 2px 5px; border-radius: 3px; cursor: pointer;">Remove</button>
    `;

      container.appendChild(item);

      // Add click handler to the button
      const removeBtn = item.querySelector(
        `.remove-manual-person[data-id="${person.id}"]`
      );
      if (removeBtn) {
        removeBtn.addEventListener("click", () => {
          console.log("Remove button clicked for:", person.id);
          this.removeManualPerson(person.id);
        });
      }
    });
  }

  /**
   * Removes a manually added person
   * @param {number|string} personId - ID of the person to remove
   */
  removeManualPerson(personId) {
    console.log("Removing manual person:", personId);

    // Find and remove the marker
    let markerToRemove = null;
    this.peopleMarkers.eachLayer((layer) => {
      if (layer.options && layer.options.personId == personId) {
        markerToRemove = layer;
      }
    });

    if (markerToRemove) {
      this.peopleMarkers.removeLayer(markerToRemove);
    }

    // Remove from manual people array
    const idx = this.manualPeople.findIndex((p) => p.id == personId);
    if (idx >= 0) {
      this.manualPeople.splice(idx, 1);
    }

    // Update UI
    this.updateManualPeopleList();
    this.updateManualControlStatus();
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
   * Returns a color for the line based on the person's age and manual status
   * Used to color-code paths on the map for better visibility
   *
   * @param {number} age - Age of the person
   * @param {boolean} isManual - Whether the person was manually added
   * @returns {string} - CSS color string
   */
  getLineColor(age, isManual = false) {
    if (isManual) {
      return "#9370DB"; // Purple for manual people
    }
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
    this.placementModeActive = enable;
    this.distancePlacementMode = false;

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
      this.map.on("contextmenu", this.handleRightClick, this);

      // Check if distance mode is enabled
      this.setupDistancePlacement();
    } else {
      // Remove event listeners
      this.map.off("click", this.handleMapClick, this);
      this.map.off("contextmenu", this.handleRightClick, this);

      // Disable distance placement
      this.disableShelterSelection();

      // Clear status message
      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }
    }
  }

  /**
   * Setup distance placement mode
   */
  setupDistancePlacement() {
    const distanceCheckbox = document.getElementById("enable-distance-mode");
    const distanceOptions = document.getElementById("distance-options");

    if (distanceCheckbox) {
      // Remove any existing listeners
      distanceCheckbox.onchange = null;

      // Add new listener
      distanceCheckbox.onchange = () => {
        this.distancePlacementMode = distanceCheckbox.checked;
        if (distanceOptions) {
          distanceOptions.style.display = distanceCheckbox.checked
            ? "block"
            : "none";
        }

        if (this.distancePlacementMode) {
          this.enableShelterSelection();
          const statusElement = document.getElementById("simulation-status");
          if (statusElement) {
            statusElement.textContent =
              "Distance mode ON: Click on a shelter to place person at set distance";
            statusElement.className = "status-message running";
          }
        } else {
          this.disableShelterSelection();
          const statusElement = document.getElementById("simulation-status");
          if (statusElement) {
            statusElement.textContent =
              "Click on the map to add people. Right-click to change age.";
            statusElement.className = "status-message running";
          }
        }
      };
    }
  }

  /**
   * Enable shelter selection for distance-based placement
   */
  enableShelterSelection() {
    this.shelterMarkers.eachLayer((marker) => {
      marker.on("click", this.handleShelterClick, this);
      // Add visual feedback
      if (marker._icon) {
        marker._icon.style.cursor = "crosshair";
        marker._icon.classList.add("shelter-selectable");
      }
    });
  }

  /**
   * Disable shelter selection
   */
  disableShelterSelection() {
    this.shelterMarkers.eachLayer((marker) => {
      marker.off("click", this.handleShelterClick, this);
      // Remove visual feedback
      if (marker._icon) {
        marker._icon.style.cursor = "";
        marker._icon.classList.remove("shelter-selectable");
      }
    });
  }

  /**
   * Handle shelter click for distance-based placement
   */
  handleShelterClick(e) {
    if (!this.placementModeActive || !this.distancePlacementMode) return;

    // Prevent event from bubbling to map
    L.DomEvent.stopPropagation(e);

    const shelterLatLng = e.target.getLatLng();
    const shelterId = this.findShelterIdByPosition(shelterLatLng);

    if (!shelterId) return;

    const shelter = this.currentSimulationData.shelters.find(
      (s) => s.id === shelterId
    );
    if (!shelter) return;

    // Get values from UI
    const distance =
      parseFloat(document.getElementById("placement-distance")?.value) || 0.5;
    const age = parseInt(document.getElementById("placement-age")?.value) || 35;

    // Random direction
    const direction = Math.floor(Math.random() * 360);

    // Place the person
    this.placePersonAtDistance(shelter, distance, direction, age);
  }

  /**
   * Find shelter ID by its position
   */
  findShelterIdByPosition(latLng) {
    for (let shelter of this.currentSimulationData.shelters) {
      if (
        Math.abs(shelter.latitude - latLng.lat) < 0.0001 &&
        Math.abs(shelter.longitude - latLng.lng) < 0.0001
      ) {
        return shelter.id;
      }
    }
    return null;
  }

  /**
   * Place a person at a specific distance from a shelter
   */
  placePersonAtDistance(shelter, distanceKm, directionDegrees, age) {
    // Convert direction to radians
    const directionRad = (directionDegrees * Math.PI) / 180;

    // Calculate the new position
    // Earth's radius in km
    const R = 6371;

    // Convert distance to angular distance
    const angularDistance = distanceKm / R;

    // Convert shelter position to radians
    const lat1Rad = (shelter.latitude * Math.PI) / 180;
    const lon1Rad = (shelter.longitude * Math.PI) / 180;

    // Calculate new position using spherical coordinates
    const lat2Rad = Math.asin(
      Math.sin(lat1Rad) * Math.cos(angularDistance) +
        Math.cos(lat1Rad) * Math.sin(angularDistance) * Math.cos(directionRad)
    );

    const lon2Rad =
      lon1Rad +
      Math.atan2(
        Math.sin(directionRad) * Math.sin(angularDistance) * Math.cos(lat1Rad),
        Math.cos(angularDistance) - Math.sin(lat1Rad) * Math.sin(lat2Rad)
      );

    // Convert back to degrees
    const lat2 = (lat2Rad * 180) / Math.PI;
    const lon2 = (lon2Rad * 180) / Math.PI;

    // Create the person at the calculated position
    const id = this.nextManualPersonId++;

    const person = {
      id: id,
      age: age,
      latitude: lat2,
      longitude: lon2,
      isManual: true,
      placedFromShelterId: shelter.id,
      placedDistance: distanceKm,
    };

    // Add to manual people array
    this.manualPeople.push(person);

    // Create marker
    const icon = this.icons.manualPerson;
    const marker = L.marker([lat2, lon2], {
      icon,
      isManual: true,
      personId: id,
      interactive: true,
      zIndexOffset: 1000,
    });

    // Add enhanced popup
    marker.bindPopup(`
      <p>Person #${id} (Manual)</p>
      <p>Age: ${person.age}</p>
      <p>Placed ${distanceKm}km from Shelter ${shelter.id}</p>
      <p>Direction: ${directionDegrees}°</p>
      <p>Status: Not yet assigned</p>
    `);

    // Add the marker to the people layer
    this.peopleMarkers.addLayer(marker);

    // Draw a helper line showing the distance
    const helperLine = L.polyline(
      [
        [shelter.latitude, shelter.longitude],
        [lat2, lon2],
      ],
      {
        color: "#9370DB",
        weight: 1,
        opacity: 0.5,
        dashArray: "3, 6",
      }
    );
    this.pathLines.addLayer(helperLine);

    // Remove helper line after 3 seconds
    setTimeout(() => {
      this.pathLines.removeLayer(helperLine);
    }, 3000);

    // Update UI
    this.updateManualControlStatus();
    this.updateManualPeopleList();

    // Show success message
    const statusElement = document.getElementById("simulation-status");
    if (statusElement) {
      statusElement.textContent = `Placed person ${id} at ${distanceKm}km from Shelter ${shelter.id}`;
      statusElement.className = "status-message success";

      setTimeout(() => {
        statusElement.textContent =
          "Distance mode ON: Click on a shelter to place person at set distance";
        statusElement.className = "status-message running";
      }, 2000);
    }

    console.log(
      `Placed person ${id} at ${distanceKm}km from shelter ${shelter.id}`
    );
  }

  /**
   * Clears all manually people from the map
   * Keeps the latest simulation intact
   */
  clearManualPeople() {
    console.log(
      "Clearing manual people. Before:",
      this.manualPeople?.length || 0
    );

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

    // Reset the manual people array
    this.manualPeople = [];

    // Update UI
    this.updateManualControlStatus();
    this.updateManualPeopleList();

    console.log("After clearing:", this.manualPeople.length);
  }

  /**
   * Handle map click to add a person at that location
   * Called when the user clicks on the map in manual placement mode
   */
  handleMapClick(e) {
    if (!this.placementModeActive) return;

    // Check if click is on empty area (not on a shelter)
    const clickPoint = e.latlng;
    let clickedOnShelter = false;

    this.shelterMarkers.eachLayer((marker) => {
      const shelterLatLng = marker.getLatLng();
      const distance = this.calculateAirDistance(
        clickPoint.lat,
        clickPoint.lng,
        shelterLatLng.lat,
        shelterLatLng.lng
      );
      if (distance < 0.01) {
        // Very close to shelter marker
        clickedOnShelter = true;
      }
    });

    // If clicked on shelter, let the shelter handler deal with it
    if (clickedOnShelter) return;

    console.log("=== MANUAL ADD START ===");
    console.log("Manual people before add:", this.manualPeople?.length || 0);

    const lat = e.latlng.lat;
    const lng = e.latlng.lng;

    // Initialize manualPeople array if it doesn't exist
    if (!this.manualPeople) {
      this.manualPeople = [];
    }

    // Generate a unique ID for this manual person
    const id = this.nextManualPersonId++;

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
    const icon = this.icons.manualPerson;
    const marker = L.marker([lat, lng], {
      icon,
      isManual: true,
      personId: id,
      interactive: true,
      zIndexOffset: 1000,
    });

    // Add popup with person information
    marker.bindPopup(`
    <p>Person #${id} (Manual)</p>
    <p>Age: ${person.age}</p>
    <p>Status: Not yet assigned</p>
  `);

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
  handleRightClick(e) {
    if (!this.placementModeActive) return;

    L.DomEvent.preventDefault(e);

    const lat = e.latlng.lat;
    const lng = e.latlng.lng;

    // Find manual markers near the click
    let closestMarker = null;
    let closestDistance = Infinity;

    this.peopleMarkers.eachLayer((layer) => {
      if (!layer.options?.isManual) return;

      const markerLatLng = layer.getLatLng();
      const distance = Math.sqrt(
        Math.pow(markerLatLng.lat - lat, 2) +
          Math.pow(markerLatLng.lng - lng, 2)
      );

      if (distance < closestDistance && distance < 0.001) {
        closestDistance = distance;
        closestMarker = layer;
      }
    });

    if (closestMarker) {
      const personId = closestMarker.options.personId;
      const person = this.manualPeople.find((p) => p.id === personId);

      if (person) {
        // Cycle through ages: adult -> elderly -> child -> adult
        if (person.age >= 70) {
          person.age = 8; // Elderly -> Child
        } else if (person.age <= 12) {
          person.age = 35; // Child -> Adult
        } else {
          person.age = 75; // Adult -> Elderly
        }

        // Update popup
        closestMarker.setPopupContent(`
          <p>Person #${personId} (Manual)</p>
          <p>Age: ${person.age}</p>
          <p>Status: Not yet assigned</p>
        `);

        console.log(`Changed person ${personId} age to ${person.age}`);
      }
    }
  }

  /**
   * Enables or disables removal mode for people markers
   * @param {boolean} enable - Whether to enable removal mode
   */
  enableUniversalRemoval(enable) {
    if (this.removalModeActive === enable) return;

    this.removalModeActive = enable;

    if (enable) {
      // Enable removal mode
      this.map.on("click", this.handleUniversalRemovalClick, this);

      if (this.map.getContainer()) {
        this.map.getContainer().classList.add("removal-mode");
      }

      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent =
          "Click on any person to remove them from the map";
        statusElement.className = "status-message running";
      }
    } else {
      // Disable removal mode
      this.map.off("click", this.handleUniversalRemovalClick, this);

      if (this.map.getContainer()) {
        this.map.getContainer().classList.remove("removal-mode");
      }

      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }
    }
  }

  /**
   * Handles clicks when in removal mode to remove any person
   * @param {Object} e - Click event object from Leaflet
   */
  handleUniversalRemovalClick(e) {
    if (!this.removalModeActive) return;

    const lat = e.latlng.lat;
    const lng = e.latlng.lng;
    const SEARCH_RADIUS = 0.0005;

    console.log("Handling universal removal click at:", lat, lng);

    let closestMarker = null;
    let closestDistance = Infinity;

    // Find the closest marker to the click point
    this.peopleMarkers.eachLayer((layer) => {
      const markerLatLng = layer.getLatLng();
      const distance = Math.sqrt(
        Math.pow(markerLatLng.lat - lat, 2) +
          Math.pow(markerLatLng.lng - lng, 2)
      );

      if (distance < closestDistance && distance < SEARCH_RADIUS) {
        closestDistance = distance;
        closestMarker = layer;
      }
    });

    if (closestMarker) {
      const personId = closestMarker.options?.personId;
      console.log(`Removing person #${personId}`);

      // If this is a manual person, remove from manual array
      if (closestMarker.options.isManual) {
        const manualIndex = this.manualPeople.findIndex(
          (p) => p.id == personId
        );
        if (manualIndex >= 0) {
          this.manualPeople.splice(manualIndex, 1);
          console.log(`Removed manual person #${personId}`);
        }
      }

      // Remove marker from map
      this.peopleMarkers.removeLayer(closestMarker);

      // Update current simulation data
      if (this.currentSimulationData?.people) {
        this.currentSimulationData.people =
          this.currentSimulationData.people.filter((p) => p.id != personId);
      }

      // Update UI
      this.updateManualPeopleList();
      this.updateManualControlStatus();

      // Show status message
      const statusElement = document.getElementById("simulation-status");
      if (statusElement) {
        statusElement.textContent = `Removed person #${personId}. Use "Run Simulation After Removal" to update assignments.`;
        statusElement.className = "status-message success";
      }
    }
  }

  /**
   * Updates the manual control button status
   * Enables the button and shows the count when people are available
   */
  updateManualControlStatus() {
    const manualButton = document.getElementById("run-with-manual");
    if (!manualButton) return;

    const manualCount = this.manualPeople?.length || 0;

    if (manualCount > 0) {
      manualButton.textContent = `Run With Manual People (${manualCount})`;
      manualButton.disabled = false;
    } else {
      manualButton.textContent = "Run With Manual People (0)";
      manualButton.disabled = true;
    }
  }

  /**
   * Updates age group statistics
   */
  updateAgeGroupStatistics(people, assignments) {
    // Reset age group statistics
    this.stats.ageGroups = {
      assigned: { elderly: 0, children: 0, adults: 0 },
      unassigned: { elderly: 0, children: 0, adults: 0 },
    };

    // Count age groups
    people.forEach((person) => {
      const isAssigned = assignments && assignments[person.id];
      const ageCategory =
        person.age >= 70 ? "elderly" : person.age <= 12 ? "children" : "adults";

      if (isAssigned) {
        this.stats.ageGroups.assigned[ageCategory]++;
      } else {
        this.stats.ageGroups.unassigned[ageCategory]++;
      }
    });

    // Update the statistics display
    this.updateStatisticsDisplay();
  }

  /**
   * Run simulation with all current people on the map (both original and manual)
   * This is the unified method that handles all simulation updates
   */
  async runUnifiedSimulationUpdate() {
    console.log("=== UNIFIED SIMULATION UPDATE START ===");
    const statusElement = document.getElementById("simulation-status");

    if (statusElement) {
      statusElement.textContent =
        "Running simulation with current configuration...";
      statusElement.className = "status-message running";
    }

    try {
      // Get parameters from UI controls
      const priorityEnabled =
        document.getElementById("priority").value === "true";
      const radius = parseFloat(document.getElementById("radius").value) || 0.5;

      // Build the people array from current map state
      const allCurrentPeople = [];
      const seenIds = new Set();

      // Get all people currently on the map
      this.peopleMarkers.eachLayer((marker) => {
        if (marker.getLatLng && marker.options && marker.options.personId) {
          const personId = marker.options.personId;

          // Skip duplicates
          if (seenIds.has(personId)) return;
          seenIds.add(personId);

          // Find person data
          let personData = null;

          // Check in current simulation data first
          if (this.currentSimulationData?.people) {
            personData = this.currentSimulationData.people.find(
              (p) => p.id == personId
            );
          }

          // If not found and it's manual, check manual people array
          if (!personData && marker.options.isManual) {
            personData = this.manualPeople.find((p) => p.id == personId);
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

      // Get current shelters
      const currentShelters = this.currentSimulationData?.shelters || [];

      console.log(
        `Running simulation with ${allCurrentPeople.length} people and ${currentShelters.length} shelters`
      );

      // Create request data
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
        customShelters:
          currentShelters.length > 0 ? currentShelters : undefined,
        useDatabaseShelters:
          document.getElementById("use-database-shelters")?.checked || false,
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

      // Update manual people array to match what came back from server
      const newManualPeople = [];
      data.people.forEach((person) => {
        if (
          person.isManual ||
          this.manualPeople.some((mp) => mp.id == person.id)
        ) {
          newManualPeople.push(person);
        }
      });
      this.manualPeople = newManualPeople;

      // Save and visualize the new simulation
      this.currentSimulationData = data;
      this.visualizeSimulation(data.people, data.shelters, data.assignments);

      // Update UI
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
  }
}

// Export the class for use in other modules (if in a module environment)
if (typeof module !== "undefined" && module.exports) {
  module.exports = ShelterSimulationVisualizer;
}
