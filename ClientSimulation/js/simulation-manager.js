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

  // Create a new visualizer instance
  globalVisualizer = new ShelterSimulationVisualizer("map");
  return globalVisualizer;
}

/**
 * Function to handle server-side simulation
 * Calls a remote API to run the simulation and visualizes the results
 */
async function runServerSimulation() {
  // Ensure we have a valid visualizer
  const visualizer = initializeVisualizer();

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

    // Clear any existing data
    visualizer.clearMap();

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
 * Initialization code for the application
 * Creates the visualizer instance and sets up the application when the DOM is ready
 */
document.addEventListener("DOMContentLoaded", function () {
  // Initialize the visualizer
  initializeVisualizer();

  // Set up the event listener for the run button
  const runButton = document.getElementById("run-simulation");
  if (runButton) {
    // Remove any existing event listeners
    const newRunButton = runButton.cloneNode(true);
    runButton.parentNode.replaceChild(newRunButton, runButton);

    // Add the event listener to the new button
    newRunButton.addEventListener("click", runServerSimulation);
  }

  console.log("Shelter simulation visualizer initialized");
});
