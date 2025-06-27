using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using FC_Server.Models;
using System.Net.NetworkInformation;
using System.Net;
public class DBservicesShelter
{
    public DBservicesShelter()
    {
        //
        // TODO: Add constructor logic here
        //
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

    //--------------------------------------------------------------------------------------------------
    // This method adds a shelter
    //--------------------------------------------------------------------------------------------------
    public Shelter? AddShelter(string shelter_type, string name, float latitude, float longitude,
                                        string address, int capacity,
                                        bool is_accessible, bool pets_friendly,
                                        string additional_information, int provider_id)
    {
        SqlConnection con;
        SqlCommand cmd;
        Shelter? shelter = null;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@shelter_type", shelter_type);
        paramDic.Add("@name", name);
        paramDic.Add("@latitude", latitude);
        paramDic.Add("@longitude", longitude);
        paramDic.Add("@address", address);
        paramDic.Add("@capacity", capacity);
        paramDic.Add("is_accessible", is_accessible);
        paramDic.Add("pets_friendly", pets_friendly);
        paramDic.Add("@additional_information", additional_information);
        paramDic.Add("@provider_id", provider_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AddShelter", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    shelter = new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        Name = dr["name"].ToString() ?? "",
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Address = dr["address"].ToString() ?? "",
                        Capacity = Convert.ToInt32(dr["capacity"]),
                        AdditionalInformation = dr["additional_information"].ToString() ?? "",
                        ProviderId = Convert.ToInt32(dr["provider_id"]),
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        PetsFriendly = Convert.ToBoolean(dr["pets_friendly"]),
                        IsActive = Convert.ToBoolean(dr["is_active"])
                    };
                }
            }
            return shelter;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("User added this shelter already"))
            {
                throw new Exception("User added this shelter already");
            }
            if (ex.Message.Contains("User not exists"))
            {
                throw new Exception("User not exists");
            }
            throw new Exception("Addition failed");
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
    // This method updates a shelter
    //--------------------------------------------------------------------------------------------------
    public Shelter? UpdateShelter(int shelter_id, string shelter_type, string name, float latitude, float longitude,
                            string address, int capacity, string additional_information, int provider_id, bool pets_friendly, bool is_accessible, bool is_active)
    {
        SqlConnection con;
        SqlCommand cmd;
        Shelter? shelter = null;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@shelter_id", shelter_id);
        paramDic.Add("@shelter_type", shelter_type);
        paramDic.Add("@name", name);
        paramDic.Add("@latitude", latitude);
        paramDic.Add("@longitude", longitude);
        paramDic.Add("@address", address);
        paramDic.Add("@capacity", capacity);
        paramDic.Add("@additional_information", additional_information);
        paramDic.Add("@pets_friendly", pets_friendly);
        paramDic.Add("@is_accessible", is_accessible);
        paramDic.Add("@is_active", is_active);
        paramDic.Add("@provider_id", provider_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdateShelter", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    shelter = new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        Name = dr["name"].ToString() ?? "",
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Address = dr["address"].ToString() ?? "",
                        Capacity = Convert.ToInt32(dr["capacity"]),
                        AdditionalInformation = dr["additional_information"].ToString() ?? "",
                        PetsFriendly = Convert.ToBoolean(dr["pets_friendly"]),
                        ProviderId = Convert.ToInt32(dr["provider_id"]),
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        IsActive = Convert.ToBoolean(dr["is_active"])
                    };
                }
            }
            return shelter;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("User added this shelter already"))
            {
                throw new Exception("User added this shelter already");
            }
            throw new Exception("Update failed");
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
    // This method geting shelters data
    //--------------------------------------------------------------------------------------------------
    public Shelter? getShelter(int shelter_id)
    {

        SqlConnection con;
        SqlCommand cmd;
        Shelter? shelter = null;

        try
        {
            con = connect("myProjDB"); // create the connection
        }
        catch (Exception)
        {
            // write to log
            throw;
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@shelter_id", shelter_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_getShelter", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    shelter = new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        Name = dr["name"].ToString() ?? "",
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Address = dr["address"].ToString() ?? "",
                        Capacity = Convert.ToInt32(dr["capacity"]),
                        AdditionalInformation = dr["additional_information"].ToString() ?? "",
                        ProviderId = dr["provider_id"] != DBNull.Value ? Convert.ToInt32(dr["provider_id"]) : 0,
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        PetsFriendly = Convert.ToBoolean(dr["pets_friendly"]),
                        IsActive = Convert.ToBoolean(dr["is_active"])
                    };
                }
            }
            return shelter;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
            }
            throw new Exception("Shelter data transfer failed");
        }
        finally
        {
            if (con != null)
            {
                // close the db connection
                con.Close();
            }
        }
    }

    public List<Shelter> getMyShelters(int provider_id)
    {
        SqlConnection con;
        SqlCommand cmd;
        List<Shelter> shelters = new List<Shelter>();

        try
        {
            con = connect("myProjDB"); // create the connection
        }
        catch (Exception)
        {
            throw;
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@provider_id", provider_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_getMyShelter", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Shelter shelter = new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        Name = dr["name"].ToString() ?? "",
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Address = dr["address"].ToString() ?? "",
                        Capacity = Convert.ToInt32(dr["capacity"]),
                        AdditionalInformation = dr["additional_information"].ToString() ?? "",
                        ProviderId = Convert.ToInt32(dr["provider_id"]),
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        IsActive = Convert.ToBoolean(dr["is_active"])
                    };

                    shelters.Add(shelter);
                }
            }

            return shelters;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
            }
            throw new Exception("Shelter data transfer failed");
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method geting shelters data
    //--------------------------------------------------------------------------------------------------
    public Shelter? shelterActiveStatus(int shelter_id, bool status)
    {

        SqlConnection con;
        SqlCommand cmd;
        Shelter? shelter = null;

        try
        {
            con = connect("myProjDB"); // create the connection
        }
        catch (Exception)
        {
            // write to log
            throw;
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@shelter_id", shelter_id);
        paramDic.Add("@status", status);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_shelterActiveStatus", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    shelter = new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        Name = dr["name"].ToString() ?? "",
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Address = dr["address"].ToString() ?? "",
                        Capacity = Convert.ToInt32(dr["capacity"]),
                        AdditionalInformation = dr["additional_information"].ToString() ?? "",
                        ProviderId = Convert.ToInt32(dr["provider_id"]),
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        IsActive = Convert.ToBoolean(dr["is_active"])
                    };
                }
            }
            return shelter;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
            }
            throw new Exception("Shelter data transfer failed");
        }
        finally
        {
            if (con != null)
            {
                // close the db connection
                con.Close();
            }
        }
    }


    public void DeleteShelter(int shelter_id, int provider_id)
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

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@shelter_id", shelter_id);
        paramDic.Add("@provider_id", provider_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_DeleteShelter", con, paramDic);

        try
        {
            cmd.ExecuteNonQuery();  // כי לא מצפים לתוצאה
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("Shelter not found"))
            {
                throw new Exception("Shelter not found");
            }
            if (ex.Message.Contains("User not found"))
            {
                throw new Exception("User not found");
            }
            if (ex.Message.Contains("Shelter not owned by this user"))
            {
                throw new Exception("Shelter not owned by this user");
            }

            throw new Exception("Deletion failed");
        }
        finally
        {
            if (con != null && con.State == ConnectionState.Open)
                con.Close();
        }
    }

    #region Emergency Allocation Methods
    //--------------------------------------------------------------------------------------------------
    // This method allocates a user to a shelter
    //--------------------------------------------------------------------------------------------------
    public bool AllocateUserToShelter(int userId, int shelterId, int alertId)
    {
        try
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                SqlCommand cmd = new SqlCommand("FC_SP_AllocateUserToShelter", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@shelterId", shelterId);
                cmd.Parameters.AddWithValue("@alertId", alertId);

                // Output parameter for result
                SqlParameter resultParam = new SqlParameter("@result", SqlDbType.Bit);
                resultParam.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(resultParam);

                cmd.ExecuteNonQuery();
                bool result = Convert.ToBoolean(resultParam.Value);
                con.Close();

                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error allocating user {userId} to shelter {shelterId}: {ex.Message}");
            return false;
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method gets the active allocation for a user
    //--------------------------------------------------------------------------------------------------

    public UserVisit GetActiveUserAllocation(int userId)
    {
        try
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                SqlCommand cmd = new SqlCommand("FC_SP_GetActiveUserAllocation", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userId", userId);

                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                if (dr.Read())
                {
                    var visit = new UserVisit
                    {
                        visit_id = Convert.ToInt32(dr["visit_id"]),
                        user_id = Convert.ToInt32(dr["user_id"]),
                        shelter_id = Convert.ToInt32(dr["shelter_id"]),
                        alert_id = dr["alert_id"] != DBNull.Value ? Convert.ToInt32(dr["alert_id"]) : 0,
                        arrival_time = dr["arrival_time"] != DBNull.Value ?
                            Convert.ToDateTime(dr["arrival_time"]) : (DateTime?)null,
                        departure_time = dr["departure_time"] != DBNull.Value ?
                            Convert.ToDateTime(dr["departure_time"]) : (DateTime?)null,
                        status = dr["status"] != DBNull.Value ? dr["status"].ToString() : "EN_ROUTE",
                        distance_to_shelter = dr["distance_to_shelter"] != DBNull.Value ?
                            Convert.ToDouble(dr["distance_to_shelter"]) : (double?)null,
                        confirmed_arrival = dr["confirmed_arrival"] != DBNull.Value ?
                            Convert.ToBoolean(dr["confirmed_arrival"]) : false,
                        walking_distance = dr["walking_distance"] != DBNull.Value ?
                            Convert.ToDouble(dr["walking_distance"]) : (double?)null,
                        route_polyline = dr["route_polyline"] != DBNull.Value ?
                            dr["route_polyline"].ToString() : null
                    };
                    con.Close();
                    return visit;
                }
                con.Close();
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting active allocation for user {userId}: {ex.Message}");
            throw;
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method updates the status of a user's visit to a shelter
    //--------------------------------------------------------------------------------------------------
    public void UpdateVisitStatus(int userId, int shelterId, string status)
    {
        try
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                SqlCommand cmd = new SqlCommand("FC_SP_UpdateVisitStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@shelterId", shelterId);
                cmd.Parameters.AddWithValue("@status", status);

                cmd.ExecuteNonQuery();
                con.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating visit status: {ex.Message}");
            throw;
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method releases a user from a shelter
    //--------------------------------------------------------------------------------------------------
    public void ReleaseUserFromShelter(int userId)
    {
        try
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                SqlCommand cmd = new SqlCommand("FC_SP_ReleaseUserFromShelter", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userId", userId);

                cmd.ExecuteNonQuery();
                con.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error releasing user {userId}: {ex.Message}");
            throw;
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method gets the current occupancy of a shelter
    //--------------------------------------------------------------------------------------------------
    public int GetCurrentOccupancy(int shelterId)
    {
        try
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                SqlCommand cmd = new SqlCommand("FC_SP_GetShelterOccupancy", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@shelterId", shelterId);

                SqlDataReader dr = cmd.ExecuteReader();
                int count = 0;
                if (dr.Read())
                {
                    count = Convert.ToInt32(dr["occupancy_count"]);
                }
                con.Close();
                return count;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting occupancy for shelter {shelterId}: {ex.Message}");
            return 0;
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method gets all active shelters
    //--------------------------------------------------------------------------------------------------

    public List<Shelter> GetActiveShelters()
    {
        List<Shelter> shelters = new List<Shelter>();

        try
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                SqlCommand cmd = new SqlCommand("FC_SP_GetActiveShelters", con);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (dr.Read())
                {
                    shelters.Add(new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ProviderId = dr["provider_id"] != DBNull.Value ? Convert.ToInt32(dr["provider_id"]) : 0,
                        ShelterType = dr["shelter_type"] != DBNull.Value ? dr["shelter_type"].ToString() : "",
                        Name = dr["name"] != DBNull.Value ? dr["name"].ToString() : "",
                        Latitude = dr["latitude"] != DBNull.Value ? (float)Convert.ToDouble(dr["latitude"]) : 0f,
                        Longitude = dr["longitude"] != DBNull.Value ? (float)Convert.ToDouble(dr["longitude"]) : 0f,
                        Address = dr["address"] != DBNull.Value ? dr["address"].ToString() : "",
                        Capacity = dr["capacity"] != DBNull.Value ? Convert.ToInt32(dr["capacity"]) : 0,
                        IsAccessible = dr["is_accessible"] != DBNull.Value ? Convert.ToBoolean(dr["is_accessible"]) : false,
                        PetsFriendly = dr["pets_friendly"] != DBNull.Value ? Convert.ToBoolean(dr["pets_friendly"]) : false,
                        IsActive = dr["is_active"] != DBNull.Value ? Convert.ToBoolean(dr["is_active"]) : true,
                        AdditionalInformation = dr["additional_information"] != DBNull.Value ? dr["additional_information"].ToString() : "",
                        CreatedAt = dr["created_at"] != DBNull.Value ? Convert.ToDateTime(dr["created_at"]) : DateTime.Now,
                        LastUpdated = dr["last_updated"] != DBNull.Value ? Convert.ToDateTime(dr["last_updated"]) : DateTime.Now
                    });
                }
                con.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting active shelters: {ex.Message}");
            throw;
        }

        return shelters;
    }

    //--------------------------------------------------------------------------------------------------
    // Get shelters within radius using stored procedure
    //--------------------------------------------------------------------------------------------------

    public List<Shelter> GetSheltersInRadius(double latitude, double longitude, double radiusKm)
    {
        List<Shelter> shelters = new List<Shelter>();

        try
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                SqlCommand cmd = new SqlCommand("FC_SP_GetSheltersInRadius", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@latitude", latitude);
                cmd.Parameters.AddWithValue("@longitude", longitude);
                cmd.Parameters.AddWithValue("@radiusKm", radiusKm);

                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (dr.Read())
                {
                    var shelter = new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ProviderId = Convert.ToInt32(dr["provider_id"]),
                        ShelterType = dr["shelter_type"].ToString(),
                        Name = dr["name"].ToString(),
                        Latitude = (float)Convert.ToDouble(dr["latitude"]),
                        Longitude = (float)Convert.ToDouble(dr["longitude"]),
                        Address = dr["address"].ToString(),
                        Capacity = Convert.ToInt32(dr["capacity"]),
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        PetsFriendly = Convert.ToBoolean(dr["pets_friendly"]),
                        IsActive = Convert.ToBoolean(dr["is_active"]),
                        AdditionalInformation = dr["additional_information"]?.ToString(),
                        CreatedAt = Convert.ToDateTime(dr["created_at"]),
                        LastUpdated = Convert.ToDateTime(dr["last_updated"])
                    };

                    // The stored procedure also returns distance
                    // You might want to add a Distance property to your Shelter model
                    // or create a ShelterWithDistance class

                    shelters.Add(shelter);
                }
                con.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting shelters in radius: {ex.Message}");
            throw;
        }

        return shelters;
    }

    //--------------------------------------------------------------------------------------------------
    // This method updates the allocation status for a user
    //--------------------------------------------------------------------------------------------------
    public void UpdateAllocationStatus(int userId, string status)
    {
        try
        {
            using (SqlConnection con = connect("myProjDB"))
            {
                SqlCommand cmd = new SqlCommand("FC_SP_UpdateAllocationStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@status", status);

                cmd.ExecuteNonQuery();
                con.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating allocation status: {ex.Message}");
            throw;
        }
    }

    //--------------------------------------------------------------------------------------------------
    // Get all active shelters from the database
    //--------------------------------------------------------------------------------------------------

    public List<Shelter> GetAllActiveShelters()
    {
        SqlConnection con;
        SqlCommand cmd;
        List<Shelter> sheltersList = new List<Shelter>();

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to connect to DB: " + ex.Message);
        }

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_getAllShelters", con, null);

        try
        {
            SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

            while (dr.Read())
            {
                Shelter shelter = new Shelter
                {
                    ShelterId = Convert.ToInt32(dr["shelter_id"]),
                    ProviderId = dr["provider_id"] != DBNull.Value ? Convert.ToInt32(dr["provider_id"]) : 0,
                    ShelterType = dr["shelter_type"] != DBNull.Value ? dr["shelter_type"].ToString() : "",
                    Name = dr["name"] != DBNull.Value ? dr["name"].ToString() : "",
                    Latitude = dr["latitude"] != DBNull.Value ? Convert.ToSingle(dr["latitude"]) : 0f,
                    Longitude = dr["longitude"] != DBNull.Value ? Convert.ToSingle(dr["longitude"]) : 0f,
                    Address = dr["address"] != DBNull.Value ? dr["address"].ToString() : "",
                    Capacity = dr["capacity"] != DBNull.Value ? Convert.ToInt32(dr["capacity"]) : 0,
                    IsAccessible = dr["is_accessible"] != DBNull.Value ? Convert.ToBoolean(dr["is_accessible"]) : false,
                    PetsFriendly = dr["pets_friendly"] != DBNull.Value ? Convert.ToBoolean(dr["pets_friendly"]) : false,
                    IsActive = dr["is_active"] != DBNull.Value ? Convert.ToBoolean(dr["is_active"]) : true,
                    CreatedAt = dr["created_at"] != DBNull.Value ? Convert.ToDateTime(dr["created_at"]) : DateTime.Now,
                    LastUpdated = dr["last_updated"] != DBNull.Value ? Convert.ToDateTime(dr["last_updated"]) : DateTime.Now,
                    AdditionalInformation = dr["additional_information"] != DBNull.Value ? dr["additional_information"].ToString() : ""
                };


                sheltersList.Add(shelter);
            }

            return sheltersList;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to get active shelters: " + ex.Message);
        }
        finally
        {
            if (con != null && con.State == ConnectionState.Open)
            {
                con.Close();
            }
        }
    }

    #endregion
}
