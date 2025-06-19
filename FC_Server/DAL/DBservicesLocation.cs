using System.Data.SqlClient;
using System.Data;
using FC_Server.Models;
using Microsoft.AspNetCore.Mvc;


    public class LocationDbService
    {
        private readonly string _connectionString;

        public LocationDbService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InsertUserLocation(UserLocation location)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("FC_SP_AddUserLastLocation", conn)) 
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@user_id", location.UserId);
                    cmd.Parameters.AddWithValue("@latitude", location.Latitude);
                    cmd.Parameters.AddWithValue("@longitude", location.Longitude);
                    cmd.Parameters.AddWithValue("@created_at", location.CreatedAt); // ← חובה בטבלה

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

    //--------------------------------------------------------------------------------------------------
    // This method creates a connection to the database according to the connectionString name in the web.config 
    //--------------------------------------------------------------------------------------------------
    public SqlConnection connect(String conString)
    {
        // read the connection string from the configuration file
        IConfigurationRoot configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json").Build();
        string cStr = configuration.GetConnectionString("myProjDB");
        SqlConnection con = new SqlConnection(cStr);
        con.Open();
        return con;
    }

    //--------------------------------------------------------------------------------------------------
    // Create the SqlCommand
    //--------------------------------------------------------------------------------------------------
    private SqlCommand CreateCommandWithStoredProcedureGeneral(String spName, SqlConnection con, Dictionary<string, object> paramDic)
    {
        SqlCommand cmd = new SqlCommand(); // create the command object
        cmd.Connection = con;              // assign the connection to the command object
        cmd.CommandText = spName;      // can be Select, Insert, Update, Delete 
        cmd.CommandTimeout = 10;           // Time to wait for the execution' The default is 30 seconds
        cmd.CommandType = System.Data.CommandType.StoredProcedure; // the type of the command, can also be text
        if (paramDic != null)
            foreach (KeyValuePair<string, object> param in paramDic)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);

            }
        return cmd;
    }

    public void CleanupUserLocations()
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

       
        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_CleanupUserLastLocations", con, null);

        try
        {
            cmd.ExecuteNonQuery(); // לא מחזיר תוצאה – רק מבצע
        }
        catch (Exception ex)
        {
            throw new Exception("Cleanup failed: " + ex.Message);
        }
        finally
        {
            if (con.State == ConnectionState.Open)
                con.Close();
        }
    }
}



