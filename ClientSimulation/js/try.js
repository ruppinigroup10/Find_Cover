/**
 * Run simulation with manual people using Google Maps walking distances
 */
async function runManualSimulationWithWalkingDistances() {
  const statusElement = document.getElementById("simulation-status");
  const button = document.getElementById("run-manual-walking");

  if (button) {
    button.classList.add("loading");
    button.disabled = true;
  }

  if (statusElement) {
    statusElement.textContent =
      "Running manual simulation with walking distances...";
    statusElement.className = "status-message running";
  }

  try {
    const priorityEnabled =
      document.getElementById("priority").value === "true";
    const radius = parseFloat(document.getElementById("radius").value) || 0.5;
    const useDatabaseShelters =
      document.getElementById("use-database-shelters")?.checked || false;

    const allCurrentPeople = [];
    const seenIds = new Set();

    window.visualizer.peopleMarkers.eachLayer((marker) => {
      if (marker.getLatLng && marker.options && marker.options.personId) {
        const personId = marker.options.personId;

        if (seenIds.has(personId)) return;
        seenIds.add(personId);

        let personData = null;

        if (window.visualizer.currentSimulationData?.people) {
          personData = window.visualizer.currentSimulationData.people.find(
            (p) => p.id == personId
          );
        }

        if (!personData && marker.options.isManual) {
          personData = window.visualizer.manualPeople.find(
            (p) => p.id == personId
          );
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

    const currentShelters =
      window.visualizer.currentSimulationData?.shelters || [];

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
      customShelters: currentShelters.length > 0 ? currentShelters : undefined,
      useDatabaseShelters: useDatabaseShelters,
    };

    const response = await fetch(
      `https://localhost:${PORT}/api/Simulation/run-with-walking-distances`,
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

    window.visualizer.clearMap();

    const newManualPeople = [];
    data.people.forEach((person) => {
      if (
        person.isManual ||
        window.visualizer.manualPeople.some((mp) => mp.id == person.id)
      ) {
        newManualPeople.push(person);
      }
    });
    window.visualizer.manualPeople = newManualPeople;

    window.visualizer.currentSimulationData = data;
    window.visualizer.visualizeSimulation(
      data.people,
      data.shelters,
      data.assignments
    );

    window.visualizer.updateManualPeopleList();
    window.visualizer.updateManualControlStatus();

    updateStatistics(data.statistics, data.people, data.assignments);

    if (statusElement) {
      statusElement.textContent =
        "Manual simulation with walking distances complete";
      statusElement.className = "status-message success";

      setTimeout(() => {
        statusElement.textContent = "";
        statusElement.className = "status-message";
      }, 3000);
    }
  } catch (error) {
    console.error(
      "Error running manual simulation with walking distances:",
      error
    );
    if (statusElement) {
      statusElement.textContent = `Error: ${error.message}`;
      statusElement.className = "status-message error";
    }
  } finally {
    if (button) {
      button.classList.remove("loading");
      button.disabled = window.visualizer?.manualPeople?.length === 0;
    }
  }
}

/**
 * Enhance the setup event handlers with Google Maps walking distance features
 */
function enhanceSetupEventHandlers() {
  // Create the new button
  const manualWalkingButton = document.createElement("button");
  manualWalkingButton.id = "run-manual-walking";
  manualWalkingButton.className = "control-button";
  manualWalkingButton.textContent = "Run With Manual (Walking Distance)";
  manualWalkingButton.disabled = true;
  manualWalkingButton.style.backgroundColor = "#27ae60";
  manualWalkingButton.style.marginTop = "5px";

  // Insert it after the regular manual button
  const runManualButton = document.getElementById("run-with-manual");
  if (runManualButton && runManualButton.parentNode) {
    runManualButton.parentNode.insertBefore(
      manualWalkingButton,
      runManualButton.nextSibling
    );
  }

  // Add event handler
  manualWalkingButton.addEventListener("click", function () {
    if (window.visualizer) {
      disableActiveModes();
      runManualSimulationWithWalkingDistances();
    }
  });

  // Update the manual control status function to handle both buttons
  if (window.visualizer) {
    const originalUpdateStatus = window.visualizer.updateManualControlStatus;
    window.visualizer.updateManualControlStatus = function () {
      originalUpdateStatus.call(this);

      const walkingButton = document.getElementById("run-manual-walking");
      if (!walkingButton) return;

      const manualCount = this.manualPeople?.length || 0;

      if (manualCount > 0) {
        walkingButton.textContent = `Run With Manual (Walking Distance) (${manualCount})`;
        walkingButton.disabled = false;
      } else {
        walkingButton.textContent = "Run With Manual (Walking Distance) (0)";
        walkingButton.disabled = true;
      }
    };

    window.visualizer.updateManualControlStatus();
  }
}
