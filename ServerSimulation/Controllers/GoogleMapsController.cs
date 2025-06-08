using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServerSimulation.Models;
using ServerSimulation.DAL;
using ServerSimulation.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace FindCover.Controllers
{

    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ServerSimulation.Models;
    using ServerSimulation.DAL;
    using ServerSimulation.Models.DTOs;
    using Microsoft.Extensions.Logging;

    namespace FindCover.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class GoogleMapsController : ControllerBase
        {
            private readonly IGoogleMapsService _googleMapsService;
            private readonly ILogger<GoogleMapsController> _logger;

            public GoogleMapsController(IGoogleMapsService googleMapsService, ILogger<GoogleMapsController> logger)
            {
                _googleMapsService = googleMapsService;
                _logger = logger;
            }

            /// <summary>
            /// Test endpoint to verify Google Maps API connection
            /// </summary>
            [HttpGet("test")]
            public async Task<IActionResult> TestConnection()
            {
                try
                {
                    // Simple test: calculate directions between two points in Beer Sheva
                    var request = new DirectionsRequest
                    {
                        Origin = new LocationPoint(31.2518, 34.7913, "test-origin"),
                        Destination = new LocationPoint(31.2589, 34.7996, "test-dest"),
                        Mode = TravelMode.Walking
                    };

                    var result = await _googleMapsService.GetDirectionsAsync(request);

                    return Ok(new
                    {
                        success = result.Success,
                        message = result.Success ?
                            "Google Maps API is working correctly" :
                            $"API Error: {result.ErrorMessage}",
                        testDetails = result.Success ? new
                        {
                            distance = result.Routes?.FirstOrDefault()?.Legs?.FirstOrDefault()?.Distance?.Value,
                            duration = result.Routes?.FirstOrDefault()?.Legs?.FirstOrDefault()?.Duration?.Value,
                            status = result.Status
                        } : null
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error testing Google Maps connection");
                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }

            /// <summary>
            /// Get walking directions between two points
            /// </summary>
            [HttpPost("directions")]
            public async Task<IActionResult> GetDirections([FromBody] DirectionsRequest request)
            {
                try
                {
                    var result = await _googleMapsService.GetDirectionsAsync(request);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting directions");
                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }

            /// <summary>
            /// Calculate shelter distances for a list of people and shelters
            /// </summary>
            [HttpPost("shelter-distances")]
            public async Task<IActionResult> CalculateShelterDistances([FromBody] ShelterDistanceRequest request)
            {
                try
                {
                    var distances = await _googleMapsService.CalculateShelterDistancesAsync(
                        request.People,
                        request.Shelters);

                    return Ok(new
                    {
                        success = true,
                        distances = distances
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating shelter distances");
                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }

            /// <summary>
            /// Get walking time between two points using Directions API
            /// </summary>
            [HttpGet("walking-time")]
            public async Task<IActionResult> GetWalkingTime(
                [FromQuery] double originLat,
                [FromQuery] double originLng,
                [FromQuery] double destLat,
                [FromQuery] double destLng)
            {
                try
                {
                    var request = new DirectionsRequest
                    {
                        Origin = new LocationPoint(originLat, originLng),
                        Destination = new LocationPoint(destLat, destLng),
                        Mode = TravelMode.Walking
                    };

                    var result = await _googleMapsService.GetDirectionsAsync(request);

                    if (result.Success && result.Routes?.Count > 0)
                    {
                        var route = result.Routes[0];
                        if (route.Legs?.Count > 0)
                        {
                            var leg = route.Legs[0];
                            return Ok(new
                            {
                                success = true,
                                distanceMeters = leg.Distance?.Value ?? 0,
                                distanceText = leg.Distance?.Text ?? "Unknown",
                                durationSeconds = leg.Duration?.Value ?? 0,
                                durationText = leg.Duration?.Text ?? "Unknown",
                                polyline = route.OverviewPolyline
                            });
                        }
                    }

                    return Ok(new { success = false, message = "No route found" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting walking time");
                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }
        }

        // DTO for shelter distance calculation request
        public class ShelterDistanceRequest
        {
            public List<PersonDto> People { get; set; }
            public List<ShelterDto> Shelters { get; set; }
        }
    }

    // [ApiController]
    // [Route("api/[controller]")]
    // public class GoogleMapsController : ControllerBase
    // {
    //     private readonly IGoogleMapsService _googleMapsService;
    //     private readonly ILogger<GoogleMapsController> _logger;

    //     public GoogleMapsController(IGoogleMapsService googleMapsService, ILogger<GoogleMapsController> logger)
    //     {
    //         _googleMapsService = googleMapsService;
    //         _logger = logger;
    //     }

    //     /// <summary>
    //     /// Test endpoint to verify Google Maps API connection
    //     /// </summary>
    //     [HttpGet("test")]
    //     public async Task<IActionResult> TestConnection()
    //     {
    //         try
    //         {
    //             // Simple test: calculate distance between two points in Beer Sheva
    //             var request = new DistanceMatrixRequest
    //             {
    //                 Origins = new List<LocationPoint>
    //                 {
    //                     new LocationPoint(31.2518, 34.7913, "test-origin")
    //                 },
    //                 Destinations = new List<LocationPoint>
    //                 {
    //                     new LocationPoint(31.2589, 34.7996, "test-dest")
    //                 },
    //                 Mode = TravelMode.Walking
    //             };

    //             var result = await _googleMapsService.GetDistanceMatrixAsync(request);

    //             return Ok(new
    //             {
    //                 success = result.Success,
    //                 message = result.Success ? "Google Maps API is working" : result.ErrorMessage,
    //                 status = result.Status
    //             });
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Error testing Google Maps connection");
    //             return StatusCode(500, new { success = false, message = ex.Message });
    //         }
    //     }

    //     /// <summary>
    //     /// Calculate distances between multiple origins and destinations
    //     /// </summary>
    //     [HttpPost("distance-matrix")]
    //     public async Task<IActionResult> GetDistanceMatrix([FromBody] DistanceMatrixRequest request)
    //     {
    //         try
    //         {
    //             var result = await _googleMapsService.GetDistanceMatrixAsync(request);
    //             return Ok(result);
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Error getting distance matrix");
    //             return StatusCode(500, new { success = false, message = ex.Message });
    //         }
    //     }

    //     /// <summary>
    //     /// Get walking directions between two points
    //     /// </summary>
    //     [HttpPost("directions")]
    //     public async Task<IActionResult> GetDirections([FromBody] DirectionsRequest request)
    //     {
    //         try
    //         {
    //             var result = await _googleMapsService.GetDirectionsAsync(request);
    //             return Ok(result);
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Error getting directions");
    //             return StatusCode(500, new { success = false, message = ex.Message });
    //         }
    //     }

    //     /// <summary>
    //     /// Calculate shelter distances for a list of people and shelters
    //     /// </summary>
    //     [HttpPost("shelter-distances")]
    //     public async Task<IActionResult> CalculateShelterDistances([FromBody] ShelterDistanceRequest request)
    //     {
    //         try
    //         {
    //             var distances = await _googleMapsService.CalculateShelterDistancesAsync(
    //                 request.People,
    //                 request.Shelters);

    //             return Ok(new
    //             {
    //                 success = true,
    //                 distances = distances
    //             });
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Error calculating shelter distances");
    //             return StatusCode(500, new { success = false, message = ex.Message });
    //         }
    //     }

    //     /// <summary>
    //     /// Get walking time between two points
    //     /// </summary>
    //     [HttpGet("walking-time")]
    //     public async Task<IActionResult> GetWalkingTime(
    //         [FromQuery] double originLat,
    //         [FromQuery] double originLng,
    //         [FromQuery] double destLat,
    //         [FromQuery] double destLng)
    //     {
    //         try
    //         {
    //             var request = new DistanceMatrixRequest
    //             {
    //                 Origins = new List<LocationPoint>
    //                 {
    //                     new LocationPoint(originLat, originLng)
    //                 },
    //                 Destinations = new List<LocationPoint>
    //                 {
    //                     new LocationPoint(destLat, destLng)
    //                 },
    //                 Mode = TravelMode.Walking
    //             };

    //             var result = await _googleMapsService.GetDistanceMatrixAsync(request);

    //             if (result.Success && result.Rows?.Count > 0 && result.Rows[0].Elements?.Count > 0)
    //             {
    //                 var element = result.Rows[0].Elements[0];
    //                 return Ok(new
    //                 {
    //                     success = true,
    //                     distanceMeters = element.Distance?.Value ?? 0,
    //                     distanceText = element.Distance?.Text ?? "Unknown",
    //                     durationSeconds = element.Duration?.Value ?? 0,
    //                     durationText = element.Duration?.Text ?? "Unknown"
    //                 });
    //             }

    //             return Ok(new { success = false, message = "No route found" });
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Error getting walking time");
    //             return StatusCode(500, new { success = false, message = ex.Message });
    //         }
    //     }
    // }

    // // DTO for shelter distance calculation request
    // public class ShelterDistanceRequest
    // {
    //     public List<PersonDto> People { get; set; }
    //     public List<ShelterDto> Shelters { get; set; }
    // }
}
