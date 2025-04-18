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

    //--------------------------------------------------------------------------------------------------
    // This method geting users preferences data
    //--------------------------------------------------------------------------------------------------
    public UserPreferences? GetUserPreferences(int user_id)
    {
        {

            SqlConnection con;
            SqlCommand cmd;
            UserPreferences? UserPreferences = null;

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

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetPreferences", con, paramDic);

            try
            {
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        UserPreferences = new UserPreferences
                        {
                            UserId = Convert.ToInt32(dr["user_id"]),
                        };
                    }
                }
                return UserPreferences;
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
    }

    //--------------------------------------------------------------------------------------------------
    // This method updates users preferences
    //--------------------------------------------------------------------------------------------------
    public UserPreferences? UpdateUserPreferences(int preference_id, int user_id, string shelter_type, bool accessibility_needed, int num_default_people, bool pets_allowed, DateTime last_update)
    {
        SqlConnection con;
        SqlCommand cmd;
        UserPreferences? userPreferences = null;
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
        paramDic.Add("@shelter_type", shelter_type);
        paramDic.Add("@num_default_people", num_default_people);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdateUserPreferences", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    userPreferences = new UserPreferences
                    {
                        PreferenceId = Convert.ToInt32(dr["preference_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        AccessibilityNeeded = Convert.ToBoolean(dr["accessibility_needed"]),
                        NumDefaultPeople = Convert.ToInt32(dr["num_default_people"]),
                        PetsAllowed = Convert.ToBoolean(dr["pets_allowed"]),
                        LastUpdate = Convert.ToDateTime(dr["last_update"])
                    };
                }
            }
            return userPreferences;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
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

    public UserPreferences? AddPreference(int preference_id, int user_id, string shelter_type, bool accessibility_needed, int num_default_people, bool pets_allowed)
    {
        SqlConnection con;
        SqlCommand cmd;
        UserPreferences? userPreferences = null;
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
        paramDic.Add("@shelter_type", shelter_type);
        paramDic.Add("@num_default_people", num_default_people);
        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AddPreference", con, paramDic);
        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    userPreferences = new UserPreferences
                    {
                        PreferenceId = Convert.ToInt32(dr["preference_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        AccessibilityNeeded = Convert.ToBoolean(dr["accessibility_needed"]),
                        NumDefaultPeople = Convert.ToInt32(dr["num_default_people"]),
                        PetsAllowed = Convert.ToBoolean(dr["pets_allowed"]),
                        LastUpdate = Convert.ToDateTime(dr["last_update"])
                    };
                }
            }
            return userPreferences;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
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
    // This method updates users known location data
    //--------------------------------------------------------------------------------------------------
    public KnownLocation? UpdateKnownLocation(int location_id, int user_id, float latitude, float longitude, float radius, string address, string location_name, string nickname, DateTime added_at)
    {
        SqlConnection con;
        SqlCommand cmd;
        KnownLocation? knownLocation = null;
        try
        {
            con = connect("myProjDB");
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message);
        }
        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@location_id", location_id);
        paramDic.Add("@user_id", user_id);
        paramDic.Add("@latitude", latitude);
        paramDic.Add("@longitude", longitude);
        paramDic.Add("@radius", radius);
        paramDic.Add("@address", address);
        paramDic.Add("@location_name", location_name);
        paramDic.Add("@nickname", nickname);
        paramDic.Add("@added_at", added_at);
        //paramDic.Add("@added_at", added_at);
        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdateKnownLocation", con, paramDic);
        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    knownLocation = new KnownLocation
                    {
                        LocationId = Convert.ToInt32(dr["location_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Radius = Convert.ToSingle(dr["radius"]),
                        Address = dr["address"].ToString() ?? "",
                        LocationName = dr["location_name"].ToString() ?? "",
                        Nickname = dr["nickname"].ToString() ?? "",
                        AddedAt = Convert.ToDateTime(dr["added_at"])
                    };
                }
            }
            return knownLocation;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
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
    // This method geting users known location data
    //--------------------------------------------------------------------------------------------------
    public KnownLocation? GetKnownLocation(int user_id)
    {
        SqlConnection con;
        SqlCommand cmd;
        KnownLocation? knownLocation = null;
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
        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetKnownLocation", con, paramDic);
        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    knownLocation = new KnownLocation
                    {
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Radius = Convert.ToSingle(dr["radius"]),
                        Address = dr["address"].ToString() ?? "",
                        LocationName = dr["location_name"].ToString() ?? "",
                        Nickname = dr["nickname"].ToString() ?? "",
                        AddedAt = Convert.ToDateTime(dr["added_at"])
                    };
                }
            }
            return knownLocation;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
            }
            throw new Exception("User data transfer failed");
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
    public KnownLocation? AddKnownLocation(int location_id, int user_id, float latitude, float longitude, float radius, string address, string location_name, string nickname)
    {
        SqlConnection con;
        SqlCommand cmd;
        KnownLocation? knownLocation = null;
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
        paramDic.Add("@latitude", latitude);
        paramDic.Add("@longitude", longitude);
        paramDic.Add("@radius", radius);
        paramDic.Add("@address", address);
        paramDic.Add("@location_name", location_name);
        paramDic.Add("@nickname", nickname);
        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AddKnownLocation", con, paramDic);
        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    knownLocation = new KnownLocation
                    {
                        LocationId = Convert.ToInt32(dr["location_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Radius = Convert.ToSingle(dr["radius"]),
                        Address = dr["address"].ToString() ?? "",
                        LocationName = dr["location_name"].ToString() ?? "",
                        Nickname = dr["nickname"].ToString() ?? "",
                    };
                }
            }
            return knownLocation;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
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

}