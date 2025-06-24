using FC_Server.DAL;
using System;

namespace FC_Server.Models
{
    public class AlertManager
    {
        private readonly DBservicesAlertZones _dbZones;

        public AlertManager()
        {
            _dbZones = new DBservicesAlertZones();
        }

        // בדיקה אם יש התרעה פעילה למשתמש לפי המיקום האחרון שלו
        public AlertCheckResult CheckActiveAlertForUser(int userId)
        {
            var result = new AlertCheckResult();

            try
            {
                // שלב 1: מצא את המיקום האחרון של המשתמש
                var lastLocation = _dbZones.GetUserLastLocation(userId);

                if (lastLocation == null)
                {
                    result.IsInAlertZone = false;
                    result.Message = "לא נמצא מיקום אחרון למשתמש";
                    return result;
                }

                // שלב 2: בדוק באיזה אזור התרעה המשתמש נמצא
                var userZone = _dbZones.GetZoneByLocation(lastLocation.Latitude, lastLocation.Longitude);

                if (userZone == null)
                {
                    result.IsInAlertZone = false;
                    result.Message = "המיקום האחרון שלך אינו באזור התרעה";
                    return result;
                }

                result.IsInAlertZone = true;
                result.ZoneName = userZone.ZoneName;
                result.ResponseTime = userZone.ResponseTime;

                // שלב 3: בדוק אם יש התרעה פעילה באזור
                var activeAlert = _dbZones.GetActiveAlertForZone(userZone.ZoneName);

                if (activeAlert.IsActive)
                {
                    result.HasActiveAlert = true;
                    result.AlertTime = activeAlert.AlertTime;

                    // חשב כמה זמן נשאר
                    var timeElapsed = DateTime.Now - activeAlert.AlertTime.Value;
                    var timeRemaining = userZone.ResponseTime - (int)timeElapsed.TotalSeconds;

                    if (timeRemaining > 0)
                    {
                        result.Message = $"🚨 התרעה פעילה באזור {userZone.ZoneName}! נשארו {timeRemaining} שניות להגיע למרחב מוגן";
                    }
                    else
                    {
                        result.Message = $"⚠️ התרעה פעילה באזור {userZone.ZoneName}! יש להימצא במרחב מוגן";
                    }
                }
                else
                {
                    result.HasActiveAlert = false;
                    result.Message = $"אין התרעה פעילה באזור {userZone.ZoneName}";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Message = "שגיאה בבדיקת התרעה: " + ex.Message;
                return result;
            }
        }

        // בדיקה אם יש התרעה פעילה במיקום ספציפי
        public AlertCheckResult CheckActiveAlertForLocation(double latitude, double longitude)
        {
            var result = new AlertCheckResult();

            // מצא את האזור של המיקום
            var userZone = _dbZones.GetZoneByLocation(latitude, longitude);

            if (userZone == null)
            {
                result.IsInAlertZone = false;
                result.Message = "המיקום אינו באזור התרעה";
                return result;
            }

            result.IsInAlertZone = true;
            result.ZoneName = userZone.ZoneName;
            result.ResponseTime = userZone.ResponseTime;

            // בדוק אם יש התרעה פעילה באזור
            var activeAlert = _dbZones.GetActiveAlertForZone(userZone.ZoneName);

            if (activeAlert.IsActive)
            {
                result.HasActiveAlert = true;
                result.AlertTime = activeAlert.AlertTime;

                // חשב כמה זמן נשאר
                var timeElapsed = DateTime.Now - activeAlert.AlertTime.Value;
                var timeRemaining = userZone.ResponseTime - (int)timeElapsed.TotalSeconds;

                if (timeRemaining > 0)
                {
                    result.Message = $"התרעה פעילה באזור {userZone.ZoneName}! נשארו {timeRemaining} שניות להגיע למרחב מוגן";
                }
                else
                {
                    result.Message = $"התרעה פעילה באזור {userZone.ZoneName}! יש להימצא במרחב מוגן";
                }
            }
            else
            {
                result.HasActiveAlert = false;
                result.Message = $"אין התרעה פעילה באזור {userZone.ZoneName}";
            }

            return result;
        }

        // מביא את כל האזורים (לתצוגה במפה)
        public System.Collections.Generic.List<AlertZone> GetAllZones()
        {
            return _dbZones.GetAllAlertZones();
        }

        // בודק אזור לפי שם (להשוואה עם API התרעות)
        public AlertZone GetZoneByAlertName(string alertAreaName)
        {
            return _dbZones.GetZoneByName(alertAreaName);
        }

        // עדכון מיקום משתמש
        public void UpdateUserLocation(int userId, double latitude, double longitude)
        {
            var location = new UserLocation
            {
                UserId = userId,
                Latitude = latitude,
                Longitude = longitude,
                CreatedAt = DateTime.Now
            };

            _dbZones.UpdateUserLocation(location);
        }
    }
}