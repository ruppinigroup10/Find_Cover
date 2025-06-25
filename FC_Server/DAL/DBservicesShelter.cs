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
        using (SqlConnection con = connect())
        {
            using (SqlTransaction transaction = con.BeginTransaction())
            {
                try
                {
                    // בדוק תפוסה נוכחית
                    string checkQuery = @"
                    SELECT s.capacity, 
                           (SELECT COUNT(*) FROM shelter_visit 
                            WHERE shelter_id = @ShelterId 
                            AND exit_time IS NULL) as current_occupancy
                    FROM Shelters s
                    WHERE s.shelter_id = @ShelterId";

                    SqlCommand checkCmd = CreateCommand(checkQuery, con, transaction);
                    checkCmd.Parameters.AddWithValue("@ShelterId", shelterId);

                    using (SqlDataReader reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int capacity = Convert.ToInt32(reader["capacity"]);
                            int currentOccupancy = Convert.ToInt32(reader["current_occupancy"]);

                            if (currentOccupancy >= capacity)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }
                    }

                    // הוסף ל-shelter_visit
                    string insertQuery = @"
                    INSERT INTO shelter_visit 
                    (user_id, shelter_id, entrance_time, alert_id, status)
                    VALUES (@UserId, @ShelterId, GETDATE(), @AlertId, 'EN_ROUTE')";

                    SqlCommand insertCmd = CreateCommand(insertQuery, con, transaction);
                    insertCmd.Parameters.AddWithValue("@UserId", userId);
                    insertCmd.Parameters.AddWithValue("@ShelterId", shelterId);
                    insertCmd.Parameters.AddWithValue("@AlertId", alertId);

                    insertCmd.ExecuteNonQuery();
                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }
    }


    //--------------------------------------------------------------------------------------------------
    // This method releases a user from a shelter
    //--------------------------------------------------------------------------------------------------
    public bool ReleaseUserFromShelter(int userId)
    {
        using (SqlConnection con = connect())
        {
            string query = @"
            UPDATE shelter_visit 
            SET exit_time = GETDATE(), status = 'COMPLETED'
            WHERE user_id = @UserId AND exit_time IS NULL";

            SqlCommand cmd = CreateCommand(query, con);
            cmd.Parameters.AddWithValue("@UserId", userId);

            return cmd.ExecuteNonQuery() > 0;
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method gets the current occupancy of a shelter
    //--------------------------------------------------------------------------------------------------
    public int GetCurrentOccupancy(int shelterId)
    {
        using (SqlConnection con = connect())
        {
            string query = @"
            SELECT COUNT(*) as occupancy
            FROM shelter_visit
            WHERE shelter_id = @ShelterId AND exit_time IS NULL";

            SqlCommand cmd = CreateCommand(query, con);
            cmd.Parameters.AddWithValue("@ShelterId", shelterId);

            object result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method gets all active shelters
    //--------------------------------------------------------------------------------------------------
    public static List<Shelter> getActiveShelters()
    {
        List<Shelter> shelters = new List<Shelter>();
        using (SqlConnection con = connect())
        {
            string query = "SELECT * FROM Shelters WHERE is_active = 1";
            SqlCommand cmd = CreateCommand(query, con);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    shelters.Add(BuildShelterFromReader(reader));
                }
            }
        }
        return shelters;
    }

    #endregion
}
