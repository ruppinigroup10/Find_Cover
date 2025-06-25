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
    public async Task<List<Alert>> GetActiveAlertsAsync()
    {
        return await Task.Run(() =>
        {
            List<Alert> alerts = new List<Alert>();
            using (SqlConnection con = connect())
            {
                string query = "SELECT * FROM Alerts WHERE is_active = 1";
                SqlCommand cmd = CreateCommand(query, con);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        alerts.Add(BuildAlertFromReader(reader));
                    }
                }
            }
            return alerts;
        });
    }

    //--------------------------------------------------------------------------------------------------
    // This method gets an Alert object from a SqlDataReader
    //--------------------------------------------------------------------------------------------------
    public async Task<Alert> GetAlertByIdAsync(int alertId)
    {
        return await Task.Run(() =>
        {
            using (SqlConnection con = connect())
            {
                string query = "SELECT * FROM Alerts WHERE alert_id = @AlertId";
                SqlCommand cmd = CreateCommand(query, con);
                cmd.Parameters.AddWithValue("@AlertId", alertId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return BuildAlertFromReader(reader);
                    }
                }
            }
            return null;
        });
    }

    //--------------------------------------------------------------------------------------------------
    // This method creates a SqlCommand with the specified query and connection
    //--------------------------------------------------------------------------------------------------
    public async Task<int> CreateAlertAsync(Alert alert)
    {
        return await Task.Run(() =>
        {
            using (SqlConnection con = connect())
            {
                string query = @"
                INSERT INTO Alerts (alert_type, CenterLatitude, CenterLongitude, 
                                  RadiusKm, created_at, is_active, created_by)
                VALUES (@Type, @Lat, @Lon, @Radius, @Created, 1, @CreatedBy);
                SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = CreateCommand(query, con);
                cmd.Parameters.AddWithValue("@Type", alert.alert_type);
                cmd.Parameters.AddWithValue("@Lat", alert.CenterLatitude);
                cmd.Parameters.AddWithValue("@Lon", alert.CenterLongitude);
                cmd.Parameters.AddWithValue("@Radius", alert.RadiusKm);
                cmd.Parameters.AddWithValue("@Created", alert.created_at);
                cmd.Parameters.AddWithValue("@CreatedBy", alert.created_by);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        });
    }

    //--------------------------------------------------------------------------------------------------
    // This method ends an active alert by setting its is_active flag to 0 and updating the end_time
    //--------------------------------------------------------------------------------------------------
    public async Task EndAlertAsync(int alertId)
    {
        await Task.Run(() =>
        {
            using (SqlConnection con = connect())
            {
                string query = @"
                UPDATE Alerts 
                SET is_active = 0, end_time = GETDATE()
                WHERE alert_id = @AlertId";

                SqlCommand cmd = CreateCommand(query, con);
                cmd.Parameters.AddWithValue("@AlertId", alertId);
                cmd.ExecuteNonQuery();
            }
        });
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
