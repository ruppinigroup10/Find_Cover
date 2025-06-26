using System.Text.Json;
using System.Data.SqlClient;
using FC_Server.Models;
using System.Data;
using Microsoft.Extensions.Configuration;

public class DBservicesAlert
{
    //--------------------------------------------------------------------------------------------------
    // This method creates a connection to the database according to the connectionString name in the web.config 
    //--------------------------------------------------------------------------------------------------
    public SqlConnection connect(String conString)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json").Build();
        string cStr = configuration.GetConnectionString("myProjDB");
        SqlConnection con = new SqlConnection(cStr);
        con.Open();
        return con;
    }


    #region Emergency System Methods
    //--------------------------------------------------------------------------------------------------
    // This method retrieves all active alerts from the database
    //--------------------------------------------------------------------------------------------------
    public List<AlertRecord> GetActiveAlerts()
    {
        SqlConnection con;
        SqlCommand cmd;
        List<AlertRecord> alerts = new List<AlertRecord>();

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        try
        {
            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetActiveAlerts", con, null);

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    AlertRecord alert = new AlertRecord
                    {
                        alert_id = Convert.ToInt32(dr["alert_id"]),
                        alert_type = dr["alert_type"].ToString() ?? "",
                        CenterLatitude = dr["center_latitude"] != DBNull.Value ? Convert.ToDouble(dr["center_latitude"]) : 0,
                        CenterLongitude = dr["center_longitude"] != DBNull.Value ? Convert.ToDouble(dr["center_longitude"]) : 0,
                        RadiusKm = dr["radius_km"] != DBNull.Value ? Convert.ToDouble(dr["radius_km"]) : 0,
                        created_at = Convert.ToDateTime(dr["created_at"]),
                        end_time = dr["end_time"] != DBNull.Value ? Convert.ToDateTime(dr["end_time"]) : null,
                        is_active = Convert.ToBoolean(dr["is_active"]),
                        created_by = dr["created_by"]?.ToString() ?? "System",
                        alert_source = dr["alert_source"]?.ToString() ?? "Database"
                    };
                    alerts.Add(alert);
                }
            }
            return alerts;
        }
        catch (Exception ex)
        {
            throw new Exception("Get active alerts failed: " + ex.Message);
        }
        finally
        {
            if (con != null && con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------
    // Async wrapper for GetActiveAlerts
    //--------------------------------------------------------------------------------------------------

    public async Task<List<AlertRecord>> GetActiveAlertsAsync()
    {
        return await Task.Run(() => GetActiveAlerts());
    }

    //--------------------------------------------------------------------------------------------------
    // This method gets an Alert object by ID
    //--------------------------------------------------------------------------------------------------
    public AlertRecord? GetAlertById(int alertId)
    {
        SqlConnection con;
        SqlCommand cmd;
        AlertRecord? alert = null;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        try
        {
            Dictionary<string, object> paramDic = new Dictionary<string, object>();
            paramDic.Add("@alertId", alertId);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetAlertById", con, paramDic);

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    alert = new AlertRecord
                    {
                        alert_id = Convert.ToInt32(dr["alert_id"]),
                        alert_type = dr["alert_type"].ToString() ?? "",
                        CenterLatitude = dr["center_latitude"] != DBNull.Value ? Convert.ToDouble(dr["center_latitude"]) : 0,
                        CenterLongitude = dr["center_longitude"] != DBNull.Value ? Convert.ToDouble(dr["center_longitude"]) : 0,
                        RadiusKm = dr["radius_km"] != DBNull.Value ? Convert.ToDouble(dr["radius_km"]) : 0,
                        created_at = Convert.ToDateTime(dr["created_at"]),
                        end_time = dr["end_time"] != DBNull.Value ? Convert.ToDateTime(dr["end_time"]) : null,
                        is_active = Convert.ToBoolean(dr["is_active"]),
                        created_by = dr["created_by"]?.ToString() ?? "System",
                        alert_source = dr["alert_source"]?.ToString() ?? "Database"
                    };
                }
            }
            return alert;
        }
        catch (Exception ex)
        {
            throw new Exception("Get alert by ID failed: " + ex.Message);
        }
        finally
        {
            if (con != null && con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method creates a new alert
    //--------------------------------------------------------------------------------------------------
    public int CreateAlertAsync(AlertRecord alert)
    {
        SqlConnection con;
        SqlCommand cmd;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        try
        {
            Dictionary<string, object> paramDic = new Dictionary<string, object>();
            paramDic.Add("@alert_type", alert.alert_type);
            paramDic.Add("@center_latitude", alert.CenterLatitude);
            paramDic.Add("@center_longitude", alert.CenterLongitude);
            paramDic.Add("@radius_km", alert.RadiusKm);
            paramDic.Add("@created_by", alert.created_by);
            paramDic.Add("@alert_source", alert.alert_source);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_CreateAlertWithLocation", con, paramDic);

            object result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Create alert failed: " + ex.Message);
        }
        finally
        {
            if (con != null && con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }
        }
    }



    //--------------------------------------------------------------------------------------------------
    // This method ends an active alert
    //--------------------------------------------------------------------------------------------------
    public void EndAlert(int alertId)
    {
        SqlConnection con;
        SqlCommand cmd;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        try
        {
            Dictionary<string, object> paramDic = new Dictionary<string, object>();
            paramDic.Add("@alertId", alertId);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_EndAlert", con, paramDic);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("End alert failed: " + ex.Message);
        }
        finally
        {
            if (con != null && con.State == System.Data.ConnectionState.Open)
            {
                con.Close();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------
    // Async wrapper for EndAlert 
    //--------------------------------------------------------------------------------------------------
    public async Task EndAlertAsync(int alertId)
    {
        await Task.Run(() => EndAlert(alertId));
    }

    #endregion

    //--------------------------------------------------------------------------------------------------
    // Create the SqlCommand
    //--------------------------------------------------------------------------------------------------
    private SqlCommand CreateCommandWithStoredProcedureGeneral(String spName, SqlConnection con, Dictionary<string, object> paramDic)
    {
        SqlCommand cmd = new SqlCommand();
        cmd.Connection = con;
        cmd.CommandText = spName;
        cmd.CommandTimeout = 10;
        cmd.CommandType = CommandType.StoredProcedure;
        if (paramDic != null)
            foreach (KeyValuePair<string, object> param in paramDic)
                cmd.Parameters.AddWithValue(param.Key, param.Value);
        return cmd;
    }

    public async Task<List<Alert>> GetAlertsFromApi()
    {
        using (HttpClient client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(120);

            var response = await client.GetAsync("https://api.tzevaadom.co.il/notifications");
            if (!response.IsSuccessStatusCode)
                return new List<Alert>();

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Alert>>(json) ?? new List<Alert>();
        }
    }

    public void SaveAlertsToDb(List<Alert> alerts)
    {
        SqlConnection con;
        SqlCommand cmd;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        foreach (var alert in alerts)
        {
            DateTime alertTime = DateTimeOffset.FromUnixTimeSeconds(alert.time).ToLocalTime().DateTime;
            string data = string.Join(",", alert.cities);
            string alertType = alert.threat switch
            {
                0 => "Rocket",
                1 => "Infiltration",
                _ => "Unknown"
            };

            Console.WriteLine($"Saving alert: time={alertTime}, data={data}");

            Dictionary<string, object> paramDic = new Dictionary<string, object>();
            paramDic.Add("@alert_time", alertTime);
            paramDic.Add("@data", data);
            paramDic.Add("@is_active", !alert.isDrill); // אם זה תרגיל – לא אקטיבי
            paramDic.Add("@alert_type", alertType);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AddAlert", con, paramDic);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[DB ERROR] Failed to save alert. Reason: {ex.Message}");
                continue;
            }
        }

        if (con != null && con.State == ConnectionState.Open)
            con.Close();
    }
}
