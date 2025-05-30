using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ServerSimulation.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerSimulation.Models.DTOs;
using Newtonsoft.Json.Linq;

namespace ServerSimulation.DAL
{
    /// <summary>
    /// Service for interacting with Google Maps APIs
    /// </summary>
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleMapsConfig _config;
        private readonly ILogger<GoogleMapsService> _logger;
        private const string DISTANCE_MATRIX_URL = "https://maps.googleapis.com/maps/api/distancematrix/json";
        private const string DIRECTIONS_URL = "https://maps.googleapis.com/maps/api/directions/json";

        public GoogleMapsService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Load configuration
            _config = new GoogleMapsConfig
            {
                ApiKey = configuration["GoogleMaps:ApiKey"],
                Language = configuration["GoogleMaps:Language"] ?? "he",
                Region = configuration["GoogleMaps:Region"] ?? "IL"
            };

            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                throw new InvalidOperationException("Google Maps API key is not configured");
            }
        }

        /// <summary>
        /// Calculate walking distances between multiple origins and destinations
        /// </summary>
        public async Task<DistanceMatrixResponse> GetDistanceMatrixAsync(DistanceMatrixRequest request)
        {
            try
            {
                // Validate request
                if (request.Origins == null || !request.Origins.Any())
                    throw new ArgumentException("At least one origin is required");

                if (request.Destinations == null || !request.Destinations.Any())
                    throw new ArgumentException("At least one destination is required");

                // Check Google's limits
                int totalElements = request.Origins.Count * request.Destinations.Count;
                if (totalElements > _config.MaxElementsPerRequest)
                {
                    _logger.LogWarning($"Request exceeds Google's limit of {_config.MaxElementsPerRequest} elements. Consider batching.");
                }

                // Build query parameters
                var queryParams = new Dictionary<string, string>
                {
                    ["origins"] = string.Join("|", request.Origins.Select(o => o.ToString())),
                    ["destinations"] = string.Join("|", request.Destinations.Select(d => d.ToString())),
                    ["mode"] = request.Mode.ToString().ToLower(),
                    ["units"] = "metric",
                    ["language"] = _config.Language,
                    ["region"] = _config.Region,
                    ["key"] = _config.ApiKey
                };

                if (request.AvoidHighways)
                    queryParams["avoid"] = "highways";
                else if (request.AvoidTolls)
                    queryParams["avoid"] = "tolls";

                // Build URL
                var url = BuildUrl(DISTANCE_MATRIX_URL, queryParams);

                // Make API call
                _logger.LogInformation($"Calling Google Distance Matrix API for {request.Origins.Count} origins and {request.Destinations.Count} destinations");
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = ParseDistanceMatrixResponse(json);

                _logger.LogInformation($"Distance Matrix API call successful. Status: {result.Status}");
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Google Maps API");
                return new DistanceMatrixResponse
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating distance matrix");
                return new DistanceMatrixResponse
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get detailed walking directions between two points
        /// </summary>
        public async Task<DirectionsResponse> GetDirectionsAsync(DirectionsRequest request)
        {
            try
            {
                // Validate request
                if (request.Origin == null || request.Destination == null)
                    throw new ArgumentException("Origin and destination are required");

                // Build query parameters
                var queryParams = new Dictionary<string, string>
                {
                    ["origin"] = request.Origin.ToString(),
                    ["destination"] = request.Destination.ToString(),
                    ["mode"] = request.Mode.ToString().ToLower(),
                    ["units"] = "metric",
                    ["language"] = _config.Language,
                    ["region"] = _config.Region,
                    ["alternatives"] = request.Alternatives.ToString().ToLower(),
                    ["key"] = _config.ApiKey
                };

                if (request.DepartureTime.HasValue)
                {
                    var unixTime = ((DateTimeOffset)request.DepartureTime.Value).ToUnixTimeSeconds();
                    queryParams["departure_time"] = unixTime.ToString();
                }

                // Build URL
                var url = BuildUrl(DIRECTIONS_URL, queryParams);

                // Make API call
                _logger.LogInformation($"Calling Google Directions API from {request.Origin} to {request.Destination}");
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = ParseDirectionsResponse(json);

                _logger.LogInformation($"Directions API call successful. Found {result.Routes?.Count ?? 0} routes");
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Google Maps API");
                return new DirectionsResponse
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting directions");
                return new DirectionsResponse
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Calculate walking distance for shelter assignments (batch processing)
        /// </summary>
        public async Task<Dictionary<string, Dictionary<string, double>>> CalculateShelterDistancesAsync(
            List<PersonDto> people,
            List<ShelterDto> shelters)
        {
            var result = new Dictionary<string, Dictionary<string, double>>();

            try
            {
                // Convert to location points
                var origins = people.Select(p => new LocationPoint(p.Latitude, p.Longitude, p.Id.ToString())).ToList();
                var destinations = shelters.Select(s => new LocationPoint(s.Latitude, s.Longitude, s.Id.ToString())).ToList();

                // Google limits: 100 elements per request, so we might need to batch
                int batchSize = 10; // 10x10 = 100 elements

                for (int i = 0; i < origins.Count; i += batchSize)
                {
                    var originBatch = origins.Skip(i).Take(batchSize).ToList();

                    for (int j = 0; j < destinations.Count; j += batchSize)
                    {
                        var destBatch = destinations.Skip(j).Take(batchSize).ToList();

                        var request = new DistanceMatrixRequest
                        {
                            Origins = originBatch,
                            Destinations = destBatch,
                            Mode = TravelMode.Walking
                        };

                        var response = await GetDistanceMatrixAsync(request);

                        if (response.Success && response.Rows != null)
                        {
                            // Process results
                            for (int oi = 0; oi < originBatch.Count; oi++)
                            {
                                var originId = originBatch[oi].Id;
                                if (!result.ContainsKey(originId))
                                    result[originId] = new Dictionary<string, double>();

                                for (int di = 0; di < destBatch.Count; di++)
                                {
                                    var destId = destBatch[di].Id;
                                    var element = response.Rows[oi].Elements[di];

                                    if (element.Status == "OK" && element.Distance != null)
                                    {
                                        // Convert meters to kilometers
                                        result[originId][destId] = element.Distance.Value / 1000.0;
                                    }
                                    else
                                    {
                                        // Use -1 to indicate no route available
                                        result[originId][destId] = -1;
                                    }
                                }
                            }
                        }

                        // Add delay to respect API rate limits
                        await Task.Delay(100);
                    }


                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shelter distances");
                throw;
            }

            return result;
        }



        public async Task<Dictionary<string, DirectionsResponse>> GetRoutesForPeople(
       List<PersonDto> people,
       List<ShelterDto> shelters,
       Dictionary<int, AssignmentDto> assignments)
        {
            var routes = new Dictionary<string, DirectionsResponse>();

            foreach (var assignment in assignments)
            {
                var person = people.FirstOrDefault(p => p.Id == assignment.Key);
                var shelter = shelters.FirstOrDefault(s => s.Id == assignment.Value.ShelterId);

                if (person != null && shelter != null)
                {
                    var request = new DirectionsRequest
                    {
                        Origin = new LocationPoint(person.Latitude, person.Longitude),
                        Destination = new LocationPoint(shelter.Latitude, shelter.Longitude),
                        Mode = TravelMode.Walking
                    };

                    var response = await GetDirectionsAsync(request);

                    if (response.Success && response.Routes?.Any() == true)
                    {
                        routes[$"{person.Id}-{shelter.Id}"] = response;
                    }

                    // Add small delay to respect API rate limits
                    await Task.Delay(50);
                }
            }

            return routes;
        }


        #region Helper Methods

        private string BuildUrl(string baseUrl, Dictionary<string, string> queryParams)
        {
            var query = string.Join("&", queryParams.Select(kv =>
                $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}"));
            return $"{baseUrl}?{query}";
        }

        private DistanceMatrixResponse ParseDistanceMatrixResponse(string json)
        {
            try
            {
                var jObject = JObject.Parse(json);
                var response = new DistanceMatrixResponse
                {
                    Status = jObject["status"]?.ToString(),
                    Rows = new List<DistanceMatrixRow>()
                };

                // Check if request was successful
                if (response.Status != "OK")
                {
                    response.Success = false;
                    response.ErrorMessage = $"Google API returned status: {response.Status}";

                    if (jObject["error_message"] != null)
                        response.ErrorMessage += $" - {jObject["error_message"]}";

                    return response;
                }

                response.Success = true;

                // Parse rows
                var rows = jObject["rows"] as JArray;
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var matrixRow = new DistanceMatrixRow
                        {
                            Elements = new List<DistanceMatrixElement>()
                        };

                        var elements = row["elements"] as JArray;
                        if (elements != null)
                        {
                            foreach (var element in elements)
                            {
                                var matrixElement = new DistanceMatrixElement
                                {
                                    Status = element["status"]?.ToString()
                                };

                                if (matrixElement.Status == "OK")
                                {
                                    if (element["distance"] != null)
                                    {
                                        matrixElement.Distance = new DistanceInfo
                                        {
                                            Text = element["distance"]["text"]?.ToString(),
                                            Value = element["distance"]["value"]?.Value<int>() ?? 0
                                        };
                                    }

                                    if (element["duration"] != null)
                                    {
                                        matrixElement.Duration = new DurationInfo
                                        {
                                            Text = element["duration"]["text"]?.ToString(),
                                            Value = element["duration"]["value"]?.Value<int>() ?? 0
                                        };
                                    }
                                }

                                matrixRow.Elements.Add(matrixElement);
                            }
                        }

                        response.Rows.Add(matrixRow);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Distance Matrix response");
                return new DistanceMatrixResponse
                {
                    Success = false,
                    ErrorMessage = $"Error parsing response: {ex.Message}"
                };
            }
        }

        private DirectionsResponse ParseDirectionsResponse(string json)
        {
            try
            {
                var jObject = JObject.Parse(json);
                var response = new DirectionsResponse
                {
                    Status = jObject["status"]?.ToString(),
                    Routes = new List<ServerSimulation.Models.Route>()
                };

                // Check if request was successful
                if (response.Status != "OK")
                {
                    response.Success = false;
                    response.ErrorMessage = $"Google API returned status: {response.Status}";

                    if (jObject["error_message"] != null)
                        response.ErrorMessage += $" - {jObject["error_message"]}";

                    return response;
                }

                response.Success = true;

                // Parse routes
                var routes = jObject["routes"] as JArray;
                if (routes != null)
                {
                    foreach (var route in routes)
                    {
                        var routeObj = new ServerSimulation.Models.Route
                        {
                            Summary = route["summary"]?.ToString(),
                            OverviewPolyline = route["overview_polyline"]?["points"]?.ToString(),
                            Legs = new List<Leg>()
                        };

                        // Parse bounds
                        if (route["bounds"] != null)
                        {
                            routeObj.Bounds = new Bounds
                            {
                                Northeast = new LocationPoint(
                                    route["bounds"]["northeast"]["lat"].Value<double>(),
                                    route["bounds"]["northeast"]["lng"].Value<double>()
                                ),
                                Southwest = new LocationPoint(
                                    route["bounds"]["southwest"]["lat"].Value<double>(),
                                    route["bounds"]["southwest"]["lng"].Value<double>()
                                )
                            };
                        }

                        // Parse legs
                        var legs = route["legs"] as JArray;
                        if (legs != null)
                        {
                            foreach (var leg in legs)
                            {
                                var legObj = ParseLeg(leg);
                                routeObj.Legs.Add(legObj);
                            }
                        }

                        response.Routes.Add(routeObj);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Directions response");
                return new DirectionsResponse
                {
                    Success = false,
                    ErrorMessage = $"Error parsing response: {ex.Message}"
                };
            }
        }

        private Leg ParseLeg(JToken leg)
        {
            var legObj = new Leg
            {
                StartAddress = leg["start_address"]?.ToString(),
                EndAddress = leg["end_address"]?.ToString(),
                Steps = new List<Step>()
            };

            // Parse distance
            if (leg["distance"] != null)
            {
                legObj.Distance = new DistanceInfo
                {
                    Text = leg["distance"]["text"]?.ToString(),
                    Value = leg["distance"]["value"]?.Value<int>() ?? 0
                };
            }

            // Parse duration
            if (leg["duration"] != null)
            {
                legObj.Duration = new DurationInfo
                {
                    Text = leg["duration"]["text"]?.ToString(),
                    Value = leg["duration"]["value"]?.Value<int>() ?? 0
                };
            }

            // Parse locations
            if (leg["start_location"] != null)
            {
                legObj.StartLocation = new LocationPoint(
                    leg["start_location"]["lat"].Value<double>(),
                    leg["start_location"]["lng"].Value<double>()
                );
            }

            if (leg["end_location"] != null)
            {
                legObj.EndLocation = new LocationPoint(
                    leg["end_location"]["lat"].Value<double>(),
                    leg["end_location"]["lng"].Value<double>()
                );
            }

            // Parse steps
            var steps = leg["steps"] as JArray;
            if (steps != null)
            {
                foreach (var step in steps)
                {
                    var stepObj = new Step
                    {
                        HtmlInstructions = step["html_instructions"]?.ToString(),
                        Polyline = step["polyline"]?["points"]?.ToString()
                    };

                    // Parse step distance
                    if (step["distance"] != null)
                    {
                        stepObj.Distance = new DistanceInfo
                        {
                            Text = step["distance"]["text"]?.ToString(),
                            Value = step["distance"]["value"]?.Value<int>() ?? 0
                        };
                    }

                    // Parse step duration
                    if (step["duration"] != null)
                    {
                        stepObj.Duration = new DurationInfo
                        {
                            Text = step["duration"]["text"]?.ToString(),
                            Value = step["duration"]["value"]?.Value<int>() ?? 0
                        };
                    }

                    // Parse step locations
                    if (step["start_location"] != null)
                    {
                        stepObj.StartLocation = new LocationPoint(
                            step["start_location"]["lat"].Value<double>(),
                            step["start_location"]["lng"].Value<double>()
                        );
                    }

                    if (step["end_location"] != null)
                    {
                        stepObj.EndLocation = new LocationPoint(
                            step["end_location"]["lat"].Value<double>(),
                            step["end_location"]["lng"].Value<double>()
                        );
                    }

                    // Parse travel mode
                    var travelMode = step["travel_mode"]?.ToString();
                    if (Enum.TryParse<TravelMode>(travelMode, true, out var mode))
                    {
                        stepObj.TravelMode = mode;
                    }

                    legObj.Steps.Add(stepObj);
                }
            }

            return legObj;
        }

        #endregion
    }


}