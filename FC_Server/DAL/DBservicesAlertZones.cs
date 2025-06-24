using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using FC_Server.Models;
using Microsoft.Extensions.Configuration;

namespace FC_Server.DAL
{
    public class DBservicesAlertZones
    {
        // שימוש באותה מתודת חיבור כמו בפרויקט שלך
        public SqlConnection connect(String conString)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json").Build();
            string cStr = configuration.GetConnectionString("myProjDB");
            SqlConnection con = new SqlConnection(cStr);
            con.Open();
            return con;
        }

        // מביא את כל האזורים הפעילים
        public List<AlertZone> GetAllAlertZones()
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            List<AlertZone> zones = new List<AlertZone>();

            try
            {
                con = connect("myProjDB");

                string query = @"
                    SELECT zone_id, zone_name, polygon_coordinates, response_time, is_active 
                    FROM FC_ALERT_ZONES 
                    WHERE is_active = 1
                    ORDER BY zone_name";

                cmd = new SqlCommand(query, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    zones.Add(new AlertZone
                    {
                        ZoneId = Convert.ToInt32(dr["zone_id"]),
                        ZoneName = dr["zone_name"].ToString(),
                        PolygonCoordinates = dr["polygon_coordinates"].ToString(),
                        ResponseTime = Convert.ToInt32(dr["response_time"]),
                        IsActive = Convert.ToBoolean(dr["is_active"])
                    });
                }

                return zones;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get alert zones: " + ex.Message);
            }
            finally
            {
                if (con != null)
                    con.Close();
            }
        }

        // מוצא אזור לפי מיקום
        public AlertZone GetZoneByLocation(double latitude, double longitude)
        {
            var zones = GetAllAlertZones();

            foreach (var zone in zones)
            {
                if (zone.ContainsPoint(latitude, longitude))
                {
                    return zone;
                }
            }

            return null;
        }

        // מוצא אזור לפי שם
        public AlertZone GetZoneByName(string zoneName)
        {
            var zones = GetAllAlertZones();

            return zones.FirstOrDefault(z => z.MatchesAlertAreaName(zoneName));
        }

        // בודק אם יש התרעה פעילה לאזור מסוים
        public ActiveAlertInfo GetActiveAlertForZone(string zoneName)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;

            try
            {
                con = connect("myProjDB");

                string query = @"
                    SELECT TOP 1 
                        aa.area_name, 
                        aa.response_time_seconds, 
                        a.alert_time,
                        a.end_time,
                        a.alert_type
                    FROM FC_ALERT_AREA aa
                    INNER JOIN FC_ALERT a ON aa.alert_id = a.alert_id
                    WHERE a.is_active = 1 
                    AND a.end_time > GETDATE()
                    AND aa.area_name = @zoneName
                    ORDER BY a.alert_time DESC";

                cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@zoneName", zoneName);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    return new ActiveAlertInfo
                    {
                        IsActive = true,
                        AlertTime = Convert.ToDateTime(dr["alert_time"]),
                        EndTime = Convert.ToDateTime(dr["end_time"]),
                        AlertType = dr["alert_type"].ToString(),
                        ResponseTimeSeconds = Convert.ToInt32(dr["response_time_seconds"])
                    };
                }

                return new ActiveAlertInfo { IsActive = false };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to check active alert: " + ex.Message);
            }
            finally
            {
                if (con != null)
                    con.Close();
            }
        }

        // מביא את המיקום האחרון של המשתמש
        public UserLocation GetUserLastLocation(int userId)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;

            try
            {
                con = connect("myProjDB");

                string query = @"
                    SELECT TOP 1 user_id, latitude, longitude, created_at
                    FROM FC_USER_LAST_LOCATION
                    WHERE user_id = @userId
                    ORDER BY created_at DESC";

                cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@userId", userId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    return new UserLocation
                    {
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Latitude = Convert.ToDouble(dr["latitude"]),
                        Longitude = Convert.ToDouble(dr["longitude"]),
                        CreatedAt = Convert.ToDateTime(dr["created_at"])
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get user last location: " + ex.Message);
            }
            finally
            {
                if (con != null)
                    con.Close();
            }
        }

        // עדכון מיקום משתמש
        public void UpdateUserLocation(UserLocation location)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;

            try
            {
                con = connect("myProjDB");

                cmd = new SqlCommand("FC_SP_AddUserLastLocation", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@user_id", location.UserId);
                cmd.Parameters.AddWithValue("@latitude", location.Latitude);
                cmd.Parameters.AddWithValue("@longitude", location.Longitude);
                cmd.Parameters.AddWithValue("@created_at", location.CreatedAt);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update user location: " + ex.Message);
            }
            finally
            {
                if (con != null)
                    con.Close();
            }
        }
    }

    // מידע על התרעה פעילה
    public class ActiveAlertInfo
    {
        public bool IsActive { get; set; }
        public DateTime? AlertTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string AlertType { get; set; }
        public int ResponseTimeSeconds { get; set; }
    }
}