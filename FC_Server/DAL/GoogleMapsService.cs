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

        public GoogleMapsService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["GoogleMaps:ApiKey"];

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

                var response = await _httpClient.GetAsync(url);
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
                _logger.LogError(ex, "Error calling Google Directions API");
                return new DirectionsResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #region Private Methods

        private async Task<Dictionary<string, Dictionary<string, double>>> GetDistanceMatrixBatch(
            List<PersonDto> people,
            List<ShelterDto> shelters)
        {
            var distances = new Dictionary<string, Dictionary<string, double>>();

            try
            {
                // בניית רשימת origins
                var origins = string.Join("|", people.Select(p => $"{p.Latitude},{p.Longitude}"));

                // בניית רשימת destinations
                var destinations = string.Join("|", shelters.Select(s => $"{s.Latitude},{s.Longitude}"));

                var url = $"{DISTANCE_MATRIX_URL}?" +
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