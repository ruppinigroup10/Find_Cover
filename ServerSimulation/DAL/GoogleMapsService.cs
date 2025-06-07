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
using System.Data.SqlClient;
using System.Data;

namespace ServerSimulation.DAL
{
    /// <summary>
    /// Service for interacting with Google Maps APIs with caching support
    /// </summary>
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleMapsConfig _config;
        private readonly ILogger<GoogleMapsService> _logger;
        private const string DISTANCE_MATRIX_URL = "https://maps.googleapis.com/maps/api/distancematrix/json";
        private const string DIRECTIONS_URL = "https://maps.googleapis.com/maps/api/directions/json";
        private readonly DBservices _dbService;

        public GoogleMapsService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _dbService = new DBservices(); // Initialize database service

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
        /// Calculate walking distances between multiple origins and destinations with caching
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

                // Create a response object
                var response = new DistanceMatrixResponse
                {
                    Success = true,
                    Status = "OK",
                    Rows = new List<DistanceMatrixRow>()
                };

                // Track which pairs need API calls
                var uncachedPairs = new List<(LocationPoint origin, LocationPoint destination, int rowIndex, int elementIndex)>();

                // First pass: Check cache for all origin-destination pairs
                for (int i = 0; i < request.Origins.Count; i++)
                {
                    var row = new DistanceMatrixRow { Elements = new List<DistanceMatrixElement>() };

                    for (int j = 0; j < request.Destinations.Count; j++)
                    {
                        var origin = request.Origins[i];
                        var destination = request.Destinations[j];

                        // Check cache
                        var cachedDistance = GetCachedDistance(
                            origin.Latitude, origin.Longitude,
                            destination.Latitude, destination.Longitude
                        );

                        if (cachedDistance != null)
                        {
                            // Use cached data
                            row.Elements.Add(cachedDistance);
                            _logger.LogInformation($"Cache HIT: ({origin.Latitude}, {origin.Longitude}) to ({destination.Latitude}, {destination.Longitude})");
                        }
                        else
                        {
                            // Add placeholder, will fill with API call
                            row.Elements.Add(null);
                            uncachedPairs.Add((origin, destination, i, j));
                            _logger.LogInformation($"Cache MISS: ({origin.Latitude}, {origin.Longitude}) to ({destination.Latitude}, {destination.Longitude})");
                        }
                    }

                    response.Rows.Add(row);
                }

                // If all pairs were cached, return immediately
                if (!uncachedPairs.Any())
                {
                    _logger.LogInformation("All distances found in cache!");
                    return response;
                }

                // Second pass: Make API calls for uncached pairs in batches
                _logger.LogInformation($"Need to fetch {uncachedPairs.Count} distances from Google Maps API");

                // Google limits: 100 elements per request
                const int maxElementsPerRequest = 100;

                for (int i = 0; i < uncachedPairs.Count; i += maxElementsPerRequest)
                {
                    var batch = uncachedPairs.Skip(i).Take(maxElementsPerRequest).ToList();

                    // Create unique origins and destinations for this batch
                    var batchOrigins = batch.Select(b => b.origin).Distinct(new LocationPointComparer()).ToList();
                    var batchDestinations = batch.Select(b => b.destination).Distinct(new LocationPointComparer()).ToList();

                    // Build query parameters
                    var queryParams = new Dictionary<string, string>
                    {
                        ["origins"] = string.Join("|", batchOrigins.Select(o => $"{o.Latitude},{o.Longitude}")),
                        ["destinations"] = string.Join("|", batchDestinations.Select(d => $"{d.Latitude},{d.Longitude}")),
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

                    // Make API call
                    var url = BuildUrl(DISTANCE_MATRIX_URL, queryParams);
                    _logger.LogInformation($"Calling Google Distance Matrix API for batch {i / maxElementsPerRequest + 1}");

                    var apiResponse = await _httpClient.GetAsync(url);
                    apiResponse.EnsureSuccessStatusCode();

                    var json = await apiResponse.Content.ReadAsStringAsync();
                    var apiResult = ParseDistanceMatrixResponse(json);

                    if (!apiResult.Success)
                    {
                        _logger.LogError($"Google Maps API error: {apiResult.ErrorMessage}");
                        continue;
                    }

                    // Map API results back to our response and cache them
                    foreach (var pair in batch)
                    {
                        // Find indices in batch response
                        var originIndex = batchOrigins.FindIndex(o =>
                            Math.Abs(o.Latitude - pair.origin.Latitude) < 0.0001 &&
                            Math.Abs(o.Longitude - pair.origin.Longitude) < 0.0001);

                        var destIndex = batchDestinations.FindIndex(d =>
                            Math.Abs(d.Latitude - pair.destination.Latitude) < 0.0001 &&
                            Math.Abs(d.Longitude - pair.destination.Longitude) < 0.0001);

                        if (originIndex >= 0 && destIndex >= 0 &&
                            apiResult.Rows.Count > originIndex &&
                            apiResult.Rows[originIndex].Elements.Count > destIndex)
                        {
                            var element = apiResult.Rows[originIndex].Elements[destIndex];

                            // Update response
                            response.Rows[pair.rowIndex].Elements[pair.elementIndex] = element;

                            // Cache the result if successful
                            if (element.Status == "OK" && element.Distance != null && element.Duration != null)
                            {
                                SaveDistanceToCache(
                                    pair.origin.Latitude, pair.origin.Longitude,
                                    pair.destination.Latitude, pair.destination.Longitude,
                                    element.Distance.Value, element.Duration.Value
                                );
                            }
                        }
                    }

                    // Add delay to respect API rate limits
                    if (i + maxElementsPerRequest < uncachedPairs.Count)
                    {
                        await Task.Delay(100);
                    }
                }

                _logger.LogInformation($"Distance Matrix completed. Cached {uncachedPairs.Count} new distances.");
                return response;
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
        /// Get walking directions between two points with caching
        /// </summary>
        public async Task<DirectionsResponse> GetDirectionsAsync(DirectionsRequest request)
        {
            try
            {
                // Validate request
                if (request.Origin == null || request.Destination == null)
                    throw new ArgumentException("Origin and destination are required");

                // Check cache first
                var cachedRoute = GetCachedRoute(
                    request.Origin.Latitude,
                    request.Origin.Longitude,
                    request.Destination.Latitude,
                    request.Destination.Longitude
                );

                if (cachedRoute != null)
                {
                    _logger.LogInformation($"Using cached route from ({request.Origin.Latitude}, {request.Origin.Longitude}) to ({request.Destination.Latitude}, {request.Destination.Longitude})");
                    return cachedRoute;
                }

                // If not in cache, make API call
                _logger.LogInformation($"No cache found, calling Google Directions API");

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
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = ParseDirectionsResponse(json);

                // Save to cache if successful
                if (result.Success && result.Routes?.Count > 0)
                {
                    SaveRouteToCache(
                        request.Origin.Latitude,
                        request.Origin.Longitude,
                        request.Destination.Latitude,
                        request.Destination.Longitude,
                        result
                    );
                }

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
        /// Calculate shelter distances for a list of people and shelters
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

                // Use our enhanced distance matrix with caching
                var request = new DistanceMatrixRequest
                {
                    Origins = origins,
                    Destinations = destinations,
                    Mode = TravelMode.Walking
                };

                var response = await GetDistanceMatrixAsync(request);

                if (response.Success && response.Rows != null)
                {
                    // Process results
                    for (int i = 0; i < origins.Count; i++)
                    {
                        var originId = origins[i].Id;
                        if (!result.ContainsKey(originId))
                            result[originId] = new Dictionary<string, double>();

                        for (int j = 0; j < destinations.Count; j++)
                        {
                            var destId = destinations[j].Id;

                            if (i < response.Rows.Count && j < response.Rows[i].Elements.Count)
                            {
                                var element = response.Rows[i].Elements[j];

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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shelter distances");
                throw;
            }

            return result;
        }

        /// <summary>
        /// Get routes for assigned people
        /// </summary>
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

        #region Cache Methods

        /// <summary>
        /// Get cached distance from database
        /// </summary>
        private DistanceMatrixElement GetCachedDistance(double originLat, double originLng, double destLat, double destLng)
        {
            try
            {
                SqlConnection con = null;
                SqlCommand cmd = null;

                con = _dbService.connect("myProjDB");

                var parameters = new Dictionary<string, object>
                {
                    {"@OriginLat", originLat},
                    {"@OriginLng", originLng},
                    {"@DestLat", destLat},
                    {"@DestLng", destLng},
                    {"@ToleranceMeters", 50}
                };

                cmd = CreateCommandWithStoredProcedure("FC_SP_GetCachedDistance", con, parameters);

                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                if (dr.Read())
                {
                    return new DistanceMatrixElement
                    {
                        Status = "OK",
                        Distance = new DistanceInfo
                        {
                            Value = Convert.ToInt32(dr["distance_meters"]),
                            Text = $"{dr["distance_meters"]} m"
                        },
                        Duration = new DurationInfo
                        {
                            Value = Convert.ToInt32(dr["duration_seconds"]),
                            Text = $"{dr["duration_seconds"]} sec"
                        }
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached distance");
                return null;
            }
        }

        /// <summary>
        /// Save distance to cache
        /// </summary>
        private void SaveDistanceToCache(double originLat, double originLng, double destLat, double destLng, int distanceMeters, int durationSeconds)
        {
            try
            {
                SqlConnection con = null;
                SqlCommand cmd = null;

                con = _dbService.connect("myProjDB");

                var parameters = new Dictionary<string, object>
                {
                    {"@OriginLat", originLat},
                    {"@OriginLng", originLng},
                    {"@DestLat", destLat},
                    {"@DestLng", destLng},
                    {"@DistanceMeters", distanceMeters},
                    {"@DurationSeconds", durationSeconds}
                };

                cmd = CreateCommandWithStoredProcedure("FC_SP_SaveDistanceToCache", con, parameters);
                cmd.ExecuteNonQuery();

                _logger.LogInformation($"Distance cached: ({originLat}, {originLng}) to ({destLat}, {destLng}) = {distanceMeters}m");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving distance to cache");
            }
        }

        /// <summary>
        /// Get cached route from database
        /// </summary>
        private DirectionsResponse GetCachedRoute(double originLat, double originLng, double destLat, double destLng)
        {
            try
            {
                SqlConnection con = null;
                SqlCommand cmd = null;

                con = _dbService.connect("myProjDB");

                // Prepare parameters
                var parameters = new Dictionary<string, object>
                {
                    {"@OriginLat", originLat},
                    {"@OriginLng", originLng},
                    {"@DestLat", destLat},
                    {"@DestLng", destLng},
                    {"@Tolerance", 0.0001} // About 11 meters tolerance
                };

                cmd = CreateCommandWithStoredProcedure("FC_SP_GetCachedRoute", con, parameters);

                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                if (dr.Read())
                {
                    // Found cached route
                    var response = new DirectionsResponse
                    {
                        Success = true,
                        Status = "OK",
                        Routes = new List<ServerSimulation.Models.Route>()
                    };

                    var route = new ServerSimulation.Models.Route
                    {
                        Summary = "Cached route",
                        OverviewPolyline = dr["route_polyline"] as string,
                        Legs = new List<Leg>()
                    };

                    var leg = new Leg
                    {
                        Distance = new DistanceInfo
                        {
                            Value = Convert.ToInt32(dr["distance_meters"]),
                            Text = dr["distance_text"] as string ?? $"{dr["distance_meters"]} m"
                        },
                        Duration = new DurationInfo
                        {
                            Value = Convert.ToInt32(dr["duration_seconds"]),
                            Text = dr["duration_text"] as string ?? $"{dr["duration_seconds"]} sec"
                        },
                        StartLocation = new LocationPoint(originLat, originLng),
                        EndLocation = new LocationPoint(destLat, destLng)
                    };

                    route.Legs.Add(leg);
                    response.Routes.Add(route);

                    return response;
                }

                return null; // No cache found
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached route");
                return null;
            }
        }

        /// <summary>
        /// Save route to cache in database
        /// </summary>
        private void SaveRouteToCache(double originLat, double originLng, double destLat, double destLng, DirectionsResponse response)
        {
            try
            {
                if (response.Routes == null || response.Routes.Count == 0) return;

                var route = response.Routes[0];
                if (route.Legs == null || route.Legs.Count == 0) return;

                var leg = route.Legs[0];

                SqlConnection con = null;
                SqlCommand cmd = null;

                con = _dbService.connect("myProjDB");

                var parameters = new Dictionary<string, object>
                {
                    {"@OriginLat", originLat},
                    {"@OriginLng", originLng},
                    {"@DestLat", destLat},
                    {"@DestLng", destLng},
                    {"@DistanceMeters", leg.Distance?.Value ?? 0},
                    {"@DurationSeconds", leg.Duration?.Value ?? 0},
                    {"@DistanceText", leg.Distance?.Text},
                    {"@DurationText", leg.Duration?.Text},
                    {"@RoutePolyline", route.OverviewPolyline}
                };

                cmd = CreateCommandWithStoredProcedure("FC_SP_SaveRouteToCache", con, parameters);
                cmd.ExecuteNonQuery();

                _logger.LogInformation($"Route cached successfully from ({originLat}, {originLng}) to ({destLat}, {destLng})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving route to cache");
                // Don't throw - caching failure shouldn't break the main flow
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to create SQL command with stored procedure
        /// </summary>
        private SqlCommand CreateCommandWithStoredProcedure(string spName, SqlConnection con, Dictionary<string, object> parameters)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = spName;
            cmd.CommandTimeout = 10;
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            return cmd;
        }

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

        /// <summary>
        /// Helper class to compare LocationPoint objects
        /// </summary>
        public class LocationPointComparer : IEqualityComparer<LocationPoint>
        {
            public bool Equals(LocationPoint x, LocationPoint y)
            {
                if (x == null || y == null) return false;
                return Math.Abs(x.Latitude - y.Latitude) < 0.0001 &&
                       Math.Abs(x.Longitude - y.Longitude) < 0.0001;
            }

            public int GetHashCode(LocationPoint obj)
            {
                return $"{obj.Latitude:F4},{obj.Longitude:F4}".GetHashCode();
            }
        }
    }
}