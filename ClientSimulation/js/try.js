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

  console.log(`Adding ${this.manualPeople.length} manual people to base simulation with ${basePeople.length} people`);

  // Create a new combined people array with base + all manual people
  const allPeople = [...basePeople];

  // Find the highest ID currently in use
  let maxId = 0;
  basePeople.forEach(person => {
    if (typeof person.id === "number" && person.id > maxId) {
      maxId = person.id;
    }
  });

  // Add ALL manual people with new sequential IDs
  this.manualPeople.forEach((person, index) => {
    const newPerson = {
      ...person,
      id: maxId + index + 1,
      isManual: true // Mark as manual for future reference
    };
    allPeople.push(newPerson);
  });

  // Run server simulation with combined people
  this.runServerSimulationWithCustomData(allPeople, baseShelters);
}