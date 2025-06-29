using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FC_Server.Models;

namespace FC_Server.DAL
{
    /// <summary>
    /// ממשק לשירות Google Maps
    /// </summary>
    public interface IGoogleMapsService
    {
        Task<DirectionsResponse> GetDirectionsAsync(DirectionsRequest request);
        Task<Dictionary<string, Dictionary<string, double>>> CalculateShelterDistancesAsync(
            List<PersonDto> people,
            List<ShelterDto> shelters);
        Task<Dictionary<string, DirectionsResponse>> GetRoutesForPeople(
            List<PersonDto> people,
            List<ShelterDto> shelters,
            Dictionary<int, AssignmentDto> assignments);

        //for geolocation
        Task<(float latitude, float longitude)?> GetCoordinatesFromAddressAsync(string address);
        Task<string> GetAddressFromCoordinatesAsync(float latitude, float longitude);
    }

    /// <summary>
    /// שירות לתקשורת עם Google Maps API
    /// מספק מרחקי הליכה ונתיבים למרחבים מוגנים
    /// </summary>
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleMapsService> _logger;
        private readonly string _apiKey;
        private const string DISTANCE_MATRIX_URL = "https://maps.googleapis.com/maps/api/distancematrix/json";
        private const string DIRECTIONS_URL = "https://maps.googleapis.com/maps/api/directions/json";
        private const string GEOCODE_URL = "https://maps.googleapis.com/maps/api/geocode/json";

        public GoogleMapsService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["GoogleMaps:ApiKey"];

            // FORCE SET THE TIMEOUT - This should override any configuration
            if (_httpClient.Timeout < TimeSpan.FromSeconds(30))
            {
                _logger.LogWarning($"HttpClient timeout was {_httpClient.Timeout.TotalMilliseconds}ms, overriding to 30 seconds");
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
            }

            _logger.LogInformation($"GoogleMapsService initialized with timeout: {_httpClient.Timeout.TotalSeconds} seconds");


            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Google Maps API key is not configured");
            }
        }

        /// <summary>
        /// חישוב מרחקי הליכה בין משתמשים למרחבים מוגנים
        /// </summary>
        public async Task<Dictionary<string, Dictionary<string, double>>> CalculateShelterDistancesAsync(
            List<PersonDto> people,
            List<ShelterDto> shelters)
        {
            var distances = new Dictionary<string, Dictionary<string, double>>();

            try
            {
                // קבוצת בקשות לפי מגבלות Google API (מקסימום 25 origins * destinations)
                const int maxElementsPerRequest = 25;
                const int maxOriginsPerRequest = 10;
                const int maxDestinationsPerRequest = 10;

                // חלוקה לקבוצות
                var peopleBatches = people.Batch(maxOriginsPerRequest).ToList();
                var shelterBatches = shelters.Batch(maxDestinationsPerRequest).ToList();

                foreach (var peopleBatch in peopleBatches)
                {
                    foreach (var shelterBatch in shelterBatches)
                    {
                        var batchDistances = await GetDistanceMatrixBatch(
                            peopleBatch.ToList(),
                            shelterBatch.ToList());

                        // מיזוג התוצאות
                        foreach (var personEntry in batchDistances)
                        {
                            if (!distances.ContainsKey(personEntry.Key))
                            {
                                distances[personEntry.Key] = new Dictionary<string, double>();
                            }

                            foreach (var shelterEntry in personEntry.Value)
                            {
                                distances[personEntry.Key][shelterEntry.Key] = shelterEntry.Value;
                            }
                        }

                        // השהייה קטנה בין בקשות למניעת חסימה
                        await Task.Delay(100);
                    }
                }

                return distances;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shelter distances");
                throw;
            }
        }

        /// <summary>
        /// קבלת נתיבי הליכה עבור ההקצאות
        /// </summary>
        public async Task<Dictionary<string, DirectionsResponse>> GetRoutesForPeople(
            List<PersonDto> people,
            List<ShelterDto> shelters,
            Dictionary<int, AssignmentDto> assignments)
        {
            var routes = new Dictionary<string, DirectionsResponse>();

            try
            {
                foreach (var assignment in assignments)
                {
                    var person = people.FirstOrDefault(p => p.Id == assignment.Key);
                    var shelter = shelters.FirstOrDefault(s => s.Id == assignment.Value.ShelterId);

                    if (person != null && shelter != null)
                    {
                        var routeKey = $"{person.Id}-{shelter.Id}";

                        var request = new DirectionsRequest
                        {
                            Origin = new LocationPoint(person.Latitude, person.Longitude, $"person-{person.Id}"),
                            Destination = new LocationPoint(shelter.Latitude, shelter.Longitude, $"shelter-{shelter.Id}"),
                            Mode = TravelMode.Walking,
                            Language = "he" // עברית
                        };

                        var response = await GetDirectionsAsync(request);
                        routes[routeKey] = response;

                        // השהייה קטנה בין בקשות
                        await Task.Delay(50);
                    }
                }

                return routes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting routes for people");
                throw;
            }
        }

        /// <summary>
        /// קבלת הוראות הליכה בין שתי נקודות
        /// </summary>
        public async Task<DirectionsResponse> GetDirectionsAsync(DirectionsRequest request)
        {
            _logger.LogInformation($"Getting directions from ({request.Origin.Latitude}, {request.Origin.Longitude}) to ({request.Destination.Latitude}, {request.Destination.Longitude})");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation($"Getting directions from ({request.Origin.Latitude}, {request.Origin.Longitude}) to ({request.Destination.Latitude}, {request.Destination.Longitude})");
            _logger.LogInformation($"HttpClient timeout is: {_httpClient.Timeout.TotalMilliseconds}ms");

            try
            {
                var origin = $"{request.Origin.Latitude},{request.Origin.Longitude}";
                var destination = $"{request.Destination.Latitude},{request.Destination.Longitude}";

                var url = $"{DIRECTIONS_URL}?" +
                         $"origin={origin}&" +
                         $"destination={destination}&" +
                         $"mode=walking&" +
                         $"language={request.Language ?? "he"}&" +
                         $"key={_apiKey}";

                _logger.LogInformation($"Calling Google Maps API...");
                var response = await _httpClient.GetAsync(url);

                stopwatch.Stop();
                _logger.LogInformation($"Google Maps API responded in {stopwatch.ElapsedMilliseconds}ms");

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Google Directions API error: {response.StatusCode} - {content}");
                    return new DirectionsResponse
                    {
                        Success = false,
                        ErrorMessage = $"API returned status: {response.StatusCode}"
                    };
                }

                return ParseDirectionsResponse(content);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Google Maps request timed out after {_httpClient.Timeout.TotalSeconds} seconds");
                _logger.LogError(ex, "Error calling Google Directions API");
                return new DirectionsResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }


        /// <summary>
        /// geocoding methods - start
        /// </summary>
        public async Task<(float latitude, float longitude)?> GetCoordinatesFromAddressAsync(string address)
        {
            try
            {
                // Add Israel to the address for better results if not already present
                if (!address.Contains("ישראל") && !address.Contains("Israel"))
                {
                    address += ", ישראל";
                }

                var queryParams = new Dictionary<string, string>
                {
                    ["address"] = address,
                    ["key"] = _apiKey,
                    ["language"] = "he"
                };

                var url = BuildUrl(GEOCODE_URL, queryParams);

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jObject = JObject.Parse(json);

                    if (jObject["status"]?.ToString() == "OK" && jObject["results"].HasValues)
                    {
                        var location = jObject["results"][0]["geometry"]["location"];
                        float lat = float.Parse(location["lat"].ToString());
                        float lng = float.Parse(location["lng"].ToString());

                        _logger.LogInformation($"Successfully geocoded address: {address} to {lat}, {lng}");
                        return (lat, lng);
                    }
                }

                _logger.LogWarning($"Failed to geocode address: {address}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error geocoding address: {address}");
            }

            return null;
        }

        public async Task<string> GetAddressFromCoordinatesAsync(float latitude, float longitude)
        {
            try
            {
                var queryParams = new Dictionary<string, string>
                {
                    ["latlng"] = $"{latitude},{longitude}",
                    ["key"] = _apiKey,
                    ["language"] = "he"
                };

                var url = BuildUrl(GEOCODE_URL, queryParams);

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jObject = JObject.Parse(json);

                    if (jObject["status"]?.ToString() == "OK" && jObject["results"].HasValues)
                    {
                        var address = jObject["results"][0]["formatted_address"].ToString();
                        _logger.LogInformation($"Successfully reverse geocoded {latitude}, {longitude} to: {address}");
                        return address;
                    }
                }

                _logger.LogWarning($"Failed to reverse geocode coordinates: {latitude}, {longitude}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reverse geocoding: {latitude}, {longitude}");
            }

            return "";
        }

        private string BuildUrl(string baseUrl, Dictionary<string, string> queryParams)
        {
            var query = string.Join("&", queryParams.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            return $"{baseUrl}?{query}";
        }

        /// <summary>
        /// geocoding methods - end
        /// </summary>


        #region Private Methods

        private async Task<Dictionary<string, Dictionary<string, double>>> GetDistanceMatrixBatch(
            List<PersonDto> people,
            List<ShelterDto> shelters)
        {
            var distances = new Dictionary<string, Dictionary<string, double>>();
            string url = "";

            try
            {

                // בניית רשימת origins
                var origins = string.Join("|", people.Select(p => $"{p.Latitude},{p.Longitude}"));

                // בניית רשימת destinations
                var destinations = string.Join("|", shelters.Select(s => $"{s.Latitude},{s.Longitude}"));

                url = $"{DISTANCE_MATRIX_URL}?" +
                         $"origins={origins}&" +
                         $"destinations={destinations}&" +
                         $"mode=walking&" +
                         $"units=metric&" +
                         $"key={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Google Distance Matrix API error: {response.StatusCode} - {content}");
                    _logger.LogError($"Request URL: {url}"); // Now url is available here
                    return distances;
                }

                var result = ParseDistanceMatrixResponse(content);

                if (result.Success && result.Rows != null)
                {
                    for (int i = 0; i < people.Count && i < result.Rows.Count; i++)
                    {
                        var personId = people[i].Id.ToString();
                        distances[personId] = new Dictionary<string, double>();

                        var row = result.Rows[i];
                        if (row.Elements != null)
                        {
                            for (int j = 0; j < shelters.Count && j < row.Elements.Count; j++)
                            {
                                var element = row.Elements[j];
                                var shelterId = shelters[j].Id.ToString();

                                if (element.Status == "OK" && element.Distance != null)
                                {
                                    // המרה ממטרים לקילומטרים
                                    distances[personId][shelterId] = element.Distance.Value / 1000.0;
                                }
                                else
                                {
                                    // אם אין נתיב הליכה, חשב מרחק אווירי
                                    distances[personId][shelterId] = CalculateAerialDistance(
                                        people[i].Latitude, people[i].Longitude,
                                        shelters[j].Latitude, shelters[j].Longitude);
                                }
                            }
                        }
                    }
                }

                return distances;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDistanceMatrixBatch");
                return distances;
            }
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

                if (response.Status != "OK")
                {
                    response.Success = false;
                    response.ErrorMessage = $"API returned status: {response.Status}";
                    return response;
                }

                response.Success = true;

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
                    Routes = new List<FC_Server.Models.Route>()
                };

                if (response.Status != "OK")
                {
                    response.Success = false;
                    response.ErrorMessage = $"Google API returned status: {response.Status}";

                    if (jObject["error_message"] != null)
                        response.ErrorMessage += $" - {jObject["error_message"]}";

                    return response;
                }

                response.Success = true;

                var routes = jObject["routes"] as JArray;
                if (routes != null && routes.Any())
                {
                    foreach (var route in routes)
                    {
                        var routeObj = new FC_Server.Models.Route
                        {
                            Summary = route["summary"]?.ToString(),
                            OverviewPolyline = route["overview_polyline"]?["points"]?.ToString(),
                            Legs = new List<RouteLeg>()
                        };

                        var legs = route["legs"] as JArray;
                        if (legs != null)
                        {
                            foreach (var leg in legs)
                            {
                                var legObj = new RouteLeg
                                {
                                    Distance = new DistanceInfo
                                    {
                                        Text = leg["distance"]?["text"]?.ToString(),
                                        Value = leg["distance"]?["value"]?.Value<int>() ?? 0
                                    },
                                    Duration = new DurationInfo
                                    {
                                        Text = leg["duration"]?["text"]?.ToString(),
                                        Value = leg["duration"]?["value"]?.Value<int>() ?? 0
                                    },
                                    StartAddress = leg["start_address"]?.ToString(),
                                    EndAddress = leg["end_address"]?.ToString(),
                                    Steps = new List<RouteStep>()
                                };

                                var steps = leg["steps"] as JArray;
                                if (steps != null)
                                {
                                    foreach (var step in steps)
                                    {
                                        var stepObj = new RouteStep
                                        {
                                            HtmlInstructions = step["html_instructions"]?.ToString(),
                                            Distance = new DistanceInfo
                                            {
                                                Text = step["distance"]?["text"]?.ToString(),
                                                Value = step["distance"]?["value"]?.Value<int>() ?? 0
                                            },
                                            Duration = new DurationInfo
                                            {
                                                Text = step["duration"]?["text"]?.ToString(),
                                                Value = step["duration"]?["value"]?.Value<int>() ?? 0
                                            }
                                        };

                                        legObj.Steps.Add(stepObj);
                                    }
                                }

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

        private double CalculateAerialDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // רדיוס כדור הארץ בק"מ
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        #endregion
    }

    #region Extension Methods

    public static class IEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return YieldBatchElements(enumerator, batchSize - 1);
                }
            }
        }

        private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 0; i < batchSize && source.MoveNext(); i++)
            {
                yield return source.Current;
            }
        }
    }

    #endregion
}