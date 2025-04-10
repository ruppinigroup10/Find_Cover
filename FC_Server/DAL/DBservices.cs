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

/// <summary>
/// DBServices is a class created by me to provides some DataBase Services
/// </summary>
public class DBservices
{

    public DBservices()
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
    // This method registers a user
    //--------------------------------------------------------------------------------------------------
    public User? RegisterUser(string username, string password_hash, string email, string phone_number)
    {
        SqlConnection con;
        SqlCommand cmd;
        User? user = null;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@username", username);
        paramDic.Add("@password_hash", password_hash);
        paramDic.Add("@email", email);
        paramDic.Add("@phone_number", phone_number);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_RegisterUser", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    user = new User
                    {
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Username = dr["username"].ToString() ?? "",
                        Email = dr["email"].ToString() ?? "",
                        PhoneNumber = dr["phone_number"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(dr["is_active"]),
                        IsProvider = Convert.ToBoolean(dr["is_provider"])
                    };
                }
            }
            return user;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("Email already exists"))
            {
                throw new Exception("Email already exists");
            }
            if (ex.Message.Contains("Phone already exists"))
            {
                throw new Exception("Phone already exists");
            }
            throw new Exception("Registration failed");
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
    // This method updates a user info
    //--------------------------------------------------------------------------------------------------
    public User? UpdateUser(int user_id, string username, string password_hash, string email, string phone_number)
    {
        SqlConnection con;
        SqlCommand cmd;
        User? user = null;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@user_id", user_id);
        paramDic.Add("@username", username);
        paramDic.Add("@password_hash", password_hash);
        paramDic.Add("@email", email);
        paramDic.Add("@phone_number", phone_number);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdateUser", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    user = new User
                    {
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Username = dr["username"].ToString() ?? "",
                        Email = dr["email"].ToString() ?? "",
                        PhoneNumber = dr["phone_number"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(dr["is_active"]),
                        IsProvider = Convert.ToBoolean(dr["is_provider"])
                    };
                }
            }
            return user;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("Email already exists"))
            {
                throw new Exception("Email already exists");
            }
            if (ex.Message.Contains("Phone already exists"))
            {
                throw new Exception("Phone already exists");
            }
            throw new Exception("Registration failed");
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
    // This method logins a user
    //--------------------------------------------------------------------------------------------------
    public User? LoginUser(string email, string password_hash)
    {
        SqlConnection con;
        SqlCommand cmd;
        User? user = null;

        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@email", email);
        paramDic.Add("@password_hash", password_hash);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_LoginUser", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    //check if active
                    bool is_active = Convert.ToBoolean(dr["is_active"]);
                    if (!is_active)
                    {
                        throw new Exception("Account is not active");
                    }

                    user = new User
                    {
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Username = dr["username"].ToString() ?? "",
                        Email = dr["email"].ToString() ?? "",
                        PhoneNumber = dr["phone_number"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(dr["is_active"]),
                        IsProvider = Convert.ToBoolean(dr["is_provider"])
                    };
                }
            }
            return user;
        }
        catch (Exception ex)
        {
            throw new Exception("Login failed: " + ex.Message);
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
    // This method geting users data
    //--------------------------------------------------------------------------------------------------
    public User? getUser(int user_id)
    {

        SqlConnection con;
        SqlCommand cmd;
        User? user = null;

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
        paramDic.Add("@user_id", user_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_getUser", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    user = new User
                    {
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Username = dr["username"].ToString() ?? "",
                        Email = dr["email"].ToString() ?? "",
                        PhoneNumber = dr["phone_number"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(dr["is_active"]),
                        IsProvider = Convert.ToBoolean(dr["is_provider"])
                    };
                }
            }
            return user;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
            }
            throw new Exception("User data trensfer failed");
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

<<<<<<< Updated upstream
    //
    //public userpreferences

    public UserPreferences? GetUserPreferences(int preferencesId) { return null; }

    public UserPreferences? UpdateUserPreferences(int preferencesId, string shelterType, int myProperty) { return null; }
=======
    //--------------------------------------------------------------------------------------------------
    // This method adds a shelter
    //--------------------------------------------------------------------------------------------------
    public Shelter? AddShelter(string shelter_type, string name, float latitude, float longitude,
                            string address, int capacity, string additional_information, int provider_id)
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
                            string address, int capacity, string additional_information, int provider_id)
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
>>>>>>> Stashed changes
}
