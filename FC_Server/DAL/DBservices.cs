﻿using System.Data.SqlClient;
using System.Data;
using System.Text;
using FC_Server.Models;

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
    /*123*/
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
    public User? RegisterUser(string username, string password_hash, string email, string phone_number, DateTime birthday)
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
        paramDic.Add("@birthday", birthday);

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
                        IsProvider = Convert.ToBoolean(dr["is_provider"]),
                        Birthday = Convert.ToDateTime(dr["birthday"])
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
        SqlConnection con;
        SqlCommand cmd;
        UserPreferences? UserPreferences = null;

        try
        {
            con = connect("myProjDB"); // יוצר ומחזיר חיבור למסד הנתונים
        }
        catch (Exception)
        {
            // write to log
            throw; // במקרה של שגיאה בחיבור, השגיאה נזרקת הלאה - מומלץ לטפל ולתעד את השגיאה בצורה מפורטת יותר
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@user_id", user_id); // הוספת פרמטר user_id למילון הפרמטרים עבור הפרוצדורה המאוחסנת

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetPreferences", con, paramDic); // יצירת פקודה עבור הפרוצדורה המאוחסנת "FC_SP_GetPreferences" עם החיבור והפרמטרים

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader()) // ביצוע הפקודה וקבלת קורא נתונים - שימוש ב-using מבטיח סגירה אוטומטית של ה-DataReader
            {
                if (dr.Read()) // קריאת שורה אחת מתוצאות השאילתה - מצופה שתחזור לכל היותר שורה אחת עבור משתמש יחיד
                {
                    UserPreferences = new UserPreferences // יצירת אובייקט UserPreferences עם הנתונים שחזרו ממסד הנתונים
                    {
                        PreferenceId = Convert.ToInt32(dr["preference_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        ShelterType = dr["preferred_shelter_type"].ToString() ?? "",
                        AccessibilityNeeded = Convert.ToBoolean(dr["accessible_needed"]),
                        NumDefaultPeople = Convert.ToInt32(dr["num_default_people"]),
                        PetsAllowed = Convert.ToBoolean(dr["pets_allowed"]),
                        LastUpdate = Convert.ToDateTime(dr["last_updated"])
                    };
                }
            }
            return UserPreferences; // החזרת אובייקט UserPreferences 
        }
        catch (Exception ex) // טיפול בשגיאות כלליות שעלולות להתרחש בזמן ביצוע הפקודה
        {
            if (ex.Message.Contains("No preferences found for this user")) 
            {
                throw new Exception("No preferences found for this user");
            }
            if (ex.Message.Contains("Invalid ID")) 
            {
                throw new Exception("Invalid ID");
            }
            throw new Exception("User data trensfer failed"); // שגיאה כללית במקרה של כשל בהעברת נתוני המשתמש - כדאי לתקן את השגיאת כתיב ל-"transfer"
        }
        finally
        {
            if (con != null)
            {
                // close the db connection
                con.Close(); // סגירת החיבור למסד הנתונים בבלוק finally כדי להבטיח שהוא תמיד נסגר
            }
        }
    }
    //--------------------------------------------------------------------------------------------------
    // This method updates users preferences
    //--------------------------------------------------------------------------------------------------
    public UserPreferences? UpdateUserPreferences(
        //int preference_id,
        int user_id, string shelter_type, bool accessibility_needed, int num_default_people, bool pets_allowed)
    {
        SqlConnection con;
        SqlCommand cmd;
        UserPreferences? userPreferences = null;
        try
        {
            con = connect("myProjDB"); // יוצר ומחזיר חיבור למסד הנתונים
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message); // טיפול בשגיאת חיבור למסד הנתונים
        }
        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@user_id", user_id);
        //paramDic.Add("@preference_id", preference_id);
        paramDic.Add("@preferred_shelter_type", shelter_type);
        paramDic.Add("@accessible_needed", accessibility_needed);
        paramDic.Add("@num_default_people", num_default_people);
        paramDic.Add("@pets_allowed", pets_allowed);
        // חסרים כאן הפרמטרים accessibility_needed, pets_allowed ו-last_update - יש להוסיף אותם למילון כדי שהעדכון יתבצע כראוי

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdatePreferences", con, paramDic); // יצירת פקודה עבור הפרוצדורה המאוחסנת "FC_SP_UpdateUserPreferences"

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader()) // ביצוע הפקודה וקבלת קורא נתונים
            {
                if (dr.Read()) // קריאת שורה אחת מתוצאות העדכון - מצופה שתחזור שורה אחת עם הנתונים המעודכנים
                {
                    userPreferences = new UserPreferences // יצירת אובייקט UserPreferences עם הנתונים המעודכנים שחזרו ממסד הנתונים
                    {
                        PreferenceId = Convert.ToInt32(dr["preference_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        ShelterType = dr["preferred_shelter_type"].ToString() ?? "",
                        AccessibilityNeeded = Convert.ToBoolean(dr["accessible_needed"]),
                        NumDefaultPeople = Convert.ToInt32(dr["num_default_people"]),
                        PetsAllowed = Convert.ToBoolean(dr["pets_allowed"]),
                        LastUpdate = Convert.ToDateTime(dr["last_updated"])
                    };
                }
            }
            return userPreferences; // החזרת אובייקט UserPreferences המעודכן (או null אם העדכון נכשל ולא חזרו נתונים)
        }
        catch (SqlException ex) // טיפול בשגיאות SQL ספציפיות
        {
            if (ex.Message.Contains("User not found")) // בדיקה אם השגיאה נובעת מ-ID לא תקין
            {
                throw new Exception("User not found");
            }
            if (ex.Message.Contains("preference not found for this user"))
            {
                throw new Exception("preference not found for this user");
            }
            throw new Exception("Update failed"); // שגיאה כללית במקרה של כשל בעדכון
        }
        finally
        {
            if (con != null && con.State == System.Data.ConnectionState.Open) // בדיקה שהחיבור פתוח לפני סגירה
            {
                con.Close(); // סגירת החיבור למסד הנתונים
            }
        }
    }

    public UserPreferences? ResetUserPreferences(int user_id)
    {
        SqlConnection con;
        SqlCommand cmd;
        UserPreferences? preferences = null;

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

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_ResetPreferences", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    preferences = new UserPreferences
                    {
                        PreferenceId = Convert.ToInt32(dr["preference_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        ShelterType = dr["preferred_shelter_type"].ToString() ?? "",
                        AccessibilityNeeded = Convert.ToBoolean(dr["accessible_needed"]),
                        NumDefaultPeople = Convert.ToInt32(dr["num_default_people"]),
                        PetsAllowed = Convert.ToBoolean(dr["pets_allowed"]),
                        LastUpdate = Convert.ToDateTime(dr["last_updated"])
                    };
                }
            }
            return preferences;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("User not found"))
                throw new Exception("User not found");
            if (ex.Message.Contains("Preferences not found"))
                throw new Exception("Preferences not found for this user");

            throw new Exception("Reset failed");
        }
        finally
        {
            if (con != null && con.State == ConnectionState.Open)
            {
                con.Close();
            }
        }
    }


    public UserPreferences? AddPreference(int user_id, string shelter_type, bool accessibility_needed, int num_default_people, bool pets_allowed)
    {
        SqlConnection con;
        SqlCommand cmd;
        UserPreferences? userPreferences = null;
        try
        {
            con = connect("myProjDB"); // יוצר ומחזיר חיבור למסד הנתונים
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message); // טיפול בשגיאת חיבור למסד הנתונים
        }
        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@user_id", user_id);
        paramDic.Add("@preferred_shelter_type", shelter_type);
        paramDic.Add("@accessible_needed", accessibility_needed);
        paramDic.Add("@num_default_people", num_default_people);
        paramDic.Add("@pets_allowed", pets_allowed);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AddUserPreference", con, paramDic); // יצירת פקודה עבור הפרוצדורה המאוחסנת "FC_SP_AddPreference"
        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader()) // ביצוע הפקודה וקבלת קורא נתונים
            {
                if (dr.Read()) // קריאת שורה אחת מתוצאות ההוספה - מצופה שתחזור שורה אחת עם הנתונים שהוספו
                {
                    userPreferences = new UserPreferences // יצירת אובייקט UserPreferences עם הנתונים שהוחזרו ממסד הנתונים
                    {
                        PreferenceId = Convert.ToInt32(dr["preference_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        ShelterType = dr["preferred_shelter_type"].ToString() ?? "",
                        AccessibilityNeeded = Convert.ToBoolean(dr["accessible_needed"]),
                        NumDefaultPeople = Convert.ToInt32(dr["num_default_people"]),
                        PetsAllowed = Convert.ToBoolean(dr["pets_allowed"]),
                        LastUpdate = Convert.ToDateTime(dr["last_updated"]) // שימו לב שייתכן שהפרוצדורה המאוחסנת לא תחזיר את last_update בעת הוספה
                    };
                }
            }
            return userPreferences; // החזרת אובייקט UserPreferences שהוסף (או null אם ההוספה נכשלה ולא חזרו נתונים)
        }
        catch (SqlException ex) // טיפול בשגיאות SQL ספציפיות
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
            }
            if (ex.Message.Contains("User added Preference already"))
            {
                throw new Exception("User added Preference already");
            }
            throw new Exception("Add failed");
        }
        finally
        {
            if (con != null && con.State == System.Data.ConnectionState.Open) // בדיקה שהחיבור פתוח לפני סגירה
            {
                con.Close(); // סגירת החיבור למסד הנתונים
            }
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method geting users known location data
    //--------------------------------------------------------------------------------------------------
    public KnownLocation? GetKnownLocation(int location_id)
    {
        SqlConnection con;
        SqlCommand cmd;
        KnownLocation? knownLocation = null; // Initialize as null

        try
        {
            con = connect("myProjDB"); // Create the connection to the database
        }
        catch (Exception)
        {
            throw; // Handle connection errors
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@location_id", location_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetKnownLocation", con, paramDic);
        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read()) // Read a single row
                {
                    knownLocation = new KnownLocation // Correctly initialize the object
                    {
                        LocationId= Convert.ToInt32(dr["location_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Radius = Convert.ToSingle(dr["radius"]),
                        Address = dr["address"].ToString() ?? "",
                        LocationName = dr["location_name"].ToString() ?? "",
                        CreatedAt = dr["created_at"] != DBNull.Value
                            ? Convert.ToDateTime(dr["created_at"])
                            : null
                    };
                }
            }
            return knownLocation; // Return the KnownLocation object or null
        }
        catch (Exception ex)
        {
            throw new Exception("User data transfer failed: " + ex.Message);
        }
        finally
        {
            if (con != null)
            {
                con.Close(); // Ensure the connection is closed
            }
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method geting users known location data
    //--------------------------------------------------------------------------------------------------
    public List<KnownLocation> GetMyKnownLocations(int user_id)
    {
        SqlConnection con;
        SqlCommand cmd;
        List<KnownLocation> knownLocations = new List<KnownLocation>();

        try
        {
            con = connect("myProjDB"); // יצירת החיבור למסד הנתונים
        }
        catch (Exception)
        {
            throw; // במקרה של שגיאה בחיבור
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@user_id", user_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetMyKnownLocations", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read()) // שים לב: שונה מ־if ל־while כדי לעבור על כל השורות
                {
                    KnownLocation location = new KnownLocation
                    {
                        LocationId = Convert.ToInt32(dr["location_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Radius = Convert.ToSingle(dr["radius"]),
                        Address = dr["address"].ToString() ?? "",
                        LocationName = dr["location_name"].ToString() ?? "",
                        CreatedAt = dr["created_at"] != DBNull.Value
    ? Convert.ToDateTime(dr["created_at"])
    : null

                    };

                    knownLocations.Add(location); // הוספת המיקום לרשימה
                }
            }

            return knownLocations; // החזרת כל המיקומים שנמצאו
        }
        catch (Exception ex)
        {
            throw new Exception("User data transfer failed: " + ex.Message);
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
    // This method updates users known location data
    //--------------------------------------------------------------------------------------------------
    public KnownLocation? UpdateKnownLocation(int location_id, int user_id, float latitude, float longitude, float radius, string address, string location_name)
    {
        SqlConnection con;
        SqlCommand cmd;
        KnownLocation? knownLocation = null;
        try
        {
            con = connect("myProjDB"); // יוצר ומחזיר חיבור למסד הנתונים
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message); // טיפול בשגיאת חיבור למסד הנתונים
        }
        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@location_id", location_id); // הוספת מזהה המיקום כפרמטר לעדכון
        paramDic.Add("@user_id", user_id);
        paramDic.Add("@latitude", latitude);
        paramDic.Add("@longitude", longitude);
        paramDic.Add("@radius", radius);
        paramDic.Add("@address", address);
        paramDic.Add("@location_name", location_name);
        
       
        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdateKnownLocation", con, paramDic); // יצירת פקודה עבור הפרוצדורה המאוחסנת "FC_SP_UpdateKnownLocation"
        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader()) // ביצוע הפקודה וקבלת קורא נתונים
            {
                if (dr.Read()) // קריאת שורה אחת מתוצאות העדכון - מצופה שתחזור שורה אחת עם הנתונים המעודכנים
                {
                    knownLocation = new KnownLocation // יצירת אובייקט KnownLocation עם הנתונים המעודכנים שחזרו ממסד הנתונים
                    {
                        LocationId = Convert.ToInt32(dr["location_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Radius = Convert.ToSingle(dr["radius"]),
                        Address = dr["address"].ToString() ?? "",
                        LocationName = dr["location_name"].ToString() ?? ""

                    };
                }
            }
            return knownLocation; // החזרת אובייקט KnownLocation המעודכן (או null אם העדכון נכשל ולא חזרו נתונים)
        }
        catch (SqlException ex) // טיפול בשגיאות SQL ספציפיות
        {
            if (ex.Message.Contains("Invalid ID"))
                throw new Exception("Invalid ID");
            if (ex.Message.Contains("Location not found"))
                throw new Exception("Location not found for this user");
            throw new Exception("Update failed");
        }
        finally
        {
            if (con != null && con.State == System.Data.ConnectionState.Open) // בדיקה שהחיבור פתוח לפני סגירה
            {
                con.Close(); // סגירת החיבור למסד הנתונים
            }
        }
    }

    public KnownLocation? AddKnownLocation(int user_id, float latitude, float longitude, float radius, string address, string location_name)
    {
        SqlConnection con;
        SqlCommand cmd;
        KnownLocation? knownLocation = null;
        try
        {
            con = connect("myProjDB"); // יוצר ומחזיר חיבור למסד הנתונים
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection error: " + ex.Message); // טיפול בשגיאת חיבור למסד הנתונים
        }
        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@user_id", user_id);
        paramDic.Add("@latitude", latitude);
        paramDic.Add("@longitude", longitude);
        paramDic.Add("@radius", radius);
        paramDic.Add("@address", address);
        paramDic.Add("@location_name", location_name);
        
        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AddKnownLocation", con, paramDic); // יצירת פקודה עבור הפרוצדורה המאוחסנת "FC_SP_AddKnownLocation"
        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader()) // ביצוע הפקודה וקבלת קורא נתונים
            {
                if (dr.Read()) // קריאת שורה אחת מתוצאות ההוספה - מצופה שתחזור שורה אחת עם הנתונים שהוספו
                {
                    knownLocation = new KnownLocation // יצירת אובייקט KnownLocation עם הנתונים שהוחזרו ממסד הנתונים
                    {
                        LocationId = Convert.ToInt32(dr["location_id"]),
                        UserId = Convert.ToInt32(dr["user_id"]),
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Radius = Convert.ToSingle(dr["radius"]),
                        Address = dr["address"].ToString() ?? "",
                        LocationName = dr["location_name"].ToString() ?? "",
                        CreatedAt = Convert.ToDateTime(dr["created_at"])
                    };
                }
            }
            return knownLocation; // החזרת אובייקט KnownLocation שהוסף (או null אם ההוספה נכשלה ולא חזרו נתונים)
        }
        catch (SqlException ex) // טיפול בשגיאות SQL ספציפיות
        {
            if (ex.Message.Contains("Invalid ID")) // בדיקה אם השגיאה נובעת מ-ID לא תקין
            {
                throw new Exception("Invalid ID");
            }
            if (ex.Message.Contains("User added this known location already")) // בדיקה אם השגיאה נובעת מ-ID לא תקין
            {
                throw new Exception("User added this known location already");
            }
            Console.WriteLine("SQL Error: " + ex.Message);
            throw new Exception("add failed"); // שגיאה כללית במקרה של כשל בהוספה - כדאי לשנות את הודעת השגיאה ל-"Add failed"
        }
        finally
        {
            if (con != null && con.State == System.Data.ConnectionState.Open) // בדיקה שהחיבור פתוח לפני סגירה
            {
                con.Close(); // סגירת החיבור למסד הנתונים
            }
        }
    }

    public bool DeleteKnownLocation(int location_id, int user_id)
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
        paramDic.Add("@location_id", location_id);
        paramDic.Add("@user_id", user_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_DeleteKnownLocation", con, paramDic);

        try
        {
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains("Invalid ID"))
                throw new Exception("Invalid ID");
            if (ex.Message.Contains("Location not found"))
                throw new Exception("Location not found for this user");
            throw new Exception("Delete failed");
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
    // This method gets the shelter visit history for a specific user
    //--------------------------------------------------------------------------------------------------
    public List<VisitHistory> GetUserVisitHistory(int user_id)
    {
        SqlConnection con;
        SqlCommand cmd;
        List<VisitHistory> historyList = new List<VisitHistory>();

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

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_GetShelterVisitHistory", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    VisitHistory visit = new VisitHistory
                    {
                        VisitId = Convert.ToInt32(dr["visit_id"]),
                        ArrivalTime = dr["arrival_time"] != DBNull.Value ? Convert.ToDateTime(dr["arrival_time"]) : (DateTime?)null,
                        ShelterName = dr["shelter_name"].ToString() ?? "",
                        ShelterAddress = dr["shelter_address"].ToString() ?? "",
                        AdditionalInformation = dr["additional_information"].ToString() ?? ""
                    };

                    historyList.Add(visit);
                }
            }
            return historyList;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
                throw new Exception("Invalid ID");
            throw new Exception("Failed to retrieve visit history");
        }
        finally
        {
            if (con != null && con.State == ConnectionState.Open)
            {
                con.Close();
            }
        }
    }


}

