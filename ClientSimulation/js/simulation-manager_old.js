//Add custom debugging utility

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

  // Add a test button if needed
  const addTestButton = function () {
    if (document.getElementById("debug-test-button")) return;

    const button = document.createElement("button");
    button.id = "debug-test-button";
    button.className = "control-button";
    button.style.backgroundColor = "#ff9800";
    button.style.color = "white";
    button.textContent = "Run Test Request";

    button.addEventListener("click", function () {
      testServerRequest();
    });

    const container = document.querySelector(".control-container");
    if (container) {
      container.appendChild(button);
    }
  };

  addTestButton();
};

// Simple test function to verify server communication
function testServerRequest() {
  const testRequest = {
    peopleCount: 5,
    shelterCount: 3,
    centerLatitude: 31.2518,
    centerLongitude: 34.7913,
    radiusKm: 0.5,
    prioritySettings: {
      enableAgePriority: true,
      childMaxAge: 12,
      elderlyMinAge: 70,
    },
  };

  console.log("Sending test request:", testRequest);

  fetch("https://localhost:7094/api/Simulation/run", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(testRequest),
  })
    .then((response) => {
      console.log("Response status:", response.status);
      return response.text().then((text) => {
        try {
          return JSON.parse(text);
        } catch (e) {
          console.log("Raw response text:", text);
          throw new Error("Failed to parse JSON: " + e);
        }
      });
    })
    .then((data) => {
      console.log("Test succeeded with response:", data);
      alert("Server test successful!");
    })
    .catch((error) => {
      console.error("Test failed:", error);
      alert("Server test failed: " + error.message);
    });
}

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
// In simulation-manager.js - update the DOMContentLoaded event handler
document.addEventListener("DOMContentLoaded", function () {
  // Add a small delay to ensure all elements are fully rendered
  setTimeout(() => {
    // Initialize the map
    window.visualizer = initializeVisualizer();

    // If map initialized successfully, set up event handlers
    if (window.visualizer) {
      // Set up the event listener for the run button
      const runButton = document.getElementById("run-simulation");
      if (runButton) {
        // First remove any existing event listeners to avoid duplicates
        const newRunButton = runButton.cloneNode(true);
        runButton.parentNode.replaceChild(newRunButton, runButton);

        newRunButton.addEventListener("click", function () {
          // Run the simulation (this will handle manual people too)
          runServerSimulation();
        });
      }

      // Set up manual people placement buttons
      const enableButton = document.getElementById("enable-placement");
      const runManualButton = document.getElementById("run-with-manual");
      const clearButton = document.getElementById("clear-manual");

      if (enableButton) {
        // Remove any existing event listeners
        const newEnableButton = enableButton.cloneNode(true);
        enableButton.parentNode.replaceChild(newEnableButton, enableButton);

        newEnableButton.addEventListener("click", function () {
          const isEnabled = newEnableButton.classList.toggle("active");
          if (isEnabled) {
            newEnableButton.textContent = "Placing People (Click Map)";
            window.visualizer.enableManualPlacement(true);
          } else {
            newEnableButton.textContent = "Place People Manually";
            window.visualizer.enableManualPlacement(false);
          }
        });
      }

      if (runManualButton) {
        // Remove any existing event listeners
        const newRunManualButton = runManualButton.cloneNode(true);
        runManualButton.parentNode.replaceChild(
          newRunManualButton,
          runManualButton
        );

        newRunManualButton.addEventListener("click", function () {
          window.visualizer.runWithManualPeople();
        });
      }

      if (clearButton) {
        // Remove any existing event listeners
        const newClearButton = clearButton.cloneNode(true);
        clearButton.parentNode.replaceChild(newClearButton, clearButton);

        newClearButton.addEventListener("click", function () {
          window.visualizer.clearManualPeople();
        });
      }

      // Initialize extreme scenarios if available
      setTimeout(() => {
        if (typeof addExtremeScenarioControls === "function") {
          addExtremeScenarioControls();
        }
      }, 500);
    }
  }, 200);
});

/**
 * Enhanced visualizer class that supports highlighting family groups
 * This extends the core ShelterSimulationVisualizer with family-specific features
 */
class EnhancedShelterVisualizer extends ShelterSimulationVisualizer {
  /**
   * Constructor - calls the parent constructor and adds family-specific icons
   * @param {string} mapElementId - The HTML element ID where the map will be rendered
   */
  constructor(mapElementId) {
    // Call the parent constructor
    super(mapElementId);

    // Add family-specific icons to the icons object
    this.icons.familyMember = (familyColor) => {
      return L.divIcon({
        className: "marker-person marker-family",
        html: `<div style="background-color: ${familyColor}; border-radius: 50%; width: 12px; height: 12px; border: 2px solid #333;"></div>`,
        iconSize: [16, 16],
        iconAnchor: [8, 8],
      });
    };
  }

  /**
   * Override the displayPeopleAndAssignments method to add family highlighting
   * @override
   */
  displayPeopleAndAssignments(people, shelters, assignments) {
    // First, check if any people have the familyId property
    const hasFamilies = people.some((p) => p.familyId !== undefined);

    // If we're using family groups, get the colors
    const familyColors = {};
    if (hasFamilies) {
      // Create a map of family IDs to colors
      const uniqueFamilyIds = [
        ...new Set(
          people.filter((p) => p.familyId !== undefined).map((p) => p.familyId)
        ),
      ];
      uniqueFamilyIds.forEach((id) => {
        // Get the family color from the global familyGroups array if it exists
        const family = window.familyGroups
          ? window.familyGroups.find((f) => f.id === id)
          : null;
        familyColors[id] = family ? family.color : this.getRandomColor();
      });
    }

    // Call the parent method to handle regular assignments
    super.displayPeopleAndAssignments(people, shelters, assignments);

    // If we have families, apply special styling for family members
    if (hasFamilies) {
      // Replace standard markers with family markers
      this.peopleMarkers.eachLayer((layer) => {
        const personId = layer.options.personId;
        if (!personId) return;

        const person = people.find((p) => p.id === personId);
        if (person && person.familyId !== undefined) {
          // Get the family color
          const color = familyColors[person.familyId] || "#ff0000";

          // Create a new marker with family styling
          const familyMarker = L.marker([person.latitude, person.longitude], {
            icon: this.icons.familyMember(color),
            personId: personId,
          });

          // Update the popup content to include family information
          const popup = layer.getPopup();
          if (popup) {
            const content = popup.getContent();
            const familyContent =
              content + `<p>Family: Group ${person.familyId}</p>`;
            familyMarker.bindPopup(familyContent);
          }

          // Replace the original marker with the family marker
          this.peopleMarkers.removeLayer(layer);
          this.peopleMarkers.addLayer(familyMarker);
        }
      });

      // Draw lines between family members of the same family
      for (const familyId of Object.keys(familyColors)) {
        const familyMembers = people.filter(
          (p) => p.familyId === parseInt(familyId)
        );

        // Skip if only one member (nothing to connect)
        if (familyMembers.length <= 1) continue;

        // Draw lines between family members
        for (let i = 0; i < familyMembers.length - 1; i++) {
          const personA = familyMembers[i];
          for (let j = i + 1; j < familyMembers.length; j++) {
            const personB = familyMembers[j];

            // Draw a dashed line between the family members
            const line = L.polyline(
              [
                [personA.latitude, personA.longitude],
                [personB.latitude, personB.longitude],
              ],
              {
                color: familyColors[familyId],
                opacity: 0.6,
                weight: 2,
                dashArray: "5, 5",
                className: "family-connection",
              }
            );
            this.pathLines.addLayer(line);
          }
        }
      }
    }
  }

  /**
   * Generate a random color for family visualization
   * @returns {string} - A random hex color
   */
  getRandomColor() {
    const letters = "0123456789ABCDEF";
    let color = "#";
    for (let i = 0; i < 6; i++) {
      color += letters[Math.floor(Math.random() * 16)];
    }
    return color;
  }
}

/**
 * Load the extreme scenarios functionality
 */
function loadExtremeScenarios() {
  // First, add the CSS
  const styleElement = document.createElement("style");
  styleElement.textContent = `
    /* Extreme scenario styles will be loaded here */
    .control-divider {
      margin: 15px 0;
      border: 0;
      height: 1px;
      background-color: #ddd;
    }
    
    .scenario-heading {
      margin: 10px 0;
      font-size: 16px;
      color: #333;
    }
    
    .scenario-button {
      margin: 5px 0;
      width: 100%;
      padding: 8px;
      background-color: #f0f0f0;
      border: 1px solid #ccc;
      border-radius: 4px;
      font-size: 14px;
      cursor: pointer;
      transition: background-color 0.2s;
    }
    
    .scenario-button:hover {
      background-color: #e0e0e0;
    }
    
    #scenario-overcrowd {
      background-color: #ffebee;
      border-color: #ffcdd2;
    }
    
    #scenario-families {
      background-color: #e3f2fd;
      border-color: #bbdefb;
    }
    
    #scenario-elderly {
      background-color: #fff8e1;
      border-color: #ffe082;
    }
    
    #scenario-reset {
      background-color: #e8f5e9;
      border-color: #c8e6c9;
    }
    
    .info-container {
      margin-top: 15px;
      padding: 10px;
      background-color: #f5f5f5;
      border-radius: 4px;
    }
  `;
  document.head.appendChild(styleElement);

  // Add the extreme scenarios controls with a short delay to ensure the UI is ready
  setTimeout(() => {
    if (typeof addExtremeScenarioControls === "function") {
      addExtremeScenarioControls();
    } else {
      console.warn(
        "Extreme scenario controls function not found, loading default version"
      );

      // Define a minimal version of the function if the main script isn't loaded
      window.addExtremeScenarioControls = function () {
        const controlContainer = document.querySelector(".control-container");
        if (!controlContainer) return;

        const divider = document.createElement("hr");
        divider.className = "control-divider";
        controlContainer.appendChild(divider);

        const heading = document.createElement("h4");
        heading.textContent = "Extreme Scenarios";
        heading.className = "scenario-heading";
        controlContainer.appendChild(heading);

        const overcrowdButton = document.createElement("button");
        overcrowdButton.id = "scenario-overcrowd";
        overcrowdButton.textContent = "Overcrowd (500 People)";
        overcrowdButton.className = "scenario-button control-button";
        overcrowdButton.addEventListener("click", function () {
          const peopleCountInput = document.getElementById("people-count");
          if (peopleCountInput) peopleCountInput.value = "500";
          runServerSimulation();
        });
        controlContainer.appendChild(overcrowdButton);
      };

      // Call the minimal version
      window.addExtremeScenarioControls();
    }
  }, 1000);
}
