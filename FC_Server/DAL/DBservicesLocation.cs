using System.Data.SqlClient;
using System.Data;
using FC_Server.Models;
using Microsoft.AspNetCore.Mvc;
using FC_Server.DAL;

namespace FC_Server.DAL
{
    public class DBservicesLocation
    {
        private readonly string _connectionString;

        public DBservicesLocation(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DBservicesLocation()
        {
            _connectionString = null; // Will be set in connect()
        }

        //--------------------------------------------------------------------------------------------------
        // This method inserts a new user location using a stored procedure
        //--------------------------------------------------------------------------------------------------
        public void InsertUserLocation(UserLocation location)
        {
            SqlConnection con;
            SqlCommand cmd;

            try
            {
                con = connect("myProjDB"); // שימוש בפונקציית connect בדיוק כמו בשאר המתודות
            }
            catch (Exception ex)
            {
                throw new Exception("Database connection error: " + ex.Message);
            }

            Dictionary<string, object> paramDic = new Dictionary<string, object>();
            paramDic.Add("@user_id", location.UserId);
            paramDic.Add("@latitude", location.Latitude);
            paramDic.Add("@longitude", location.Longitude);
            paramDic.Add("@created_at", location.CreatedAt);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AddUserLastLocation", con, paramDic);

            try
            {
                cmd.ExecuteNonQuery(); // אין קריאת תוצאה – רק הוספה
            }
            catch (SqlException ex)
            {
                throw new Exception("Insert location failed: " + ex.Message);
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }



        //--------------------------------------------------------------------------------------------------

        #region Emergency System Methods - New Methods to Add

        //-----------------------------------------------------------------------------------

        /// <summary>
        /// שמירת מיקום משתמש (לשימוש כ-fallback)
        /// </summary>
        public async Task SaveUserLocation(int userId, double lat, double lon)
        {
            using (SqlConnection con = connect("myProjDB"))
            {

                string insertQuery = @"
            INSERT INTO FC_USER_LAST_LOCATION 
            (created_at, user_id, latitude, longitude)
            VALUES (GETDATE(), @UserId, @Lat, @Lon)";

                SqlCommand cmd = new SqlCommand(insertQuery, con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Lat", lat);
                cmd.Parameters.AddWithValue("@Lon", lon);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// קבלת מיקום אחרון ידוע (רק אם אין מיקום real-time)
        /// </summary>
        public async Task<(double? lat, double? lon, DateTime? time)> GetLastKnownLocation(int userId)
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                string query = @"
            SELECT TOP 1 latitude, longitude, created_at
            FROM FC_USER_LAST_LOCATION 
            WHERE user_id = @UserId
            ORDER BY created_at DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        return (
                            lat: reader["latitude"] as double?,
                            lon: reader["longitude"] as double?,
                            time: reader["created_at"] as DateTime?
                        );
                    }
                }

                return (null, null, null);
            }
        }

        #endregion

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

}