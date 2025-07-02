/**
 * Google Maps API client for Find Cover application
 * Handles communication with the server-side Google Maps controller
 */

//old version
class GoogleMapsClient {
  constructor(baseUrl = "https://localhost", port = 7777) {
    this.baseUrl = `${baseUrl}:${port}/api/GoogleMaps`;
  }

  // //new version
  // class GoogleMapsClient {
  //   constructor(baseUrl = "https://localhost:7777/api/GoogleMaps") {
  //     this.baseUrl = baseUrl;
  //   }

  /**
   * Calculate walking distances between multiple origins and destinations
   * @param {Array} origins - Array of {latitude, longitude} objects
   * @param {Array} destinations - Array of {latitude, longitude} objects
   * @returns {Promise<Object>} Distance matrix response
   */
  async getDistanceMatrix(origins, destinations) {
    try {
      const response = await fetch(`${this.baseUrl}/distance-matrix`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          origins: origins.map((o) => ({
            latitude: o.latitude,
            longitude: o.longitude,
            id: o.id || null,
          })),
          destinations: destinations.map((d) => ({
            latitude: d.latitude,
            longitude: d.longitude,
            id: d.id || null,
          })),
          mode: "Walking",
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Error getting distance matrix:", error);
      throw error;
    }
  }

  /**
   * Get walking directions between two points
   * @param {Object} origin - {latitude, longitude}
   * @param {Object} destination - {latitude, longitude}
   * @returns {Promise<Object>} Directions response with route details
   */
  async getDirections(origin, destination) {
    try {
      const response = await fetch(`${this.baseUrl}/directions`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          origin: {
            latitude: origin.latitude,
            longitude: origin.longitude,
          },
          destination: {
            latitude: destination.latitude,
            longitude: destination.longitude,
          },
          mode: "Walking",
          alternatives: false,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Error getting directions:", error);
      throw error;
    }
  }

  /**
   * Calculate walking distances between people and shelters
   * @param {Array} people - Array of person objects
   * @param {Array} shelters - Array of shelter objects
   * @returns {Promise<Object>} Distance calculations for shelter assignment
   */
  async calculateShelterDistances(people, shelters) {
    try {
      const response = await fetch(`${this.baseUrl}/shelter-distances`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          people: people,
          shelters: shelters,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Error calculating shelter distances:", error);
      throw error;
    }
  }

  /**
   * Get walking time between two points
   * @param {Object} origin - {latitude, longitude}
   * @param {Object} destination - {latitude, longitude}
   * @returns {Promise<Object>} Walking time and distance
   */
  async getWalkingTime(origin, destination) {
    try {
      const params = new URLSearchParams({
        originLat: origin.latitude,
        originLng: origin.longitude,
        destLat: destination.latitude,
        destLng: destination.longitude,
      });

      const response = await fetch(`${this.baseUrl}/walking-time?${params}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Error getting walking time:", error);
      throw error;
    }
  }

  /**
   * Test Google Maps API connectivity
   * @returns {Promise<Object>} Test result
   */
  async testConnection() {
    try {
      const response = await fetch(`${this.baseUrl}/test`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Error testing Google Maps connection:", error);
      throw error;
    }
  }

  /**
   * Draw route on Leaflet map
   * @param {Object} map - Leaflet map instance
   * @param {Object} route - Route object from directions API
   * @param {Object} options - Drawing options
   */
  drawRouteOnMap(map, route, options = {}) {
    const defaultOptions = {
      color: "#4169e1",
      weight: 4,
      opacity: 0.7,
      smoothFactor: 1,
    };

    const drawOptions = { ...defaultOptions, ...options };

    // Decode the polyline if using overview_polyline
    if (route.overviewPolyline) {
      const decodedPath = this.decodePolyline(route.overviewPolyline);
      return L.polyline(decodedPath, drawOptions).addTo(map);
    }

    // Otherwise draw from legs and steps
    if (route.legs && route.legs.length > 0) {
      const coordinates = [];

      route.legs.forEach((leg) => {
        if (leg.steps) {
          leg.steps.forEach((step) => {
            if (step.startLocation) {
              coordinates.push([
                step.startLocation.latitude,
                step.startLocation.longitude,
              ]);
            }
            if (step.endLocation) {
              coordinates.push([
                step.endLocation.latitude,
                step.endLocation.longitude,
              ]);
            }
          });
        }
      });

      if (coordinates.length > 0) {
        return L.polyline(coordinates, drawOptions).addTo(map);
      }
    }

    return null;
  }

  /**
   * Decode Google's encoded polyline format
   * @param {string} encoded - Encoded polyline string
   * @returns {Array} Array of [lat, lng] coordinates
   */
  decodePolyline(encoded) {
    const points = [];
    let index = 0;
    let lat = 0;
    let lng = 0;

    while (index < encoded.length) {
      let shift = 0;
      let result = 0;
      let byte;

      do {
        byte = encoded.charCodeAt(index++) - 63;
        result |= (byte & 0x1f) << shift;
        shift += 5;
      } while (byte >= 0x20);

      const dlat = result & 1 ? ~(result >> 1) : result >> 1;
      lat += dlat;

      shift = 0;
      result = 0;

      do {
        byte = encoded.charCodeAt(index++) - 63;
        result |= (byte & 0x1f) << shift;
        shift += 5;
      } while (byte >= 0x20);

      const dlng = result & 1 ? ~(result >> 1) : result >> 1;
      lng += dlng;

      points.push([lat / 1e5, lng / 1e5]);
    }

    return points;
  }
}

// =====================================================
// Integration with existing simulation code
// =====================================================

/**
 * Enhanced function to run simulation with real walking distances
 */
async function runServerSimulationWithWalkingDistances() {
  const statusElement = document.getElementById("simulation-status");

  if (statusElement) {
    statusElement.textContent =
      "Running simulation with real walking distances...";
    statusElement.className = "status-message running";
  }

  try {
    // Get simulation parameters
    const peopleCount =
      parseInt(document.getElementById("people-count").value) || 1;
    const shelterCount =
      parseInt(document.getElementById("shelter-count").value) || 1;
    const radius = parseFloat(document.getElementById("radius").value) || 0.5;
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

    console.log("Calling server to run simulation with walking distances...");

    //Call the server endpoint that handles EVERYTHING including Google Maps
    //old version
    const response = await fetch(
      `https://localhost:${PORT}/api/Simulation/run-with-walking-distances`,
      // //new version
      // const response = await fetch(
      //   `https://proj.ruppin.ac.il/igroup18/test2/tar1/api/Simulation/run-with-walking-distances`,
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
    console.log("Simulation complete with walking distances!");
    console.log("Response data:", data);

    // Check a sample assignment to verify it has walking distances
    if (data.assignments) {
      const firstPersonId = Object.keys(data.assignments)[0];
      if (firstPersonId) {
        console.log(
          `Sample - Person ${firstPersonId}:`,
          data.assignments[firstPersonId]
        );
      }
    }

    // Display results on map
    const visualizer = window.visualizer || initializeVisualizer();
    visualizer.clearMap();
    visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    // Update statistics
    updateStatistics(data.statistics, data.people, data.assignments);

    if (statusElement) {
      statusElement.textContent = "Simulation complete with walking distances";
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
// =====================================================
// Example usage for getting walking directions
// =====================================================

async function showWalkingDirections(
  personLat,
  personLng,
  shelterLat,
  shelterLng
) {
  //old version
  const googleMapsClient = new GoogleMapsClient("https://localhost", PORT);

  //new version
  // const googleMapsClient = new GoogleMapsClient(
  //   "https://proj.ruppin.ac.il/igroup18/test2/tar1/api/GoogleMaps"
  // );

  try {
    const directions = await googleMapsClient.getDirections(
      { latitude: personLat, longitude: personLng },
      { latitude: shelterLat, longitude: shelterLng }
    );

    if (directions.success && directions.routes.length > 0) {
      const route = directions.routes[0];
      const leg = route.legs[0];

      console.log(`Walking distance: ${leg.distance.text}`);
      console.log(`Walking time: ${leg.duration.text}`);
      console.log(`Steps: ${leg.steps.length}`);

      // Draw the route on the map if you have a map instance
      if (window.visualizer && window.visualizer.map) {
        googleMapsClient.drawRouteOnMap(window.visualizer.map, route, {
          color: "#ff4500",
          weight: 5,
        });
      }

      return leg;
    }
  } catch (error) {
    console.error("Error getting walking directions:", error);
  }
}

// Export for use in other modules
if (typeof module !== "undefined" && module.exports) {
  module.exports = {
    GoogleMapsClient,
    runServerSimulationWithWalkingDistances,
  };
}
