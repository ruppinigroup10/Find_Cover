using ServerSimulation.Models;
using ServerSimulation.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServerSimulation.DAL
{
    public interface IGoogleMapsService
    {
        // Distance Matrix API
        //Task<DistanceMatrixResponse> GetDistanceMatrixAsync(DistanceMatrixRequest request);

        // Directions API
        Task<DirectionsResponse> GetDirectionsAsync(DirectionsRequest request);
        Task<Dictionary<string, Dictionary<string, double>>> CalculateShelterDistancesAsync(
            List<PersonDto> people,
            List<ShelterDto> shelters);

        Task<Dictionary<string, DirectionsResponse>> GetRoutesForPeople(
List<PersonDto> people,
List<ShelterDto> shelters,
Dictionary<int, AssignmentDto> assignments);
    }
}