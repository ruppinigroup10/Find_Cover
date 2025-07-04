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
    /// שירות ניהול התראות חירום
    /// </summary>
    public class EmergencyAlertService
    {
        private readonly ILogger<EmergencyAlertService> _logger;
        private readonly DBservicesAlert _dbAlert;

        public EmergencyAlertService(ILogger<EmergencyAlertService> logger)
        {
            _logger = logger;
            _dbAlert = new DBservicesAlert();
        }

        /// <summary>
        /// בדיקה אם יש התראה פעילה במיקום מסוים
        /// </summary>
        public async Task<ActiveAlert> GetActiveAlertForLocation(double lat, double lon)
        {
            try
            {
                // Call the method that returns ActiveAlert
                var alert = await _dbAlert.GetActiveAlertForLocation(lat, lon);

                if (alert != null)
                {
                    _logger.LogInformation($"Found active alert: {alert.AlertType} (ID: {alert.AlertId}) at location ({lat}, {lon})");
                }
                else
                {
                    _logger.LogInformation($"No active alerts found for location ({lat}, {lon})");
                }

                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active alert for location ({Lat}, {Lon})", lat, lon);
                return null; // במקרה של שגיאה, נניח שאין התראה
            }
        }

        /// <summary>
        /// קבלת סטטוס התראה
        /// </summary>
        public async Task<AlertStatus> GetAlertStatus(int alertId)
        {
            try
            {
                var status = await _dbAlert.GetAlertStatus(alertId);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert status for alert {AlertId}", alertId);
                return null;
            }
        }


        /// <summary>
        /// יצירת התראה חדשה
        /// </summary>
        public async Task<int> CreateEmergencyAlert(EmergencyAlertRequest request)
        {
            try
            {
                var newAlert = new AlertRecord
                {
                    alert_type = request.AlertType,
                    CenterLatitude = request.CenterLatitude,
                    CenterLongitude = request.CenterLongitude,
                    RadiusKm = request.RadiusKm,
                    created_at = DateTime.Now,
                    is_active = true,
                    created_by = request.CreatedBy ?? "System",
                    alert_source = "EmergencySystem"
                };

                var alertId = _dbAlert.CreateAlertAsync(newAlert);

                _logger.LogInformation($"Created new emergency alert {alertId} at ({request.CenterLatitude}, {request.CenterLongitude})");

                return alertId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating emergency alert");
                throw;
            }
        }

        /// <summary>
        /// סיום התראה ושחרור כל המשתמשים
        /// </summary>
        public async Task EndEmergencyAlert(int alertId)
        {
            try
            {
                // עדכון סטטוס ההתראה
                await _dbAlert.EndAlertAsync(alertId);

                // שחרור אוטומטי של כל המשתמשים
                await ReleaseAllUsersFromAlert(alertId);

                _logger.LogInformation($"Ended emergency alert {alertId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending alert {AlertId}", alertId);
                throw;
            }
        }

        /// <summary>
        /// קבלת כל ההתראות הפעילות
        /// </summary>
        public async Task<List<ActiveEmergencyAlert>> GetAllActiveAlerts()
        {
            try
            {
                var alerts = await _dbAlert.GetActiveAlertsAsync();

                return alerts.Select(a => new ActiveEmergencyAlert
                {
                    AlertId = a.alert_id,
                    AlertType = a.alert_type,
                    CenterLatitude = a.CenterLatitude != 0 ? a.CenterLatitude : 31.2518,
                    CenterLongitude = a.CenterLongitude != 0 ? a.CenterLongitude : 34.7913,
                    RadiusKm = a.RadiusKm > 0 ? a.RadiusKm : 10,
                    StartTime = a.created_at,
                    IsActive = a.is_active
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all active alerts");
                throw;
            }
        }

        #region Private Methods

        private async Task ReleaseAllUsersFromAlert(int alertId)
        {
            try
            {
                // קבלת כל המשתמשים המושפעים מההתראה
                var affectedUsers = await GetUsersInAlert(alertId);

                DBservicesShelter shelterDb = new DBservicesShelter();

                foreach (var userId in affectedUsers)
                {
                    try
                    {
                        shelterDb.ReleaseUserFromShelter(userId);
                        _logger.LogInformation($"Released user {userId} from alert {alertId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error releasing user {UserId} from alert {AlertId}",
                            userId, alertId);
                        // ממשיך לשחרר משתמשים אחרים גם אם יש שגיאה
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing users from alert {AlertId}", alertId);
            }
        }

        private async Task<int> GetAffectedUsersCount(int alertId)
        {
            try
            {
                var users = await GetUsersInAlert(alertId);
                return users.Count;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<List<int>> GetUsersInAlert(int alertId)
        {
            return await Task.Run(() =>
            {
                var users = new List<int>();
                try
                {
                    DBservicesShelter shelterDb = new DBservicesShelter();
                    // כאן צריך לממש שאילתה לקבלת משתמשים בהתראה
                    // לדוגמה: SELECT DISTINCT user_id FROM shelter_visit WHERE alert_id = @alertId
                    // users = shelterDb.GetUsersInAlert(alertId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting users in alert {AlertId}", alertId);
                }
                return users;
            });
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

        #endregion
    }
}