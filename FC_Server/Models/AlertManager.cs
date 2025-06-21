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

        // בודק אם יש התרעה פעילה במיקום המשתמש
        public AlertCheckResult CheckActiveAlertForLocation(double latitude, double longitude)
        {
            var result = new AlertCheckResult();

            // מצא את האזור של המשתמש
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
    }
}