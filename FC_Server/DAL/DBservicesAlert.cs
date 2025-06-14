using System.Text.Json;
using System.Data.SqlClient;
using FC_Server.Models;
using System.Data;
using Microsoft.Extensions.Configuration;

public class DBservicesAlert
{
    public SqlConnection connect(String conString)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json").Build();
        string cStr = configuration.GetConnectionString("myProjDB");
        SqlConnection con = new SqlConnection(cStr);
        con.Open();
        return con;
    }

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
