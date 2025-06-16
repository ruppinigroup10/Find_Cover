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
    }

