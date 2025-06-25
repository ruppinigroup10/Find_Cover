using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FC_Server.Models;
using FC_Server.DAL;

namespace FC_Server.Services
{
    /// <summary>
    /// שירות מעקב אחר מיקום משתמשים
    /// </summary>
    public class UserLocationTrackingService
    {
        private readonly DBservicesLocation _dbLocation;
        private readonly ILogger<UserLocationTrackingService> _logger;
        private readonly IGoogleMapsService _googleMapsService;
        private const double ARRIVAL_THRESHOLD_KM = 0.01; // 10 מטר
        private const double ROUTE_DEVIATION_THRESHOLD_KM = 0.1; // 100 מטר

        public UserLocationTrackingService(
            ILogger<UserLocationTrackingService> logger,
            IGoogleMapsService googleMapsService,
            DBservicesLocation dbLocation)
        {
            _logger = logger;
            _googleMapsService = googleMapsService;
            _dbLocation = new DBservicesLocation();
        }

        /// <summary>
        /// התחלת מעקב אחר משתמש
        /// </summary>
        public async Task StartTrackingUser(int userId, int shelterId, double initialLat, double initialLon)
        {
            try
            {
                // שמור מיקום ראשוני
                await _dbLocation.SaveUserLocation(userId, initialLat, initialLon);

                _logger.LogInformation($"Started tracking user {userId} to shelter {shelterId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting tracking for user {userId}", userId);
                throw;
            }
        }

        /// <summary>
        /// עדכון מיקום משתמש
        /// </summary>
        public async Task<TrackingResult> UpdateUserLocation(int userId, double lat, double lon)
        {
            try
            {
                // שמור מיקום (במשורה - רק פעם בדקה)
                if (ShouldSaveLocation())
                {
                    await _dbLocation.SaveUserLocation(userId, lat, lon);
                }

                // בדוק אם המשתמש בהקצאה פעילה
                DBservicesShelter dbShelter = new DBservicesShelter();
                var activeVisit = dbShelter.GetActiveUserAllocation(userId);

                if (activeVisit == null)
                {
                    return new TrackingResult { IsBeingTracked = false };
                }

                // קבלת פרטי המרחב המוגן
                var shelter = Shelter.getShelter(activeVisit.shelter_id);
                if (shelter == null)
                {
                    throw new Exception($"Shelter {activeVisit.shelter_id} not found");
                }

                // חישוב מרחק למרחב המוגן
                var distanceToShelter = CalculateDistance(lat, lon, shelter.latitude, shelter.longitude);

                var result = new TrackingResult
                {
                    IsBeingTracked = true,
                    ShelterId = activeVisit.shelter_id,
                    DistanceToShelter = distanceToShelter,
                    HasArrivedAtShelter = distanceToShelter <= ARRIVAL_THRESHOLD_KM
                };

                // בדיקה אם הגיע
                if (result.HasArrivedAtShelter && activeVisit.status != "ARRIVED")
                {
                    dbShelter.UpdateVisitStatus(userId, activeVisit.shelter_id, "ARRIVED");
                    _logger.LogInformation($"User {userId} arrived at shelter {activeVisit.shelter_id}");
                }

                // חישוב זמן הגעה משוער
                if (!result.HasArrivedAtShelter)
                {
                    result.EstimatedTimeToArrival = CalculateEstimatedArrival(distanceToShelter);
                    result.CurrentNavigationInstruction = await GetCurrentInstruction(lat, lon, shelter);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location for user {userId}", userId);
                throw;
            }
        }

        /// <summary>
        /// קבלת מיקום אחרון ידוע
        /// </summary>
        public async Task<UserLocationData> GetLastKnownLocation(int userId)
        {
            try
            {
                var lastLocation = await _dbLocation.GetLastKnownLocation(userId);

                if (lastLocation.lat.HasValue && lastLocation.lon.HasValue)
                {
                    return new UserLocationData
                    {
                        UserId = userId,
                        Latitude = lastLocation.lat.Value,
                        Longitude = lastLocation.lon.Value,
                        LastUpdateTime = lastLocation.time ?? DateTime.Now,
                        Status = await GetUserTrackingStatus(userId)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last location for user {userId}", userId);
                throw;
            }
        }

        /// <summary>
        /// עצירת מעקב
        /// </summary>
        public async Task StopTrackingUser(int userId)
        {
            try
            {
                _logger.LogInformation($"Stopped tracking user {userId}");
                // לא צריך לעשות כלום מיוחד - הטבלה מתנקה אוטומטית
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping tracking for user {userId}", userId);
                throw;
            }
        }

        /// <summary>
        /// עדכון סטטוס מיקום
        /// </summary>
        public async Task UpdateLocationStatus(int userId, UserLocationStatus status)
        {
            try
            {
                // כאן אפשר להוסיף לוגיקה נוספת אם צריך
                _logger.LogInformation($"User {userId} location status updated: " +
                    $"Distance: {status.DistanceToShelter}km, Arrived: {status.HasArrivedAtShelter}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location status for user {userId}", userId);
                throw;
            }
        }

        #region Private Methods

        private DateTime _lastLocationSave = DateTime.MinValue;

        private bool ShouldSaveLocation()
        {
            // שמור רק פעם בדקה
            if (DateTime.Now - _lastLocationSave > TimeSpan.FromMinutes(1))
            {
                _lastLocationSave = DateTime.Now;
                return true;
            }
            return false;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private TimeSpan CalculateEstimatedArrival(double distanceKm)
        {
            const double WALKING_SPEED_KMH = 5.0; // מהירות הליכה ממוצעת
            var hoursToArrival = distanceKm / WALKING_SPEED_KMH;
            return TimeSpan.FromHours(hoursToArrival);
        }

        private async Task<string> GetCurrentInstruction(double lat, double lon, Shelter shelter)
        {
            try
            {
                var distance = CalculateDistance(lat, lon, shelter.latitude, shelter.longitude);

                if (distance < 0.05) // 50 מטר
                    return "המרחב המוגן ממש קרוב, חפש את הכניסה";
                else if (distance < 0.1) // 100 מטר
                    return "המשך ישר, המרחב המוגן במרחק של כ-100 מטר";
                else if (distance < 0.2) // 200 מטר
                    return "המשך בכיוון המרחב המוגן, כ-200 מטר";
                else
                    return $"המשך לכיוון המרחב המוגן, {distance:F2} ק\"מ";
            }
            catch
            {
                return "המשך לכיוון המרחב המוגן";
            }
        }

        private async Task<string> GetUserTrackingStatus(int userId)
        {
            try
            {
                DBservicesShelter dbShelter = new DBservicesShelter();
                var activeVisit = dbShelter.GetActiveUserAllocation(userId);
                return activeVisit?.status ?? "IDLE";
            }
            catch
            {
                return "IDLE";
            }
        }

        #endregion
    }
}