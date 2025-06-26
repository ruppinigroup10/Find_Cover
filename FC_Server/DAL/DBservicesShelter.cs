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
            // First check current occupancy
            Dictionary<string, object> checkParams = new Dictionary<string, object>();
            checkParams.Add("@shelterId", shelterId);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetShelterOccupancy", con, checkParams);

            int capacity = 0;
            int currentOccupancy = 0;

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    capacity = Convert.ToInt32(dr["capacity"]);
                    currentOccupancy = Convert.ToInt32(dr["current_occupancy"]);
                }
            }

            // Check if shelter is full
            if (currentOccupancy >= capacity)
            {
                return false;
            }

            // Allocate user to shelter
            Dictionary<string, object> allocateParams = new Dictionary<string, object>();
            allocateParams.Add("@userId", userId);
            allocateParams.Add("@shelterId", shelterId);
            allocateParams.Add("@alertId", alertId);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AllocateUserToShelter", con, allocateParams);
            cmd.ExecuteNonQuery();

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Allocation failed: " + ex.Message);
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
    // This method gets the active allocation for a user
    //--------------------------------------------------------------------------------------------------
    public ShelterAllocation? GetActiveUserAllocation(int userId)
    {
        SqlConnection con;
        SqlCommand cmd;
        ShelterAllocation? allocation = null;

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
            paramDic.Add("@userId", userId);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetActiveUserAllocation", con, paramDic);

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    allocation = new ShelterAllocation
                    {
                        allocation_id = Convert.ToInt32(dr["allocation_id"]),
                        user_id = Convert.ToInt32(dr["user_id"]),
                        shelter_id = Convert.ToInt32(dr["shelter_id"]),
                        alert_id = Convert.ToInt32(dr["alert_id"]),
                        allocation_time = Convert.ToDateTime(dr["allocation_time"]),
                        arrival_time = dr["arrival_time"] != DBNull.Value ? Convert.ToDateTime(dr["arrival_time"]) : null,
                        exit_time = dr["exit_time"] != DBNull.Value ? Convert.ToDateTime(dr["exit_time"]) : null,
                        status = dr["status"].ToString() ?? "EN_ROUTE",
                        is_active = Convert.ToBoolean(dr["is_active"]),
                        walking_distance = dr["walking_distance"] != DBNull.Value ? Convert.ToDouble(dr["walking_distance"]) : null,
                        actual_walking_time = dr["actual_walking_time"] != DBNull.Value ? Convert.ToInt32(dr["actual_walking_time"]) : null
                    };
                }
            }

            return allocation;
        }
        catch (Exception ex)
        {
            throw new Exception("Get active allocation failed: " + ex.Message);
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
    // This method updates the status of a user's visit to a shelter
    //--------------------------------------------------------------------------------------------------
    public void UpdateVisitStatus(int userId, int shelterId, string status)
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
            paramDic.Add("@userId", userId);
            paramDic.Add("@shelterId", shelterId);
            paramDic.Add("@status", status);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdateVisitStatus", con, paramDic);

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Update visit status failed: " + ex.Message);
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
    // This method releases a user from a shelter
    //--------------------------------------------------------------------------------------------------
    public bool ReleaseUserFromShelter(int userId)
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
            paramDic.Add("@userId", userId);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_ReleaseUserFromShelter", con, paramDic);

            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Release failed: " + ex.Message);
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
    // This method gets the current occupancy of a shelter
    //--------------------------------------------------------------------------------------------------
    public int GetCurrentOccupancy(int shelterId)
    {
        SqlConnection con;
        SqlCommand cmd;
        int occupancy = 0;

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
            paramDic.Add("@shelterId", shelterId);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetCurrentOccupancy", con, paramDic);

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    occupancy = Convert.ToInt32(dr["occupancy"]);
                }
            }

            return occupancy;
        }
        catch (Exception ex)
        {
            throw new Exception("Get occupancy failed: " + ex.Message);
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
    // This method gets all active shelters
    //--------------------------------------------------------------------------------------------------
    public List<Shelter> GetActiveShelters()
    {
        SqlConnection con;
        SqlCommand cmd;
        List<Shelter> shelters = new List<Shelter>();

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
            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetActiveShelters", con, null);

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
                        ProviderId = dr["provider_id"] != DBNull.Value ? Convert.ToInt32(dr["provider_id"]) : 0,
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        PetsFriendly = Convert.ToBoolean(dr["pets_friendly"]),
                        IsActive = Convert.ToBoolean(dr["is_active"]),
                        CreatedAt = Convert.ToDateTime(dr["created_at"]),
                        LastUpdated = Convert.ToDateTime(dr["last_updated"])
                    };

                    shelters.Add(shelter);
                }
            }

            return shelters;
        }
        catch (Exception ex)
        {
            throw new Exception("Get active shelters failed: " + ex.Message);
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
    // This method updates the allocation status for a user
    //--------------------------------------------------------------------------------------------------
    public void UpdateAllocationStatus(int userId, string status)
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
            paramDic.Add("@userId", userId);
            paramDic.Add("@status", status);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdateAllocationStatus", con, paramDic);

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Update allocation status failed: " + ex.Message);
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
                    ProviderId = Convert.ToInt32(dr["provider_id"]),
                    ShelterType = dr["shelter_type"].ToString(),
                    Name = dr["name"].ToString(),
                    Latitude = Convert.ToSingle(dr["latitude"]),
                    Longitude = Convert.ToSingle(dr["longitude"]),
                    Address = dr["address"].ToString(),
                    Capacity = Convert.ToInt32(dr["capacity"]),
                    IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                    PetsFriendly = dr["pets_friendly"] != DBNull.Value ? Convert.ToBoolean(dr["pets_friendly"]) : false,
                    IsActive = Convert.ToBoolean(dr["is_active"]),
                    CreatedAt = Convert.ToDateTime(dr["created_at"]),
                    LastUpdated = Convert.ToDateTime(dr["last_updated"]),
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
